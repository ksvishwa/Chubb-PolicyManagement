export interface Policy {
  id: string;
  policyNumber: string;
  policyholderName: string;
  lineOfBusiness: string;
  status: PolicyStatus;
  premiumAmount: number;
  currency: string;
  effectiveDate: string;
  expiryDate: string;
  region: string;
  underwriter: string;
  flaggedForReview: boolean;
  createdAt: string;
  updatedAt: string;
}

export type PolicyStatus = 'Active' | 'Expired' | 'Pending' | 'Cancelled';

export type LineOfBusiness = 'Property' | 'Casualty' | 'AAndH' | 'Marine';

export interface PagedResult<T> {
  data: T[];
  page: number;
  size: number;
  totalCount: number;
  totalPages: number;
}

export interface PolicySummary {
  totalPolicies: number;
  activePolicies: number;
  expiredPolicies: number;
  pendingPolicies: number;
  cancelledPolicies: number;
  flaggedForReview: number;
  totalPremiumAmount: number;
  byLineOfBusiness: Record<string, number>;
  byRegion: Record<string, number>;
}

export interface PolicyFilterParams {
  page?: number;
  size?: number;
  sort?: string;
  status?: string;
  lineOfBusiness?: string;
  region?: string;
  effectiveDateFrom?: string;
  effectiveDateTo?: string;
  search?: string;
}

export interface BulkFlagRequest {
  policyIds: string[];
}
