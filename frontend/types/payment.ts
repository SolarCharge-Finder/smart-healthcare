export interface CreatePaymentIntentResponse {
  paymentIntentId: string;
  clientSecret: string;
  amount: number;
  currency: string;
  status: string;
}

export interface ConfirmPaymentRequest {
  isSuccess: boolean;
  failureReason?: string;
}

export interface StripeConfigResponse {
  publishableKey: string;
}

export interface Payment {
  id: string;
  appointmentId: string;
  stripePaymentIntentId: string;
  amount: number;
  currency: string;
  status: string;
  failureReason?: string;
  createdAt: string;
  updatedAt: string;
}
