import { useQuery } from "@tanstack/react-query";
import api from "../lib/api";
import {
  DoctorFilterOptions,
  DoctorSearchResult,
} from "../types/doctor";

type DoctorSearchParams = {
  doctorName?: string;
  specialization?: string;
  hospitalId?: string;
  date?: string;
  lookAheadDays?: number;
};

function formatIsoDate(date: Date) {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}

function addDays(baseDate: Date, days: number) {
  const next = new Date(baseDate);
  next.setDate(next.getDate() + days);
  return next;
}

export function useDoctorFilterOptions() {
  return useQuery<DoctorFilterOptions>({
    queryKey: ["doctor-filter-options"],
    queryFn: async () => {
      const { data } = await api.get<DoctorFilterOptions>("/doctors/filter-options");
      return data;
    },
  });
}

export function useDoctorSearch(params: DoctorSearchParams) {
  const lookAheadDays = params.lookAheadDays ?? 7;

  return useQuery<DoctorSearchResult[]>({
    queryKey: ["doctor-search", params, lookAheadDays],
    enabled: Boolean(params.date),
    queryFn: async () => {
      const selectedDate = params.date ? new Date(`${params.date}T00:00:00`) : null;

      if (!selectedDate || Number.isNaN(selectedDate.getTime())) {
        return [];
      }

      const requests = Array.from({ length: lookAheadDays + 1 }, (_, offset) => {
        const targetDate = formatIsoDate(addDays(selectedDate, offset));

        return api.get<DoctorSearchResult[]>("/doctors/search", {
          params: {
            doctorName: params.doctorName || undefined,
            specialization: params.specialization || undefined,
            hospitalId: params.hospitalId || undefined,
            date: targetDate,
          },
        });
      });

      const responses = await Promise.all(requests);

      const data = responses
        .flatMap((response) => response.data)
        .sort((a, b) => {
          const byDate = a.date.localeCompare(b.date);
          if (byDate !== 0) return byDate;
          return a.doctorName.localeCompare(b.doctorName);
        });

      return data;
    },
  });
}

export function useDoctorAvailability(doctorId?: string, date?: string) {
  return useQuery<{ availableSlots: string[] }>({
    queryKey: ["doctor-availability", doctorId, date],
    enabled: Boolean(doctorId && date),
    queryFn: async () => {
      const { data } = await api.get<{ availableSlots: string[] }>(
        "/doctors/availability",
        {
          params: {
            doctorId,
            date,
          },
        }
      );
      return data;
    },
  });
}
