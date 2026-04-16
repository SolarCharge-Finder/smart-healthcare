import { useQuery } from '@tanstack/react-query';
import api from '../lib/api';
import { Appointment } from '../types/appointment';

export function useAppointments() {
  return useQuery<Appointment[]>({
    queryKey: ['appointments'],
    queryFn: async () => {
      const { data } = await api.get<Appointment[]>('/appointments');
      return data;
    },
  });
}
