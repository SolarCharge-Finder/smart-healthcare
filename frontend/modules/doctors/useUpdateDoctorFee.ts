import { useMutation } from '@tanstack/react-query';
import apiClient from '@/shared/apiClient';

export function useUpdateDoctorFee() {
  return useMutation({
    mutationFn: async ({
      doctorId,
      fee,
    }: {
      doctorId: string;
      fee: number;
    }) => {
      await apiClient.put(`/doctors/${doctorId}/fee`, { fee });
    },
  });
}