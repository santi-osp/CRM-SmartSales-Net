import { Page } from '../contacts/contact.models';

export type FollowUpBucket = 'Overdue' | 'Today' | 'Upcoming' | 'Completed';
export interface FollowUp {
  id: string;
  contactId: string;
  opportunityId: string | null;
  subject: string;
  scheduledAt: string;
  ownerUserId: string;
  status: 'Pending' | 'Completed';
  createdAt: string;
  completedAt: string | null;
  completedByUserId: string | null;
  version: number;
}
export type FollowUpPage = Page<FollowUp>;
