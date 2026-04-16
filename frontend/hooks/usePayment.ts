import { useMutation, useQueryClient } from '@tanstack/react-query';
import axios from 'axios';
import { paymentApi } from '../lib/api';
import { Payment, CreatePaymentIntentResponse, ConfirmPaymentRequest } from '../types/payment';

export interface CreatePaymentIntentPayload {
  appointmentId: string;
}

function extractApiError(error: unknown, fallbackMessage: string): Error {
  if (axios.isAxiosError(error)) {
    const detail = error.response?.data?.detail;
    const title = error.response?.data?.title;
    const message =
      (typeof detail === 'string' && detail.trim()) ||
      (typeof title === 'string' && title.trim()) ||
      (typeof error.response?.data === 'string' && error.response.data.trim()) ||
      (typeof error.message === 'string' && error.message.trim()) ||
      fallbackMessage;

    return new Error(message);
  }

  if (error instanceof Error && error.message.trim()) {
    return new Error(error.message);
  }

  return new Error(fallbackMessage);
}

export function useCreatePaymentIntent() {
  return useMutation<CreatePaymentIntentResponse, Error, CreatePaymentIntentPayload>({
    mutationFn: async (payload) => {
      try {
        const { data } = await paymentApi.post<CreatePaymentIntentResponse>(
          '/payments/intents',
          payload,
        );
        return data;
      } catch (error) {
        throw extractApiError(error, 'Failed to create payment intent.');
      }
    },
  });
}

export function useConfirmPayment() {
  const queryClient = useQueryClient();

  return useMutation<Payment, Error, { paymentId: string; payload: ConfirmPaymentRequest }>({
    mutationFn: async ({ paymentId, payload }) => {
      try {
        const { data } = await paymentApi.post<Payment>(`/payments/${paymentId}/confirm`, payload);
        return data;
      } catch (error) {
        throw extractApiError(error, 'Failed to confirm payment.');
      }
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['payments'] });
      queryClient.invalidateQueries({ queryKey: ['appointments'] });
    },
  });
}

export async function getPaymentByAppointmentId(appointmentId: string) {
  const { data } = await paymentApi.get<Payment>(`/payments/appointment/${appointmentId}`);
  return data;
}
