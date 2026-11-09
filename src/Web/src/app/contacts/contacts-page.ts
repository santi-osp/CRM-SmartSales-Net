import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { RouterLink } from '@angular/router';
import { EMPTY, Subject, finalize, merge, switchMap, tap, debounceTime, distinctUntilChanged, startWith, map, catchError } from 'rxjs';
import { ApiProblem, Contact, ContactSource, ContactType, ContactWrite, Page } from './contact.models';
import { ContactsApi } from './contacts.api';
import { InteractionPanel } from '../interactions/interaction-panel';

@Component({
  selector: 'app-contacts-page',
  imports: [ReactiveFormsModule, MatButtonModule, MatFormFieldModule, MatInputModule, MatSelectModule, RouterLink,
    InteractionPanel],
  templateUrl: './contacts-page.html',
  styleUrl: './contacts-page.scss'
})
export class ContactsPage {
  private readonly api = inject(ContactsApi);
  private readonly refresh = new Subject<void>();
  readonly query = new FormControl('', { nonNullable: true });
  readonly pageNumber = signal(1);
  readonly page = signal<Page<Contact> | null>(null);
  readonly loading = signal(true);
  readonly listError = signal<string | null>(null);
  readonly detailError = signal<string | null>(null);
  readonly selected = signal<Contact | null>(null);
  readonly showForm = signal(false);
  readonly saving = signal(false);
  readonly formError = signal<string | null>(null);
  readonly duplicateIds = signal<string[]>([]);
  readonly form = new FormGroup({
    name: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(160)] }),
    type: new FormControl<ContactType>('Prospect', { nonNullable: true, validators: [Validators.required] }),
    email: new FormControl('', { nonNullable: true, validators: [Validators.email] }),
    phone: new FormControl('', { nonNullable: true }),
    source: new FormControl<ContactSource>('Web', { nonNullable: true, validators: [Validators.required] })
  });
  readonly sources: { value: ContactSource; label: string }[] = [
    { value: 'Web', label: 'Web' }, { value: 'Referral', label: 'Referido' },
    { value: 'WhatsApp', label: 'WhatsApp' }, { value: 'Call', label: 'Llamada' },
    { value: 'Other', label: 'Otro' }
  ];

  constructor() {
    merge(
      this.query.valueChanges.pipe(startWith(''), debounceTime(250), distinctUntilChanged(),
        tap(() => this.pageNumber.set(1))),
      this.refresh.pipe(map(() => this.query.value))
    ).pipe(
      tap(() => { this.loading.set(true); this.listError.set(null); }),
      switchMap(q => this.api.list(q, this.pageNumber()).pipe(
        catchError(() => {
          this.listError.set('No se pudieron cargar los contactos.');
          return EMPTY;
        }),
        finalize(() => this.loading.set(false))
      )),
      takeUntilDestroyed()
    ).subscribe(result => this.page.set(result));
    this.form.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => this.duplicateIds.set([]));
  }

  reload(): void { this.refresh.next(); }

  previous(): void {
    if (this.pageNumber() > 1) { this.pageNumber.update(value => value - 1); this.reload(); }
  }

  next(): void {
    if (this.pageNumber() * 20 < (this.page()?.totalCount ?? 0)) {
      this.pageNumber.update(value => value + 1);
      this.reload();
    }
  }

  open(id: string): void {
    this.detailError.set(null);
    this.showForm.set(false);
    this.api.get(id).subscribe({
      next: contact => this.selected.set(contact),
      error: () => this.detailError.set('No se pudo abrir la ficha. Intenta de nuevo.')
    });
  }

  create(): void {
    this.selected.set(null);
    this.form.reset({ name: '', type: 'Prospect', email: '', phone: '', source: 'Web' });
    this.formError.set(null);
    this.duplicateIds.set([]);
    this.showForm.set(true);
  }

  edit(): void {
    const contact = this.selected();
    if (!contact) return;
    this.form.reset({ name: contact.name, type: contact.type, email: contact.email ?? '',
      phone: contact.phone ?? '', source: contact.source });
    this.formError.set(null);
    this.duplicateIds.set([]);
    this.showForm.set(true);
  }

  cancel(): void { this.showForm.set(false); this.duplicateIds.set([]); }

  save(acceptPotentialDuplicate = false): void {
    if (this.saving() || this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const value = this.form.getRawValue();
    if (!value.email.trim() && !value.phone.trim()) {
      this.formError.set('Indica correo o teléfono.');
      return;
    }
    const body: ContactWrite = {
      name: value.name.trim(), type: value.type,
      email: value.email.trim() || null, phone: value.phone.trim() || null,
      source: value.source, acceptPotentialDuplicate
    };
    const current = this.selected();
    this.saving.set(true);
    this.formError.set(null);
    const request = current
      ? this.api.update(current.id, { ...body, version: current.version })
      : this.api.create(body);
    request.pipe(finalize(() => this.saving.set(false))).subscribe({
      next: contact => {
        this.selected.set(contact);
        this.showForm.set(false);
        this.duplicateIds.set([]);
        this.reload();
      },
      error: (failure: unknown) => {
        const problem = failure instanceof HttpErrorResponse ? failure.error as ApiProblem : null;
        if (failure instanceof HttpErrorResponse && failure.status === 409 &&
            problem?.code === 'potential_duplicate') {
          this.duplicateIds.set(problem.duplicateContactIds ?? []);
          this.formError.set('Hay un posible duplicado. Revisa antes de confirmar.');
        } else if (failure instanceof HttpErrorResponse && failure.status === 409 &&
                   problem?.code === 'concurrency_conflict') {
          this.formError.set('La ficha cambió. Recarga antes de editarla de nuevo.');
        } else {
          this.formError.set(problem?.title ?? 'No se pudo guardar el contacto.');
        }
      }
    });
  }

  typeLabel(type: ContactType): string { return type === 'Prospect' ? 'Prospecto' : 'Cliente'; }
}
