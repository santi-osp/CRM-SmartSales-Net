import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { DateTime } from 'luxon';
import { AuthService } from '../auth/auth.service';
import { ContactsApi } from '../contacts/contacts.api';
import { OpportunitiesApi } from '../opportunities/opportunities.api';
import { FollowUpsApi } from './follow-ups.api';
import { FollowUpsPage } from './follow-ups-page';

describe('FollowUpsPage', () => {
  it('envía la hora de Bogotá como instante UTC y usa el usuario actual', () => {
    const create = vi.fn((body: { contactId: string; opportunityId: string | null;
      subject: string; scheduledAt: string }) => of({
        ...body, id: 'follow-up-1', ownerUserId: 'user-1', status: 'Pending' as const,
        createdAt: '2026-11-08T10:00:00Z', completedAt: null, completedByUserId: null, version: 1
      }));
    TestBed.configureTestingModule({ providers: [
      { provide: AuthService, useValue: { user: signal({ id: 'user-1', role: 'Commercial' }) } },
      { provide: ContactsApi, useValue: { list: () => of({ items: [] }) } },
      { provide: OpportunitiesApi, useValue: { forContact: () => of({ items: [], totalCount: 0 }) } },
      { provide: FollowUpsApi, useValue: {
        list: () => of({ items: [], page: 1, pageSize: 20, totalCount: 0 }),
        calendar: () => of([]),
        create
      } }
    ] });
    const page = TestBed.runInInjectionContext(() => new FollowUpsPage());
    page.form.setValue({ contactId: 'contact-1', opportunityId: '', subject: ' Llamar ',
      scheduledLocal: '2026-11-09T00:00' });
    page.save();
    expect(create).toHaveBeenCalledWith({ contactId: 'contact-1', opportunityId: null,
      subject: 'Llamar', scheduledAt: '2026-11-09T05:00:00.000Z' });
    expect(page.isAdmin()).toBe(false);
    page.selectScope('All');
    expect(page.scope()).toBe('Mine');
  });

  it('consulta todos los seguimientos del rango visible y respeta el alcance', () => {
    const calendar = vi.fn(() => of([]));
    TestBed.configureTestingModule({ providers: [
      { provide: AuthService, useValue: { user: signal({ id: 'admin-1', role: 'Administrator' }) } },
      { provide: ContactsApi, useValue: { list: () => of({ items: [] }) } },
      { provide: OpportunitiesApi, useValue: { forContact: () => of({ items: [], totalCount: 0 }) } },
      { provide: FollowUpsApi, useValue: {
        list: () => of({ items: [], page: 1, pageSize: 20, totalCount: 0 }), calendar
      } }
    ] });
    const page = TestBed.runInInjectionContext(() => new FollowUpsPage());
    page.onDatesSet({ startStr: '2026-11-01T00:00:00', endStr: '2026-12-01T00:00:00' } as never);
    expect(calendar).toHaveBeenLastCalledWith('2026-11-01', '2026-11-30', 'Mine');
    page.selectScope('All');
    expect(calendar).toHaveBeenLastCalledWith('2026-11-01', '2026-11-30', 'All');
    expect(page.isAdmin()).toBe(true);
  });

  it('prepara formulario con fecha seleccionada en Bogotá', () => {
    TestBed.configureTestingModule({ providers: [
      { provide: AuthService, useValue: { user: signal({ id: 'user-1', role: 'Commercial' }) } },
      { provide: ContactsApi, useValue: { list: () => of({ items: [] }) } },
      { provide: OpportunitiesApi, useValue: { forContact: () => of({ items: [], totalCount: 0 }) } },
      { provide: FollowUpsApi, useValue: {
        list: () => of({ items: [], page: 1, pageSize: 20, totalCount: 0 }),
        calendar: () => of([])
      } }
    ] });
    const page = TestBed.runInInjectionContext(() => new FollowUpsPage());
    page.onDateClick({ dateStr: '2026-11-09' } as never);
    expect(page.form.controls.scheduledLocal.value).toBe('2026-11-09T09:00');
    const bogota = DateTime.fromISO('2026-11-09T09:00', { zone: 'America/Bogota' });
    expect(bogota.toUTC().toISO()).toBe('2026-11-09T14:00:00.000Z');
  });
});
