import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import apiClient from '@/shared/apiClient';
import { CreateAvailabilityDto, Availability } from '../../../types/doctor';

export function useDoctorAvailability(doctorId: string | undefined) {
  const queryClient = useQueryClient();

  const availabilityQuery = useQuery<Availability[]>({
    queryKey: ['availability', doctorId],
    queryFn: async () => {
      const { data } = await apiClient.get(`/doctors/${doctorId}/availability`);
      return data;
    },
    enabled: !!doctorId,
  });

  const createAvailability = useMutation({
    mutationFn: async (payload: CreateAvailabilityDto) => {
      await apiClient.post(`/doctors/${doctorId}/availability`, payload);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['availability', doctorId] });
    },
  });

  const deactivateAvailability = useMutation({
    mutationFn: async ({ availabilityId }: { availabilityId: string }) => {
      await apiClient.patch(`/doctors/${doctorId}/availability/${availabilityId}/deactivate`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['availability', doctorId] });
    },
  });

  return {
    availabilityQuery,
    createAvailability,
    deactivateAvailability,
  };
}
