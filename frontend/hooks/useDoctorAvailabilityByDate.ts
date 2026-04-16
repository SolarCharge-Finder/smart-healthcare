import { useQuery } from '@tanstack/react-query';
import api from '../lib/api';

export function useDoctorAvailabilityByDate(doctorId?: string, date?: string) {
  return useQuery<{ availableSlots: string[] }>({
    queryKey: ['doctor-availability-by-date', doctorId, date],
    enabled: !!doctorId && !!date,
    queryFn: async () => {
      const { data } = await api.get(
        `/doctors/${doctorId}/availability/by-date`,
        {
          params: { date },
        }
      );
      return data;
    },
  });
}