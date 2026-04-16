import { useQuery } from '@tanstack/react-query';
import api from '../../../lib/api';
import { DoctorFilterOptions, DoctorSearchResult } from '../../../types/doctor';

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
  const lookAheadDays = params.lookAheadDays ?? 7;

  return useQuery<DoctorSearchResult[]>({
    queryKey: ['doctor-search', params, lookAheadDays],
    enabled: Boolean(params.date),
    queryFn: async () => {
      const selectedDate = params.date
        ? new Date(params.date + 'T00:00:00Z') // safer timezone handling
        : null;

      if (!selectedDate || Number.isNaN(selectedDate.getTime())) {
        return [];
      }

      const requests = Array.from({ length: lookAheadDays + 1 }, (_, offset) => {
        const targetDate = formatIsoDate(addDays(selectedDate, offset));

        return api.get<DoctorSearchDto[]>('/doctors/search', {
          params: {
            name: params.doctorName || undefined,
            specialization: params.specialization || undefined,
            hospital: params.hospital || undefined,
            date: targetDate,
          },
        });
      });

      const responses = await Promise.all(requests);

      const data = responses
        .flatMap((response) => response.data)
        .map(mapDoctor)
        .sort((a, b) => {
          const byDate = a.date.localeCompare(b.date);
          if (byDate !== 0) return byDate;
          return a.doctorName.localeCompare(b.doctorName);
        });

      return data;
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
