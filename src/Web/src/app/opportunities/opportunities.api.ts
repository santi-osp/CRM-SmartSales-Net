import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { UserSummary } from '../auth/auth.models';
import { Page } from '../contacts/contact.models';
import { Opportunity, OpportunityPage, OpportunityWrite, OwnerChange, Stage } from './opportunity.models';

@Injectable({ providedIn: 'root' })
export class OpportunitiesApi {
  private readonly http = inject(HttpClient);
  private readonly base = '/api/sales/v1/opportunities';

  list(page: number, ownerUserId?: string, priority?: string, stage?: Stage): Observable<OpportunityPage> {
    let params = new HttpParams().set('page', page).set('pageSize', 20);
    if (ownerUserId) params = params.set('ownerUserId', ownerUserId);
    if (priority) params = params.set('priority', priority);
    if (stage) params = params.set('stage', stage);
    return this.http.get<OpportunityPage>(this.base, { params });
  }

  get(id: string): Observable<Opportunity> {
    return this.http.get<Opportunity>(`${this.base}/${id}`);
  }

  forContact(contactId: string, page = 1): Observable<OpportunityPage> {
    const params = new HttpParams().set('contactId', contactId).set('page', page).set('pageSize', 100);
    return this.http.get<OpportunityPage>(this.base, { params });
  }

  create(body: OpportunityWrite & { contactId: string; ownerUserId?: string }): Observable<Opportunity> {
    return this.http.post<Opportunity>(this.base, body);
  }

  update(id: string, body: OpportunityWrite & { version: number }): Observable<Opportunity> {
    return this.http.put<Opportunity>(`${this.base}/${id}`, body);
  }

  changeOwner(id: string, ownerUserId: string, version: number): Observable<Opportunity> {
    return this.http.patch<Opportunity>(`${this.base}/${id}/owner`, { ownerUserId, version });
  }

  changeStage(id: string, stage: 'New' | 'Contacted' | 'Proposal', version: number): Observable<Opportunity> {
    return this.http.patch<Opportunity>(`${this.base}/${id}/stage`, { stage, version });
  }

  close(id: string, result: 'Won' | 'Lost', finalAmount: number | null,
    lossReason: string | null, version: number): Observable<Opportunity> {
    return this.http.post<Opportunity>(`${this.base}/${id}/close`, { result, finalAmount, lossReason, version });
  }

  ownerChanges(id: string): Observable<Page<OwnerChange>> {
    return this.http.get<Page<OwnerChange>>(`${this.base}/${id}/owner-changes`);
  }

  users(): Observable<{ items: UserSummary[] }> {
    return this.http.get<{ items: UserSummary[] }>('/api/contacts/v1/references/users');
  }
}
