import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { AuthService } from '../auth/auth.service';
import { DashboardApi } from '../dashboard/dashboard.api';
import { OpportunitiesApi } from '../opportunities/opportunities.api';
import { Home } from './home';

describe('Dashboard', () => {
  it('aplica y limpia los filtros comunes y rechaza un rango invertido', () => {
    const get = vi.fn(() => of({
      openCount: 0, wonCount: 0, lostCount: 0, openEstimatedAmount: 0, currency: 'COP',
      byStage: ['New', 'Contacted', 'Proposal', 'Won', 'Lost'].map(stage => ({ stage, count: 0 })),
      filters: { ownerUserId: null, createdFrom: null, createdTo: null },
      generatedAt: '2026-11-08T10:00:00Z'
    }));
    TestBed.configureTestingModule({ providers: [
      provideRouter([]),
      { provide: AuthService, useValue: { user: signal(null), clear: () => {} } },
      { provide: OpportunitiesApi, useValue: { users: () => of({ items: [] }) } },
      { provide: DashboardApi, useValue: { get } }
    ] });
    const page = TestBed.runInInjectionContext(() => new Home());
    expect(get).toHaveBeenCalledWith('', '', '');
    page.owner.setValue('owner-1');
    page.createdFrom.setValue('2026-11-09');
    page.createdTo.setValue('2026-11-08');
    page.apply();
    expect(page.validationError()).toContain('fecha inicial');
    expect(get).toHaveBeenCalledTimes(1);
    page.createdFrom.setValue('2026-11-08');
    page.apply();
    expect(get).toHaveBeenCalledWith('owner-1', '2026-11-08', '2026-11-08');
    page.clear();
    expect(get).toHaveBeenLastCalledWith('', '', '');
    expect(page.validationError()).toBeNull();
  });
});
