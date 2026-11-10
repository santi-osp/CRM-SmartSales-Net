import { HttpErrorResponse } from '@angular/common/http';
import { CdkDrag, CdkDropList, CdkDropListGroup, CdkDragDrop } from '@angular/cdk/drag-drop';
import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatMenuModule } from '@angular/material/menu';
import { MatSelectModule } from '@angular/material/select';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { catchError, of } from 'rxjs';
import { UserSummary } from '../auth/auth.models';
import { ContactsApi } from '../contacts/contacts.api';
import { Opportunity, Priority } from '../opportunities/opportunity.models';
import { OpportunitiesApi } from '../opportunities/opportunities.api';
import { CloseOpportunityDialog, ClosePayload } from './close-opportunity-dialog';

type OpenStage = 'New' | 'Contacted' | 'Proposal';
interface Column { items: Opportunity[]; page: number; total: number; loading: boolean; error: boolean; }

@Component({
  selector: 'app-pipeline-page',
  imports: [CdkDrag, CdkDropList, CdkDropListGroup, ReactiveFormsModule,
    MatButtonModule, MatDialogModule, MatFormFieldModule, MatMenuModule, MatSelectModule, RouterLink],
  templateUrl: './pipeline-page.html',
  styleUrl: './pipeline-page.scss'
})
export class PipelinePage {
  private readonly api = inject(OpportunitiesApi);
  private readonly contacts = inject(ContactsApi);
  private readonly dialog = inject(MatDialog);
  private readonly destroyRef = inject(DestroyRef);
  private readonly router = inject(Router, { optional: true });
  private readonly route = inject(ActivatedRoute, { optional: true });
  private readonly generation: Record<OpenStage, number> = { New: 0, Contacted: 0, Proposal: 0 };
  readonly stages: { value: OpenStage; label: string }[] = [
    { value: 'New', label: 'Nuevo' },
    { value: 'Contacted', label: 'En contacto' },
    { value: 'Proposal', label: 'Propuesta' }
  ];
  readonly columns = signal<Record<OpenStage, Column>>({
    New: this.empty(), Contacted: this.empty(), Proposal: this.empty()
  });
  readonly contactNames = signal<Record<string, string>>({});
  readonly users = signal<UserSummary[]>([]);
  readonly ownerFilter = new FormControl('', { nonNullable: true });
  readonly priorityFilter = new FormControl('', { nonNullable: true });
  readonly appliedOwner = signal('');
  readonly appliedPriority = signal('');
  readonly savingId = signal<string | null>(null);
  readonly error = signal<string | null>(null);

  constructor() {
    const params = this.route?.snapshot.queryParams;
    if (params) {
      if (params['owner']) {
        this.ownerFilter.setValue(params['owner']);
        this.appliedOwner.set(params['owner']);
      }
      if (params['priority']) {
        this.priorityFilter.setValue(params['priority']);
        this.appliedPriority.set(params['priority']);
      }
    }
    this.refreshAll();
    this.api.users().pipe(takeUntilDestroyed()).subscribe({
      next: result => this.users.set(result.items),
      error: () => this.error.set('No se cargaron los nombres de responsables.')
    });
  }

  private empty(): Column { return { items: [], page: 0, total: 0, loading: false, error: false }; }

  refreshAll(): void {
    for (const stage of this.stages) this.load(stage.value, true);
  }

  load(stage: OpenStage, reset = false): void {
    const previous = this.columns()[stage];
    if (previous.loading && !reset) return;
    const page = reset ? 1 : previous.page + 1;
    const requestId = ++this.generation[stage];
    this.setColumn(stage, { ...previous, loading: true, error: false });
    this.api.list(page, this.appliedOwner(), this.appliedPriority(), stage)
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: result => {
          if (this.generation[stage] !== requestId) return;
          const seen = new Map<string, Opportunity>();
          for (const item of [...(reset ? [] : previous.items), ...result.items]) {
            const current = seen.get(item.id);
            if (!current || item.version >= current.version) seen.set(item.id, item);
          }
          this.setColumn(stage, { items: [...seen.values()], page, total: result.totalCount,
            loading: false, error: false });
          this.loadContactNames(result.items.map(item => item.contactId));
        },
        error: () => {
          if (this.generation[stage] === requestId)
            this.setColumn(stage, { ...this.columns()[stage], loading: false, error: true });
        }
      });
  }

  private loadContactNames(ids: string[]): void {
    const missing = [...new Set(ids)].filter(id => !(id in this.contactNames()));
    if (missing.length === 0) return;
    this.contacts.lookup(missing).pipe(
      catchError(() => of({ items: [], missingIds: missing })),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe(result => this.contactNames.update(names => ({ ...names,
      ...Object.fromEntries(result.items.map(item => [item.id, item.name])) })));
  }

  private setColumn(stage: OpenStage, column: Column): void {
    this.columns.update(current => ({ ...current, [stage]: column }));
  }

  applyFilters(): void {
    this.appliedOwner.set(this.ownerFilter.value);
    this.appliedPriority.set(this.priorityFilter.value);
    this.router?.navigate([], {
      queryParams: {
        owner: this.ownerFilter.value || null,
        priority: this.priorityFilter.value || null
      },
      queryParamsHandling: 'merge'
    }).catch(() => {});
    this.refreshAll();
  }
  clearFilters(): void {
    this.ownerFilter.setValue('');
    this.priorityFilter.setValue('');
    this.router?.navigate([], {
      queryParams: { owner: null, priority: null },
      queryParamsHandling: 'merge'
    }).catch(() => {});
    this.applyFilters();
  }

  drop(event: CdkDragDrop<OpenStage>): void {
    const item = event.item.data as Opportunity;
    this.move(item, event.container.data);
  }

  move(item: Opportunity, target: OpenStage): void {
    if (item.stage === target || this.savingId()) return;
    this.error.set(null);
    this.savingId.set(item.id);
    this.api.changeStage(item.id, target, item.version).subscribe({
        next: () => {
          if (this.destroyRef.destroyed) return;
          this.savingId.set(null); this.refreshAll();
        },
        error: () => {
          if (this.destroyRef.destroyed) return;
          this.savingId.set(null);
          this.error.set('No se pudo mover la oportunidad. Se recargó el estado guardado.');
          this.refreshAll();
        }
      });
  }

  beginClose(item: Opportunity): void {
    if (this.savingId()) return;
    this.dialog.open<CloseOpportunityDialog, Opportunity, ClosePayload>(CloseOpportunityDialog,
      { data: item, width: 'min(100vw, 480px)' })
      .afterClosed().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(payload => {
        if (payload) this.confirmClose(item, payload);
      });
  }

  private confirmClose(item: Opportunity, payload: ClosePayload): void {
    if (this.savingId()) return;
    this.savingId.set(item.id);
    this.error.set(null);
    this.api.close(item.id, payload.result, payload.finalAmount, payload.lossReason, item.version).subscribe({
        next: () => {
          if (this.destroyRef.destroyed) return;
          this.savingId.set(null); this.refreshAll();
        },
        error: (failure: unknown) => {
          if (this.destroyRef.destroyed) return;
          this.savingId.set(null);
          this.error.set(failure instanceof HttpErrorResponse && failure.status === 409
            ? 'La oportunidad cambió o ya está cerrada. Revisa el tablero actualizado.'
            : 'No se pudo cerrar la oportunidad. Se recargó el estado guardado.');
          this.refreshAll();
        }
      });
  }

  contactLabel(id: string): string { return this.contactNames()[id] ?? `Contacto no disponible (${id})`; }
  ownerLabel(id: string): string { return this.users().find(user => user.id === id)?.displayName ?? id; }
  priorityLabel(value: Priority): string { return ({ High: 'Alta', Medium: 'Media', Low: 'Baja' })[value]; }
  money(value: number): string { return new Intl.NumberFormat('es-CO', { style: 'currency', currency: 'COP' }).format(value); }
}
