import { useMutation, useQueryClient } from '@tanstack/react-query';
import api from '../lib/api';
import { Appointment } from '../types/appointment';

export function useCancelAppointment() {
  const queryClient = useQueryClient();

  return useMutation<void, unknown, string>({
    mutationFn: async (id) => {
      await api.delete(`/appointments/${id}`);
    },
    onSuccess: (_, id) => {
      queryClient.setQueryData<Appointment[] | undefined>(['appointments'], (current) =>
        current?.filter((appointment) => {
          const status = appointment.status.toUpperCase();
          return appointment.id !== id && status !== 'CANCELLED';
        }),
      );

      queryClient.invalidateQueries({ queryKey: ['appointments'] });
    },
  });
}
