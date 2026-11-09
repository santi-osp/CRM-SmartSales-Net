import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { RouterLink } from '@angular/router';
import { EMPTY, Subject, catchError, debounceTime, distinctUntilChanged, finalize, map, of, startWith, switchMap } from 'rxjs';
import { AuthService } from '../auth/auth.service';
import { UserSummary } from '../auth/auth.models';
import { ApiProblem, Contact, ContactLookupResponse } from '../contacts/contact.models';
import { ContactsApi } from '../contacts/contacts.api';
import { Opportunity, OpportunityPage, OpportunityWrite, OwnerChange, Priority } from './opportunity.models';
import { OpportunitiesApi } from './opportunities.api';
import { InteractionPanel } from '../interactions/interaction-panel';

@Component({
  selector: 'app-opportunities-page',
  imports: [ReactiveFormsModule, MatButtonModule, MatFormFieldModule, MatInputModule, MatSelectModule, RouterLink,
    InteractionPanel],
  templateUrl: './opportunities-page.html',
  styleUrl: './opportunities-page.scss'
})
export class OpportunitiesPage {
  private readonly api = inject(OpportunitiesApi);
  private readonly contactsApi = inject(ContactsApi);
  private readonly auth = inject(AuthService);
  private readonly refresh = new Subject<void>();
  readonly isAdmin = computed(() => this.auth.user()?.role === 'Administrator');
  readonly page = signal<OpportunityPage | null>(null);
  readonly pageNumber = signal(1);
  readonly loading = signal(true);
  readonly listError = signal<string | null>(null);
  readonly contactNames = signal<Record<string, string>>({});
  readonly users = signal<UserSummary[]>([]);
  readonly usersError = signal<string | null>(null);
  readonly ownerFilter = new FormControl('', { nonNullable: true });
  readonly priorityFilter = new FormControl('', { nonNullable: true });
  readonly appliedOwner = signal('');
  readonly appliedPriority = signal('');
  readonly selected = signal<Opportunity | null>(null);
  readonly ownerChanges = signal<OwnerChange[]>([]);
  readonly detailError = signal<string | null>(null);
  readonly showForm = signal(false);
  readonly saving = signal(false);
  readonly formError = signal<string | null>(null);
  readonly contactQuery = new FormControl('', { nonNullable: true });
  readonly contactChoices = signal<Contact[]>([]);
  readonly contactSearchError = signal<string | null>(null);
  readonly contactId = new FormControl('', { nonNullable: true, validators: [Validators.required] });
  readonly ownerId = new FormControl('', { nonNullable: true });
  readonly newOwnerId = new FormControl('', { nonNullable: true });
  readonly form = new FormGroup({
    title: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(160)] }),
    estimatedAmount: new FormControl<number | null>(null, [Validators.required, Validators.min(0)]),
    expectedCloseDate: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    priority: new FormControl<Priority>('Medium', { nonNullable: true, validators: [Validators.required] })
  });

  constructor() {
    this.refresh.pipe(startWith(undefined), switchMap(() => {
      this.loading.set(true);
      this.listError.set(null);
      return this.api.list(this.pageNumber(), this.appliedOwner(), this.appliedPriority()).pipe(
        switchMap(page => {
          const ids = [...new Set(page.items.map(item => item.contactId))];
          if (ids.length === 0) return of({ page, names: {} as Record<string, string> });
          return this.contactsApi.lookup(ids).pipe(
            map((lookup: ContactLookupResponse) => ({ page,
              names: Object.fromEntries(lookup.items.map(item => [item.id, item.name])) })),
            catchError(() => of({ page, names: {} as Record<string, string> }))
          );
        }),
        catchError(() => { this.listError.set('No se pudieron cargar las oportunidades.'); return EMPTY; }),
        finalize(() => this.loading.set(false))
      );
    }), takeUntilDestroyed()).subscribe(({ page, names }) => {
      this.page.set(page);
      this.contactNames.set(names);
    });

    this.contactQuery.valueChanges.pipe(startWith(''), debounceTime(250), distinctUntilChanged(),
      switchMap(q => this.contactsApi.list(q, 1).pipe(
        catchError(() => { this.contactSearchError.set('No se pudo buscar contactos.'); return of(null); })
      )), takeUntilDestroyed()).subscribe(result => {
        if (result) { this.contactChoices.set(result.items); this.contactSearchError.set(null); }
      });
    this.api.users().pipe(takeUntilDestroyed()).subscribe({
      next: result => this.users.set(result.items),
      error: () => this.usersError.set('No se pudo cargar el directorio de responsables.')
    });
  }

  reload(): void { this.refresh.next(); }
  applyFilters(): void {
    this.appliedOwner.set(this.ownerFilter.value);
    this.appliedPriority.set(this.priorityFilter.value);
    this.pageNumber.set(1);
    this.reload();
  }
  clearFilters(): void {
    this.ownerFilter.setValue(''); this.priorityFilter.setValue('');
    this.applyFilters();
  }
  previous(): void { if (this.pageNumber() > 1) { this.pageNumber.update(value => value - 1); this.reload(); } }
  next(): void {
    if (this.pageNumber() * 20 < (this.page()?.totalCount ?? 0)) {
      this.pageNumber.update(value => value + 1); this.reload();
    }
  }

  open(id: string): void {
    this.showForm.set(false);
    this.detailError.set(null);
    this.api.get(id).subscribe({
      next: item => {
        this.selected.set(item);
        this.newOwnerId.setValue(item.ownerUserId);
        this.api.ownerChanges(id).subscribe({
          next: result => this.ownerChanges.set(result.items),
          error: () => this.detailError.set('La oportunidad está disponible, pero no se pudo cargar la auditoría.')
        });
      },
      error: () => this.detailError.set('No se pudo abrir la oportunidad.')
    });
  }

  create(): void {
    this.selected.set(null);
    this.showForm.set(true);
    this.formError.set(null);
    this.contactId.setValue('');
    this.ownerId.setValue('');
    this.form.reset({ title: '', estimatedAmount: null, expectedCloseDate: '', priority: 'Medium' });
  }
  edit(): void {
    const item = this.selected();
    if (!item) return;
    this.form.reset({ title: item.title, estimatedAmount: item.estimatedAmount,
      expectedCloseDate: item.expectedCloseDate, priority: item.priority });
    this.showForm.set(true);
    this.formError.set(null);
  }
  cancel(): void { this.showForm.set(false); }

  save(): void {
    if (this.form.invalid || (this.selected() === null && this.contactId.invalid) || this.saving()) {
      this.form.markAllAsTouched(); this.contactId.markAsTouched(); return;
    }
    const values = this.form.getRawValue();
    if (values.estimatedAmount === null) return;
    const body: OpportunityWrite = { title: values.title.trim(), estimatedAmount: values.estimatedAmount,
      expectedCloseDate: values.expectedCloseDate, priority: values.priority };
    const current = this.selected();
    const request = current
      ? this.api.update(current.id, { ...body, version: current.version })
      : this.api.create({ ...body, contactId: this.contactId.value,
          ...(this.isAdmin() && this.ownerId.value ? { ownerUserId: this.ownerId.value } : {}) });
    this.saving.set(true);
    this.formError.set(null);
    request.pipe(finalize(() => this.saving.set(false))).subscribe({
      next: item => { this.selected.set(item); this.showForm.set(false); this.reload(); },
      error: failure => this.formError.set(this.errorMessage(failure))
    });
  }

  changeOwner(): void {
    const current = this.selected();
    if (!this.isAdmin() || !current || !this.newOwnerId.value || this.saving()) return;
    this.saving.set(true);
    this.detailError.set(null);
    this.api.changeOwner(current.id, this.newOwnerId.value, current.version)
      .pipe(finalize(() => this.saving.set(false))).subscribe({
        next: item => { this.selected.set(item); this.open(item.id); this.reload(); },
        error: failure => this.detailError.set(this.errorMessage(failure))
      });
  }

  contactLabel(id: string): string { return this.contactNames()[id] ?? `Contacto no disponible (${id})`; }
  ownerLabel(id: string): string { return this.users().find(user => user.id === id)?.displayName ?? id; }
  priorityLabel(value: Priority): string { return ({ High: 'Alta', Medium: 'Media', Low: 'Baja' })[value]; }
  money(value: number): string { return new Intl.NumberFormat('es-CO', { style: 'currency', currency: 'COP' }).format(value); }
  dateTime(value: string): string { return new Intl.DateTimeFormat('es-CO', {
    dateStyle: 'medium', timeStyle: 'short', timeZone: 'America/Bogota'
  }).format(new Date(value)); }

  private errorMessage(failure: unknown): string {
    if (!(failure instanceof HttpErrorResponse)) return 'No se pudo completar la operación.';
    const problem = failure.error as ApiProblem;
    if (failure.status === 409) return 'La oportunidad cambió. Recarga la ficha antes de guardar.';
    if (failure.status === 503) return 'El directorio no está disponible. Intenta de nuevo.';
    if (failure.status === 403) return 'Tu rol no permite esta operación.';
    return problem?.title ?? 'No se pudo completar la operación.';
  }
}
