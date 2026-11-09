import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { OpportunitiesApi } from '../opportunities/opportunities.api';
import { InteractionPanel } from './interaction-panel';
import { InteractionsApi } from './interactions.api';

describe('InteractionPanel', () => {
  it('mantiene la ficha disponible cuando falla la consulta de actividad', () => {
    TestBed.configureTestingModule({ providers: [
      { provide: InteractionsApi, useValue: {
        list: () => throwError(() => new Error('Servicio temporalmente no disponible'))
      } },
      { provide: OpportunitiesApi, useValue: {
        forContact: () => of({ items: [], totalCount: 0 })
      } }
    ] });
    const panel = TestBed.runInInjectionContext(() => new InteractionPanel());
    panel.contactId = 'contact-1';
    panel.ngOnChanges();
    expect(panel.listError()).toContain('La ficha sigue disponible');
    expect(panel.page()).toBeNull();
    expect(panel.contactId).toBe('contact-1');
  });
});
