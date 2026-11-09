import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, inject, Input, OnChanges, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { DateTime } from 'luxon';
import { Opportunity } from '../opportunities/opportunity.models';
import { OpportunitiesApi } from '../opportunities/opportunities.api';
import { InteractionPage, InteractionType } from './interaction.models';
import { InteractionsApi } from './interactions.api';

@Component({
  selector: 'app-interaction-panel',
  imports: [ReactiveFormsModule, MatButtonModule, MatFormFieldModule, MatInputModule, MatSelectModule],
  templateUrl: './interaction-panel.html',
  styleUrl: './interaction-panel.scss'
})
export class InteractionPanel implements OnChanges {
  @Input({ required: true }) contactId!: string;
  @Input() opportunityId: string | null = null;
  private readonly api = inject(InteractionsApi);
  private readonly opportunities = inject(OpportunitiesApi);
  private readonly destroyRef = inject(DestroyRef);
  private requestId = 0;
  readonly pageNumber = signal(1);
  readonly page = signal<InteractionPage | null>(null);
  readonly loading = signal(false);
  readonly listError = signal<string | null>(null);
  readonly formError = signal<string | null>(null);
  readonly saving = signal(false);
  readonly opportunityChoices = signal<Opportunity[]>([]);
  readonly opportunityTotal = signal(0);
  readonly opportunityPage = signal(0);
  readonly form = new FormGroup({
    opportunityId: new FormControl('', { nonNullable: true }),
    type: new FormControl<InteractionType>('Call', { nonNullable: true }),
    occurredLocal: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    summary: new FormControl('', { nonNullable: true,
      validators: [Validators.required, Validators.maxLength(2000)] })
  });

  ngOnChanges(): void {
    this.pageNumber.set(1);
    this.page.set(null);
    this.form.controls.opportunityId.setValue(this.opportunityId ?? '');
    this.form.controls.occurredLocal.setValue(DateTime.now().setZone('America/Bogota')
      .toFormat("yyyy-LL-dd'T'HH:mm"));
    this.opportunityChoices.set([]);
    this.opportunityPage.set(0);
    this.opportunityTotal.set(0);
    if (this.contactId) {
      this.load();
      if (!this.opportunityId) this.loadOpportunities(1);
    }
  }

  load(): void {
    if (!this.contactId) return;
    const id = ++this.requestId;
    this.loading.set(true);
    this.listError.set(null);
    this.api.list(this.opportunityId ? null : this.contactId, this.opportunityId,
      this.pageNumber()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: result => {
          if (id !== this.requestId) return;
          this.page.set(result);
          this.loading.set(false);
        },
        error: () => {
          if (id !== this.requestId) return;
          this.loading.set(false);
          this.listError.set('No se pudo cargar la actividad. La ficha sigue disponible.');
        }
      });
  }

  previous(): void { if (this.pageNumber() > 1) { this.pageNumber.update(value => value - 1); this.load(); } }
  next(): void {
    if (this.pageNumber() * 20 < (this.page()?.totalCount ?? 0)) {
      this.pageNumber.update(value => value + 1);
      this.load();
    }
  }

  private loadOpportunities(page: number): void {
    const contactId = this.contactId;
    this.opportunities.forContact(contactId, page).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: result => {
        if (this.contactId !== contactId) return;
        this.opportunityChoices.update(previous => page === 1 ? result.items : [...previous, ...result.items]);
        this.opportunityTotal.set(result.totalCount);
        this.opportunityPage.set(page);
      },
      error: () => this.formError.set('No se cargaron las oportunidades; puedes guardar la actividad sin asociarla.')
    });
  }
  moreOpportunities(): void {
    if (this.opportunityChoices().length < this.opportunityTotal())
      this.loadOpportunities(this.opportunityPage() + 1);
  }

  save(): void {
    if (this.form.invalid || this.saving()) { this.form.markAllAsTouched(); return; }
    const value = this.form.getRawValue();
    const local = DateTime.fromISO(value.occurredLocal, { zone: 'America/Bogota' });
    if (!local.isValid || local > DateTime.now()) {
      this.formError.set('La fecha debe ser válida y no estar en el futuro.');
      return;
    }
    this.saving.set(true);
    this.formError.set(null);
    this.api.create({ contactId: this.contactId, opportunityId: this.opportunityId ?? (value.opportunityId || null),
      type: value.type, occurredAt: local.toUTC().toISO()!, summary: value.summary.trim() })
      .subscribe({
        next: () => {
          if (this.destroyRef.destroyed) return;
          this.saving.set(false);
          this.form.controls.summary.setValue('');
          this.pageNumber.set(1);
          this.load();
        },
        error: failure => {
          if (this.destroyRef.destroyed) return;
          this.saving.set(false);
          this.formError.set(failure instanceof HttpErrorResponse
            ? failure.error?.title ?? 'No se pudo guardar la interacción.'
            : 'No se pudo guardar la interacción.');
        }
      });
  }

  typeLabel(type: InteractionType): string {
    return ({ Call: 'Llamada', Email: 'Correo', Meeting: 'Reunión' })[type];
  }
  dateTime(value: string): string {
    return DateTime.fromISO(value, { setZone: true }).setZone('America/Bogota')
      .toFormat('dd/LL/yyyy, HH:mm');
  }
}
