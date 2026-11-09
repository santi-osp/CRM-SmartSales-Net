import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
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
        list: () => of({ items: [], page: 1, pageSize: 20, totalCount: 0 }), create
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
});
