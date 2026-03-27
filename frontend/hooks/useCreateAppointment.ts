import { useMutation, useQueryClient } from "@tanstack/react-query";
import api from "../lib/api";
import { Appointment } from "../types/appointment";

export type CreateAppointmentPayload = {
  patientId: string;
  doctorId: string;
  slotTime: string;
};

export function useCreateAppointment() {
  const queryClient = useQueryClient();

  return useMutation<Appointment, unknown, CreateAppointmentPayload>({
    mutationFn: async (payload) => {
      const { data } = await api.post<Appointment>(
        "/appointments",
        payload
      );
      return data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["appointments"] });
    }
  });
}
