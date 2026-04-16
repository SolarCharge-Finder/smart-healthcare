import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import apiClient from '@/shared/apiClient';
import { PendingAdminDto, PendingDoctorDto } from '../types/admin';

export function useAdmin() {
  const queryClient = useQueryClient();

  // get pending admin requests
  const pendingAdminsQuery = useQuery({
    queryKey: ['pending-admins'],
    queryFn: async (): Promise<PendingAdminDto[]> => {
      const res = await apiClient.get('/admin/pending');

      return res.data.map((a: any) => ({
        id: a.id,
        fullName: a.fullName ?? `${a.firstName} ${a.lastName}`,
      }));
    },
  });

  // approve admin
  const approveAdmin = useMutation({
    mutationFn: async (id: string) => {
      await apiClient.put(`/admin/${id}/approve`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['pending-admins'] });
    },
  });

  // reject admin
  const rejectAdmin = useMutation({
    mutationFn: async (id: string) => {
      await apiClient.delete(`/admin/${id}/reject`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['pending-admins'] });
    },
  });

  // pending doctors
  const pendingDoctorsQuery = useQuery({
    queryKey: ['pending-doctors'],
    queryFn: async (): Promise<PendingDoctorDto[]> => {
      const res = await apiClient.get('/admin/doctors/pending');

      return res.data.map((d: any) => ({
        id: d.id,
        fullName: d.fullName ?? `${d.firstName} ${d.lastName}`,
      }));
    },
  });

  // approve doctor
  const approveDoctor = useMutation({
    mutationFn: async (id: string) => {
      await apiClient.put(`/admin/doctors/${id}/approve`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['pending-doctors'] });
    },
  });

  return {
    pendingAdminsQuery,
    approveAdmin,
    rejectAdmin,
    pendingDoctorsQuery,
    approveDoctor,
  };
}
