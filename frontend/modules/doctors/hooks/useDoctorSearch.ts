import { useQuery } from '@tanstack/react-query';
import api from '../../../lib/api';
import { DoctorFilterOptions, DoctorSearchResult } from '../../../types/doctor';
import { DoctorDetailsDto } from '../types/doctor';

// backend dto (actual API response shape)
type DoctorSearchDto = {
  id: string;
  fullName: string;
  specialization: string;
  hospital: string;
  date: string;
};

// map api → ui model
function mapDoctor(dto: DoctorSearchDto): DoctorSearchResult {
  return {
    doctorId: dto.id,
    doctorName: dto.fullName,
    specialization: dto.specialization,
    hospitalName: dto.hospital,
    date: dto.date,
  };
}
// map to UI model (optional but consistent)
function mapDoctorDetails(dto: DoctorDetailsDto) {
  return {
    doctorId: dto.id,
    doctorName: dto.fullName,
    specialization: dto.specialization,
    hospitalName: dto.hospital,
  };
}

type DoctorSearchParams = {
  doctorName?: string;
  specialization?: string;
  hospital?: string;
  date?: string;
  lookAheadDays?: number;
};

function formatIsoDate(date: Date) {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

function addDays(baseDate: Date, days: number) {
  const next = new Date(baseDate);
  next.setDate(next.getDate() + days);
  return next;
}

export function useDoctorFilterOptions() {
  return useQuery<DoctorFilterOptions>({
    queryKey: ['doctor-filter-options'],
    queryFn: async () => {
      const { data } = await api.get<DoctorFilterOptions>('/doctors/filter-options');
      return data;
    },
  });
}

export function useDoctorSearch(params: DoctorSearchParams) {
  return useQuery<DoctorSearchResult[]>({
    queryKey: ['doctor-search', params],
    enabled: !!params.date,
    queryFn: async () => {
      if (!params.date) return [];

      const { data } = await api.get<DoctorSearchDto[]>('/doctors/search', {
        params: {
          Name: params.doctorName || undefined,
          Specialization: params.specialization || undefined,
          Hospital: params.hospital || undefined,
          Date: params.date, // backend already accepts YYYY-MM-DD
        },
      });

      return data.map(mapDoctor);
    },
  });
}

export function useDoctorAvailability(doctorId?: string) {
  return useQuery({
    queryKey: ['doctor-availability', doctorId],
    enabled: !!doctorId,
    queryFn: async () => {
      const { data } = await api.get(`/doctors/${doctorId}/availability`);
      return data;
    },
  });
}

export function useDoctorDetails(doctorId?: string) {
  return useQuery({
    queryKey: ['doctor-details', doctorId],
    enabled: !!doctorId,
    queryFn: async () => {
      const { data } = await api.get<DoctorDetailsDto>(`/doctors/${doctorId}`);
      return mapDoctorDetails(data);
    },
  });
}