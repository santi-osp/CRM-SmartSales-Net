import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { Interaction, InteractionPage, InteractionType } from './interaction.models';

@Injectable({ providedIn: 'root' })
export class InteractionsApi {
  private readonly http = inject(HttpClient);
  private readonly base = '/api/sales/v1/interactions';

  list(contactId: string | null, opportunityId: string | null, page = 1): Observable<InteractionPage> {
    let params = new HttpParams().set('page', page).set('pageSize', 20);
    if (contactId) params = params.set('contactId', contactId);
    if (opportunityId) params = params.set('opportunityId', opportunityId);
    return this.http.get<InteractionPage>(this.base, { params });
  }

  create(body: { contactId: string; opportunityId: string | null; type: InteractionType;
    occurredAt: string; summary: string }): Observable<Interaction> {
    return this.http.post<Interaction>(this.base, body);
  }
}
