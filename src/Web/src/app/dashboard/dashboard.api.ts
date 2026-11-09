import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { Dashboard } from './dashboard.models';

@Injectable({ providedIn: 'root' })
export class DashboardApi {
  private readonly http = inject(HttpClient);

  get(ownerUserId: string, createdFrom: string, createdTo: string): Observable<Dashboard> {
    let params = new HttpParams();
    if (ownerUserId) params = params.set('ownerUserId', ownerUserId);
    if (createdFrom) params = params.set('createdFrom', createdFrom);
    if (createdTo) params = params.set('createdTo', createdTo);
    return this.http.get<Dashboard>('/api/sales/v1/dashboard', { params });
  }
}
