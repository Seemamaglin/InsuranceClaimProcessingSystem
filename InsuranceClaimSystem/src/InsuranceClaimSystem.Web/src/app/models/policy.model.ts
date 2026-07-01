export enum BenefitType {
  Reimbursement = 1,
  FixedBenefit = 2
}

export enum PremiumFrequency {
  Monthly = 1,
  Quarterly = 2,
  HalfYearly = 3,
  Yearly = 4
}

export interface PolicyType {
  id: string;
  typeName: string;
  description: string;
  defaultBenefitType: BenefitType;
  allowsNomineeClaim: boolean;
  allowsThirdPartyClaim: boolean;
  defaultCoverageAmount: number;
  isActive: boolean;
}

export interface ApplyForPolicyRequest {
  policyTypeId: string;
  startDate: string;
  coverageAmount: number;
  premiumAmount: number;
  premiumFrequency: PremiumFrequency;
  nominees: any[]; 
}