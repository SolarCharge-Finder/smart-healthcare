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

// api dto from backend
export type DoctorSearchDto = {
  id: string;
  fullName: string;
  specialization: string;
  hospital: string;
};

// ui model
export type DoctorSearchResult = {
  doctorId: string;
  doctorName: string;
  specialization: string;
  hospitalName: string;
};

// mapper
export function mapDoctor(dto: DoctorSearchDto): DoctorSearchResult {
  return {
    doctorId: dto.id,
    doctorName: dto.fullName,
    specialization: dto.specialization,
    hospitalName: dto.hospital,
  };
}
