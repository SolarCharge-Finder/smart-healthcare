import apiClient from '../../shared/apiClient';

export type UserResponse = {
  id: string;
  name: string;
  email?: string;
  role: string;
};

export const createPatient = async (payload: { fullName: string; email: string }) => {
  const { data } = await apiClient.post<UserResponse>('/patient', payload);
  return data;
};

export const createDoctor = async (payload: {
  fullName: string;
  specialization: string;
  hospital: string;
}) => {
  const { data } = await apiClient.post<UserResponse>('/doctors', payload);
  return data;
};

export const createAdmin = async (payload: { fullName: string }) => {
  const { data } = await apiClient.post<UserResponse>('/admin', payload);
  return data;
};
