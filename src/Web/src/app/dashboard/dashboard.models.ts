import { Stage } from '../opportunities/opportunity.models';

export interface Dashboard {
  openCount: number;
  wonCount: number;
  lostCount: number;
  openEstimatedAmount: number;
  currency: 'COP';
  byStage: { stage: Stage; count: number }[];
  filters: { ownerUserId: string | null; createdFrom: string | null; createdTo: string | null };
  generatedAt: string;
}
