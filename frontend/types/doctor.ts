export type FilterHospital = {
  id: string;
  name: string;
};

export type DoctorFilterOptions = {
  doctorNames: string[];
  specializations: string[];
  hospitals: string[];
};

export type PricingBreakdown = {
  doctorFee: number;
  hospitalFee: number;
  eChannellingFee: number;
  discount: number;
  totalFee: number;
};

export type DoctorSearchResult = {
  doctorId: string;
  doctorName: string;
  specialization: string;
  hospitalId: string;
  hospitalName: string;
  date: string;
  availableSlots: string[];
  pricing: PricingBreakdown;
};
