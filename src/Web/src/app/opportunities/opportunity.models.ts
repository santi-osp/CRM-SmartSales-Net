import { Page } from '../contacts/contact.models';

export type Priority = 'High' | 'Medium' | 'Low';
export type Stage = 'New' | 'Contacted' | 'Proposal' | 'Won' | 'Lost';

export interface Opportunity {
  id: string;
  contactId: string;
  title: string;
  estimatedAmount: number;
  expectedCloseDate: string;
  ownerUserId: string;
  priority: Priority;
  stage: Stage;
  createdByUserId: string;
  createdAt: string;
  updatedAt: string;
  closedAt: string | null;
  closedByUserId: string | null;
  finalAmount: number | null;
  lossReason: string | null;
  version: number;
}

export interface OpportunityWrite {
  title: string;
  estimatedAmount: number;
  expectedCloseDate: string;
  priority: Priority;
}

export interface OwnerChange {
  id: string;
  previousOwnerUserId: string;
  newOwnerUserId: string;
  changedByUserId: string;
  changedAt: string;
}

export type OpportunityPage = Page<Opportunity>;
