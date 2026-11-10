import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { FollowUp, FollowUpBucket, FollowUpPage } from './follow-up.models';

@Injectable({ providedIn: 'root' })
export class FollowUpsApi {
  private readonly http = inject(HttpClient);
  private readonly base = '/api/sales/v1/follow-ups';

  list(bucket: FollowUpBucket, scope: 'Mine' | 'All', page: number): Observable<FollowUpPage> {
    const params = new HttpParams().set('bucket', bucket).set('scope', scope)
      .set('page', page).set('pageSize', 20);
    return this.http.get<FollowUpPage>(this.base, { params });
  }

  calendar(from: string, to: string, scope: 'Mine' | 'All'): Observable<FollowUp[]> {
    const params = new HttpParams().set('from', from).set('to', to).set('scope', scope);
    return this.http.get<FollowUp[]>(`${this.base}/calendar`, { params });
  }

  create(body: { contactId: string; opportunityId: string | null; subject: string;
    scheduledAt: string }): Observable<FollowUp> {
    return this.http.post<FollowUp>(this.base, body);
  }

  complete(id: string, version: number): Observable<FollowUp> {
    return this.http.post<FollowUp>(`${this.base}/${id}/complete`, { version });
  }
}
