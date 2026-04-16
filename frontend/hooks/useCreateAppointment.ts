import { useMutation, useQueryClient } from '@tanstack/react-query';
import api from '../lib/api';
import { Appointment } from '../types/appointment';

export type CreateAppointmentPayload = {
  patientId?: string;
  userId?: string;
  doctorId: string;
  doctorName?: string;
  hospitalId?: string;
  hospitalName?: string;
  specialization?: string;
  slotTime: string;
  guest?: {
    fullName: string;
    email: string;
    phoneNumber: string;
    area: string;
    nicOrPassport: string;
  };
};

export function useCreateAppointment() {
  const queryClient = useQueryClient();

  return useMutation<Appointment, unknown, CreateAppointmentPayload>({
    mutationFn: async (payload) => {
      const { data } = await api.post<Appointment>('/appointments', payload);
      return data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['appointments'] });
    },
  });
}
