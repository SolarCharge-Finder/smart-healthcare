import { useMutation, useQueryClient } from "@tanstack/react-query";
import api from "../lib/api";

export function useCancelAppointment() {
  const queryClient = useQueryClient();

  return useMutation<void, unknown, string>({
    mutationFn: async (id) => {
      await api.delete(`/appointments/${id}`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["appointments"] });
    }
  });
}
