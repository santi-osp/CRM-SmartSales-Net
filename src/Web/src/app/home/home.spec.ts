import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { AuthService } from '../auth/auth.service';
import { DashboardApi } from '../dashboard/dashboard.api';
import { OpportunitiesApi } from '../opportunities/opportunities.api';
import { Home } from './home';
import { Dashboard } from '../dashboard/dashboard.models';

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

  it('calcula el win ratio de oportunidades cerradas y maneja el caso sin cierres', () => {
    const dashboardData: Dashboard = {
      openCount: 50,
      wonCount: 15,
      lostCount: 5,
      openEstimatedAmount: 10000000,
      currency: 'COP',
      byStage: [
        { stage: 'New', count: 20 },
        { stage: 'Contacted', count: 15 },
        { stage: 'Proposal', count: 15 },
        { stage: 'Won', count: 15 },
        { stage: 'Lost', count: 5 }
      ],
      filters: { ownerUserId: null, createdFrom: null, createdTo: null },
      generatedAt: '2026-11-09T10:00:00Z'
    };
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: { user: signal(null), clear: () => {} } },
        { provide: OpportunitiesApi, useValue: { users: () => of({ items: [] }) } },
        { provide: DashboardApi, useValue: { get: () => of(dashboardData) } }
      ]
    });
    const page = TestBed.runInInjectionContext(() => new Home());
    expect(page.winRatio()).toBe(75); // 15 / (15 + 5) * 100 = 75%

    // Caso sin cierres
    page.data.set({
      ...dashboardData,
      wonCount: 0,
      lostCount: 0
    });
    expect(page.winRatio()).toBeNull();

    // Comprobar ngOnDestroy no lanza error
    expect(() => page.ngOnDestroy()).not.toThrow();
  });
});
