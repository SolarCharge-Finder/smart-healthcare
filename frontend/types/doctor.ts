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
  hospitalName: string;
  date: string;
};

export type Availability = {
  id: string;
  doctorId: string;
  hospital: string;
  startTime: string;
  endTime: string;
  isActive: boolean;
  isRecurring: boolean;
  dayOfWeek: number | null;
};

// payload for create
export type CreateAvailabilityDto = {
  hospital: string;
  startTime: string;
  endTime: string;
  isRecurring: boolean;
  dayOfWeek: number | null;
};