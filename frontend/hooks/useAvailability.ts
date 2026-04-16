import { useQuery } from '@tanstack/react-query';
import api from '../lib/api';
import { AvailabilitySlot } from '../types/availability';

export function useAvailability(doctorId?: string, fromDate?: string, toDate?: string) {
  return useQuery<AvailabilitySlot[]>({
    queryKey: ['availability', doctorId, fromDate, toDate],
    enabled: Boolean(doctorId && fromDate && toDate),
    queryFn: async () => {
      if (!doctorId || !fromDate || !toDate) return [];

      const from = new Date(`${fromDate}T00:00:00`);
      const to = new Date(`${toDate}T23:59:59`);

      const { data } = await api.get<AvailabilitySlot[]>(`/appointments/availability`, {
        params: {
          doctorId,
          from: from.toISOString(),
          to: to.toISOString(),
        },
      });
      return data;
    },
  });
}
