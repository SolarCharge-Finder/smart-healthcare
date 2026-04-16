// modules/doctors/useDoctorProfile.ts

import { useQuery } from '@tanstack/react-query';
import apiClient from '@/shared/apiClient';

// doctor profile dto
export type DoctorProfile = {
  id: string; // doctorId
  userId: string;
  fullName: string;
  specialization: string;
  hospital: string;
  isApproved: boolean;
};

export function useDoctorProfile() {
  return useQuery({
    queryKey: ['doctor-profile'],
    queryFn: async () => {
      const { data } = await apiClient.get<DoctorProfile>('/doctors/me');
      return data;
    },
  });
}
