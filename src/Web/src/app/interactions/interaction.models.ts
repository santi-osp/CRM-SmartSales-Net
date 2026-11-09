import { Page } from '../contacts/contact.models';

export type InteractionType = 'Call' | 'Email' | 'Meeting';
export interface Interaction {
  id: string;
  contactId: string;
  opportunityId: string | null;
  type: InteractionType;
  occurredAt: string;
  summary: string;
  authorUserId: string;
  createdAt: string;
}
export type InteractionPage = Page<Interaction>;
