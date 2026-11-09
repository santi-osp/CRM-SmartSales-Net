export type ContactType = 'Prospect' | 'Customer';
export type ContactSource = 'Web' | 'Referral' | 'WhatsApp' | 'Call' | 'Other';

export interface Contact {
  id: string;
  name: string;
  type: ContactType;
  email: string | null;
  phone: string | null;
  source: ContactSource;
  createdAt: string;
  updatedAt: string;
  version: number;
}

export interface ContactWrite {
  name: string;
  type: ContactType;
  email: string | null;
  phone: string | null;
  source: ContactSource;
  acceptPotentialDuplicate: boolean;
}

export interface Page<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface ApiProblem {
  title?: string;
  code?: string;
  duplicateContactIds?: string[];
  traceId?: string;
}

export interface ContactReference { id: string; name: string; type: ContactType; }
export interface ContactLookupResponse { items: ContactReference[]; missingIds: string[]; }
