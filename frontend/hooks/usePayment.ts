import { useMutation, useQueryClient } from "@tanstack/react-query";
import api from "../lib/api";
import {
  CreatePaymentIntentResponse,
  ConfirmPaymentRequest,
} from "../types/payment";

export interface CreatePaymentIntentPayload {
  appointmentId: string;
}

export function useCreatePaymentIntent() {
  return useMutation<CreatePaymentIntentResponse, unknown, CreatePaymentIntentPayload>({
    mutationFn: async (payload) => {
      const { data } = await api.post<CreatePaymentIntentResponse>(
        "/payments/intents",
        payload
      );
      return data;
    },
  });
}

export function useConfirmPayment() {
  const queryClient = useQueryClient();

  return useMutation<void, unknown, { paymentId: string; payload: ConfirmPaymentRequest }>({
    mutationFn: async ({ paymentId, payload }) => {
      await api.post(`/payments/${paymentId}/confirm`, payload);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["payments"] });
    },
  });
}
