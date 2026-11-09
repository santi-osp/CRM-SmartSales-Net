import { TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { of, throwError } from 'rxjs';
import { ContactsApi } from '../contacts/contacts.api';
import { Opportunity } from '../opportunities/opportunity.models';
import { OpportunitiesApi } from '../opportunities/opportunities.api';
import { PipelinePage } from './pipeline-page';

describe('PipelinePage', () => {
  it('recarga la etapa persistida cuando el movimiento falla', () => {
    const item: Opportunity = {
      id: 'opportunity-1', contactId: 'contact-1', title: 'Venta de prueba',
      estimatedAmount: 120, expectedCloseDate: '2026-12-01', ownerUserId: 'user-1',
      priority: 'Medium', stage: 'New', createdByUserId: 'user-1',
      createdAt: '2026-11-08T10:00:00Z', updatedAt: '2026-11-08T10:00:00Z',
      closedAt: null, closedByUserId: null, finalAmount: null, lossReason: null, version: 1
    };
    const api = {
      users: () => of({ items: [] }),
      list: (_page: number, _owner: string, _priority: string, stage: string) =>
        of({ items: stage === 'New' ? [item] : [], page: 1, pageSize: 20,
          totalCount: stage === 'New' ? 1 : 0 }),
      changeStage: () => throwError(() => new Error('Fallo temporal'))
    };
    TestBed.configureTestingModule({ providers: [
      { provide: OpportunitiesApi, useValue: api },
      { provide: ContactsApi, useValue: { lookup: () => of({ items: [], missingIds: ['contact-1'] }) } },
      { provide: MatDialog, useValue: {} }
    ] });

    const page = TestBed.runInInjectionContext(() => new PipelinePage());
    expect(page.columns().New.items).toHaveLength(1);
    page.move(item, 'Contacted');
    expect(page.error()).toContain('No se pudo mover');
    expect(page.columns().New.items).toEqual([item]);
    expect(page.columns().Contacted.items).toHaveLength(0);
  });
});
