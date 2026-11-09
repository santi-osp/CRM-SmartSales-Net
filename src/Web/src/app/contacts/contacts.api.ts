import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { Contact, ContactLookupResponse, ContactWrite, Page } from './contact.models';

@Injectable({ providedIn: 'root' })
export class ContactsApi {
  private readonly http = inject(HttpClient);
  private readonly base = '/api/contacts/v1/contacts';

  list(q: string, page = 1, type?: string): Observable<Page<Contact>> {
    let params = new HttpParams().set('q', q).set('page', page).set('pageSize', 20);
    if (type) params = params.set('type', type);
    return this.http.get<Page<Contact>>(this.base, { params });
  }

  get(id: string): Observable<Contact> {
    return this.http.get<Contact>(`${this.base}/${id}`);
  }

  create(body: ContactWrite): Observable<Contact> {
    return this.http.post<Contact>(this.base, body);
  }

  update(id: string, body: ContactWrite & { version: number }): Observable<Contact> {
    return this.http.put<Contact>(`${this.base}/${id}`, body);
  }

  lookup(ids: string[]): Observable<ContactLookupResponse> {
    return this.http.post<ContactLookupResponse>('/api/contacts/v1/references/contacts/lookup', { ids });
  }
}
