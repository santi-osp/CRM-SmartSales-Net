import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, DestroyRef, inject, signal, ViewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { DateTime } from 'luxon';
import { FullCalendarModule, FullCalendarComponent } from '@fullcalendar/angular';
import { CalendarOptions, DatesSetArg, EventClickArg } from '@fullcalendar/core';
import { DateClickArg } from '@fullcalendar/interaction';
import esLocale from '@fullcalendar/core/locales/es';
import dayGridPlugin from '@fullcalendar/daygrid';
import timeGridPlugin from '@fullcalendar/timegrid';
import listPlugin from '@fullcalendar/list';
import interactionPlugin from '@fullcalendar/interaction';
import { catchError, debounceTime, distinctUntilChanged, of, startWith, switchMap } from 'rxjs';
import { AuthService } from '../auth/auth.service';
import { Contact } from '../contacts/contact.models';
import { ContactsApi } from '../contacts/contacts.api';
import { Opportunity } from '../opportunities/opportunity.models';
import { OpportunitiesApi } from '../opportunities/opportunities.api';
import { FollowUp, FollowUpBucket, FollowUpPage } from './follow-up.models';
import { FollowUpsApi } from './follow-ups.api';

@Component({
  selector: 'app-follow-ups-page',
  imports: [ReactiveFormsModule, MatButtonModule, MatFormFieldModule, MatInputModule,
    MatSelectModule, FullCalendarModule],
  templateUrl: './follow-ups-page.html',
  styleUrl: './follow-ups-page.scss'
})
export class FollowUpsPage {
  @ViewChild('calendar') calendar?: FullCalendarComponent;
  private readonly api = inject(FollowUpsApi);
  private readonly contacts = inject(ContactsApi);
  private readonly opportunities = inject(OpportunitiesApi);
  private readonly auth = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);
  private requestId = 0;
  private calendarRequestId = 0;
  visibleFrom = '';
  visibleTo = '';
  readonly viewMode = signal<'Calendar' | 'List'>('Calendar');
  readonly calendarEvents = signal<FollowUp[]>([]);
  readonly calendarLoading = signal(false);
  readonly calendarError = signal<string | null>(null);
  readonly selectedEvent = signal<FollowUp | null>(null);
  readonly isAdmin = computed(() => this.auth.user()?.role === 'Administrator');

  readonly calendarOptions: CalendarOptions = {
    plugins: [dayGridPlugin, timeGridPlugin, listPlugin, interactionPlugin],
    initialView: 'dayGridMonth',
    locales: [esLocale],
    locale: 'es',
    timeZone: 'America/Bogota',
    firstDay: 1,
    weekNumbers: false,
    nowIndicator: true,
    selectable: true,
    editable: false,
    eventStartEditable: false,
    eventDurationEditable: false,
    slotLabelFormat: { hour: '2-digit', minute: '2-digit', hour12: false },
    eventTimeFormat: { hour: '2-digit', minute: '2-digit', hour12: false },
    headerToolbar: {
      left: 'prev,next today',
      center: 'title',
      right: 'dayGridMonth,timeGridWeek,timeGridDay,listMonth'
    },
    views: {
      dayGridMonth: { dayMaxEvents: 2 }
    },
    buttonText: { today: 'Hoy', month: 'Mes', week: 'Semana', day: 'Día', list: 'Lista' },
    height: 'auto',
    datesSet: (arg: DatesSetArg) => this.onDatesSet(arg),
    eventClick: (arg: EventClickArg) => this.onEventClick(arg),
    dateClick: (arg: DateClickArg) => this.onDateClick(arg),
    events: []
  };
  readonly buckets: { value: FollowUpBucket; label: string }[] = [
    { value: 'Overdue', label: 'Vencidos' }, { value: 'Today', label: 'Hoy' },
    { value: 'Upcoming', label: 'Próximos' }, { value: 'Completed', label: 'Completados' }
  ];
  readonly bucket = signal<FollowUpBucket>('Today');
  readonly scope = signal<'Mine' | 'All'>('Mine');
  readonly pageNumber = signal(1);
  readonly page = signal<FollowUpPage | null>(null);
  readonly loading = signal(false);
  readonly listError = signal<string | null>(null);
  readonly actionError = signal<string | null>(null);
  readonly saving = signal(false);
  readonly completingId = signal<string | null>(null);
  readonly contactNames = signal<Record<string, string>>({});
  readonly contactQuery = new FormControl('', { nonNullable: true });
  readonly contactChoices = signal<Contact[]>([]);
  readonly opportunityChoices = signal<Opportunity[]>([]);
  readonly opportunityTotal = signal(0);
  readonly opportunityPage = signal(0);
  readonly opportunityError = signal<string | null>(null);
  readonly form = new FormGroup({
    contactId: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    opportunityId: new FormControl('', { nonNullable: true }),
    subject: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(160)] }),
    scheduledLocal: new FormControl('', { nonNullable: true, validators: [Validators.required] })
  });

  constructor() {
    this.load();
    this.contactQuery.valueChanges.pipe(startWith(''), debounceTime(250), distinctUntilChanged(),
      switchMap(q => this.contacts.list(q).pipe(catchError(() => of(null)))),
      takeUntilDestroyed()).subscribe(result => {
        if (result) this.contactChoices.set(result.items);
      });
    this.form.controls.contactId.valueChanges.pipe(takeUntilDestroyed()).subscribe(id => {
      this.form.controls.opportunityId.setValue('');
      this.opportunityChoices.set([]);
      this.opportunityTotal.set(0);
      this.opportunityPage.set(0);
      if (id) this.loadOpportunities(id, 1);
    });
  }

  selectBucket(value: FollowUpBucket): void {
    if (value === this.bucket()) return;
    this.bucket.set(value);
    this.pageNumber.set(1);
    this.load();
  }

  selectScope(value: 'Mine' | 'All'): void {
    if (value === 'All' && !this.isAdmin()) return;
    this.scope.set(value);
    this.pageNumber.set(1);
    this.load();
    if (this.visibleFrom && this.visibleTo) {
      this.loadCalendar(this.visibleFrom, this.visibleTo);
    }
  }

  toggleViewMode(mode: 'Calendar' | 'List'): void {
    this.viewMode.set(mode);
    if (mode === 'Calendar' && this.visibleFrom && this.visibleTo) {
      this.loadCalendar(this.visibleFrom, this.visibleTo);
    }
  }

  onDatesSet(arg: DatesSetArg): void {
    // FullCalendar uses UTC coercion for named zones without a timezone plugin.
    // Its date strings represent the visible Bogotá grid dates; JS Dates do not.
    const start = DateTime.fromISO(arg.startStr.slice(0, 10), { zone: 'America/Bogota' });
    const end = DateTime.fromISO(arg.endStr.slice(0, 10), { zone: 'America/Bogota' }).minus({ days: 1 });
    this.visibleFrom = start.toISODate()!;
    this.visibleTo = end.toISODate()!;
    this.loadCalendar(this.visibleFrom, this.visibleTo);
  }

  loadCalendar(from: string, to: string): void {
    const id = ++this.calendarRequestId;
    this.calendarLoading.set(true);
    this.calendarError.set(null);
    this.api.calendar(from, to, this.scope()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: items => {
        if (id !== this.calendarRequestId) return;
        this.calendarEvents.set(items);
        this.calendarLoading.set(false);
        this.updateCalendarOptionsEvents(items);

        const missing = [...new Set(items.map(item => item.contactId))]
          .filter(contactId => !(contactId in this.contactNames()));
        if (missing.length) {
          // Lookup admite 100 IDs; dividir en lotes seguros para agendas extensas.
          for (let offset = 0; offset < missing.length; offset += 100) {
            this.contacts.lookup(missing.slice(offset, offset + 100)).pipe(
              catchError(() => of({ items: [], missingIds: [] })),
              takeUntilDestroyed(this.destroyRef)
            ).subscribe(lookup => {
              this.contactNames.update(names => ({
                ...names,
                ...Object.fromEntries(lookup.items.map(item => [item.id, item.name]))
              }));
            });
          }
        }
      },
      error: (failure: unknown) => {
        if (id !== this.calendarRequestId) return;
        this.calendarLoading.set(false);
        this.calendarEvents.set([]);
        this.calendar?.getApi()?.removeAllEvents();
        this.calendarError.set(failure instanceof HttpErrorResponse && failure.status === 413
          ? 'La vista supera 500 seguimientos. Selecciona semana o día para acotar la consulta.'
          : 'No se pudieron cargar los seguimientos del calendario. Intenta otra vez.');
      }
    });
  }

  private updateCalendarOptionsEvents(items: FollowUp[]): void {
    const now = DateTime.now().setZone('America/Bogota');
    const tomorrow = now.startOf('day').plus({ days: 1 });

    const events = items.map(item => {
      const scheduled = DateTime.fromISO(item.scheduledAt, { setZone: true }).setZone('America/Bogota');
      let color = '#0B9E87'; // Upcoming
      let statusLabel = 'Próximo';

      if (item.status === 'Completed') {
        color = '#5A6B87'; // Neutral
        statusLabel = 'Completado';
      } else if (scheduled < now) {
        color = '#D44F62'; // Overdue
        statusLabel = 'Vencido';
      } else if (scheduled < tomorrow) {
        color = '#315BFF'; // Today
        statusLabel = 'Hoy';
      }

      return {
        id: item.id,
        title: `${item.subject} (${statusLabel})`,
        start: scheduled.toISO() ?? item.scheduledAt,
        backgroundColor: color,
        borderColor: color,
        textColor: '#FFFFFF',
        extendedProps: { followUp: item, statusLabel }
      };
    });

    this.calendarOptions.events = events;
    if (this.calendar?.getApi()) {
      this.calendar.getApi().removeAllEvents();
      this.calendar.getApi().addEventSource(events);
    }
  }

  onEventClick(arg: EventClickArg): void {
    const followUp = arg.event.extendedProps['followUp'] as FollowUp;
    if (followUp) {
      this.selectedEvent.set(followUp);
    }
  }

  onDateClick(arg: DateClickArg): void {
    const dateStr = arg.dateStr;
    const defaultTime = dateStr.includes('T') ? dateStr.slice(0, 16) : `${dateStr}T09:00`;
    this.form.controls.scheduledLocal.setValue(defaultTime);
  }

  closeEventDetail(): void {
    this.selectedEvent.set(null);
  }

  completeSelectedEvent(): void {
    const item = this.selectedEvent();
    if (!item) return;
    this.complete(item);
    this.selectedEvent.set(null);
  }

  load(): void {
    const id = ++this.requestId;
    this.loading.set(true);
    this.listError.set(null);
    this.api.list(this.bucket(), this.scope(), this.pageNumber())
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: result => {
          if (id !== this.requestId) return;
          this.page.set(result);
          this.loading.set(false);
          const missing = [...new Set(result.items.map(item => item.contactId))]
            .filter(contactId => !(contactId in this.contactNames()));
          if (missing.length) this.contacts.lookup(missing).pipe(
            catchError(() => of({ items: [], missingIds: missing })),
            takeUntilDestroyed(this.destroyRef)).subscribe(lookup =>
              this.contactNames.update(names => ({ ...names,
                ...Object.fromEntries(lookup.items.map(item => [item.id, item.name])) })));
        },
        error: () => {
          if (id !== this.requestId) return;
          this.loading.set(false);
          this.listError.set('No se pudo cargar la agenda.');
        }
      });
  }

  previous(): void { if (this.pageNumber() > 1) { this.pageNumber.update(value => value - 1); this.load(); } }
  next(): void {
    if (this.pageNumber() * 20 < (this.page()?.totalCount ?? 0)) {
      this.pageNumber.update(value => value + 1); this.load();
    }
  }

  private loadOpportunities(contactId: string, page: number): void {
    this.opportunities.forContact(contactId, page).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: result => {
        if (this.form.controls.contactId.value !== contactId) return;
        this.opportunityChoices.update(previous => page === 1 ? result.items : [...previous, ...result.items]);
        this.opportunityTotal.set(result.totalCount);
        this.opportunityPage.set(page);
        this.opportunityError.set(null);
      },
      error: () => this.opportunityError.set('No se cargaron las oportunidades del contacto.')
    });
  }

  moreOpportunities(): void {
    const id = this.form.controls.contactId.value;
    if (id && this.opportunityChoices().length < this.opportunityTotal())
      this.loadOpportunities(id, this.opportunityPage() + 1);
  }

  save(): void {
    if (this.form.invalid || this.saving()) { this.form.markAllAsTouched(); return; }
    const value = this.form.getRawValue();
    const local = DateTime.fromISO(value.scheduledLocal, { zone: 'America/Bogota' });
    if (!local.isValid || !local.toUTC().toISO()) {
      this.actionError.set('Indica una fecha y hora válidas en Bogotá.');
      return;
    }
    this.actionError.set(null);
    this.saving.set(true);
    this.api.create({ contactId: value.contactId, opportunityId: value.opportunityId || null,
      subject: value.subject.trim(), scheduledAt: local.toUTC().toISO()! })
      .subscribe({
        next: item => {
          if (this.destroyRef.destroyed) return;
          this.saving.set(false);
          this.form.reset({ contactId: '', opportunityId: '', subject: '', scheduledLocal: '' });
          const scheduled = DateTime.fromISO(item.scheduledAt, { setZone: true });
          const current = DateTime.now();
          const tomorrow = current.setZone('America/Bogota').startOf('day').plus({ days: 1 });
          this.bucket.set(scheduled < current ? 'Overdue'
            : scheduled.setZone('America/Bogota') < tomorrow ? 'Today' : 'Upcoming');
          this.pageNumber.set(1);
          this.load();
          if (this.visibleFrom && this.visibleTo) this.loadCalendar(this.visibleFrom, this.visibleTo);
        },
        error: failure => {
          if (this.destroyRef.destroyed) return;
          this.saving.set(false);
          this.actionError.set(this.errorMessage(failure));
        }
      });
  }

  complete(item: FollowUp): void {
    if (this.completingId()) return;
    this.completingId.set(item.id);
    this.actionError.set(null);
    this.api.complete(item.id, item.version).subscribe({
      next: () => {
        if (this.destroyRef.destroyed) return;
        this.completingId.set(null);
        this.load();
        if (this.visibleFrom && this.visibleTo) this.loadCalendar(this.visibleFrom, this.visibleTo);
      },
      error: failure => {
        if (this.destroyRef.destroyed) return;
        this.completingId.set(null);
        this.actionError.set(this.errorMessage(failure));
        this.load();
      }
    });
  }

  contactLabel(id: string): string { return this.contactNames()[id] ?? `Contacto no disponible (${id})`; }
  dateTime(value: string): string {
    return DateTime.fromISO(value, { setZone: true }).setZone('America/Bogota').toFormat('dd/LL/yyyy, HH:mm');
  }
  private errorMessage(failure: unknown): string {
    if (!(failure instanceof HttpErrorResponse)) return 'No se pudo completar la operación.';
    if (failure.status === 409) return 'El seguimiento cambió. La agenda se actualizó.';
    if (failure.status === 503) return 'El directorio no está disponible. Intenta de nuevo.';
    return failure.error?.title ?? 'No se pudo completar la operación.';
  }
}
