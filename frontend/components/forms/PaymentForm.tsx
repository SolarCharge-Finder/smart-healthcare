"use client";

import { useState } from "react";
import {
  PaymentElement,
  useStripe,
  useElements,
} from "@stripe/react-stripe-js";
import Card from "../ui/Card";
import Button from "../ui/Button";
import Alert from "../ui/Alert";
import Input from "../ui/Input";
import { getPaymentByAppointmentId, useConfirmPayment } from "../../hooks/usePayment";
import { Payment } from "../../types/payment";


interface PaymentFormProps {
  appointmentId: string;
  paymentId: string;
  amount: number;
  currency: string;
  onPaymentSuccess?: (payment: Payment) => void;
}

function isSuccessfulPaymentStatus(status: string | undefined | null) {
  const normalized = (status ?? "").toLowerCase();
  return normalized === "succeeded" || normalized === "success" || normalized === "complete" || normalized === "completed";
}

export default function PaymentForm({
  appointmentId,
  paymentId,
  amount,
  currency,
  onPaymentSuccess,
}: PaymentFormProps) {
  const stripe = useStripe();
  const elements = useElements();
  const { mutateAsync: confirmPaymentAsync } = useConfirmPayment();
  const [email, setEmail] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [status, setStatus] = useState<{
    type: "success" | "error" | "info";
    message: string;
  } | null>(null);


  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!stripe || !elements) {
      setStatus({
        type: "error",
        message: "Payment system not ready. Please refresh the page.",
      });
      return;
    }

    if (!email) {
      setStatus({
        type: "error",
        message: "Email is required",
      });
      return;
    }

    setIsLoading(true);

    try {
      console.log("Confirming payment with Stripe...");

      const confirmResult = await stripe.confirmPayment({
        elements,
        confirmParams: {
          return_url: `${window.location.origin}/payment`,
          payment_method_data: {
            billing_details: {
              email,
            },
          },
        },
        redirect: "if_required",
      });

      console.log("Confirmation result:", confirmResult);

      const { error, paymentIntent } = confirmResult;

      if (error) {
        console.error("Stripe error:", error);

        setStatus({
          type: "error",
          message: error.message || "Payment failed",
        });
      } else if (paymentIntent) {
        console.log("Payment intent status:", paymentIntent.status);

        if (isSuccessfulPaymentStatus(paymentIntent.status)) {
          let resolvedPaymentId = paymentId;

          if (!resolvedPaymentId) {
            try {
              const payment = await getPaymentByAppointmentId(appointmentId);
              resolvedPaymentId = payment.id;
            } catch (lookupError) {
              console.error("Failed to resolve payment record from appointment:", lookupError);
            }
          }

          if (!resolvedPaymentId) {
            setStatus({
              type: "error",
              message: "Payment completed in Stripe, but the app could not find the stored payment record.",
            });
            return;
          }

          const confirmedPayment = await confirmPaymentAsync({
            paymentId: resolvedPaymentId,
            payload: { isSuccess: true },
          });

          if (!isSuccessfulPaymentStatus(confirmedPayment.status)) {
            setStatus({
              type: "error",
              message: `Payment was not saved as successful. Current status: ${confirmedPayment.status}`,
            });
            return;
          }

          setStatus({
            type: "success",
            message: "Payment completed successfully. Your doctor channeling summary is ready below.",
          });

          onPaymentSuccess?.(confirmedPayment);
          setEmail("");

        } else if (paymentIntent.status === "processing") {
          setStatus({
            type: "info",
            message: "Payment is processing. Please wait...",
          });
        } else if (paymentIntent.status === "requires_payment_method") {
          setStatus({
            type: "error",
            message: "Payment requires a payment method. Please try again with a valid card.",
          });
        } else {
          setStatus({
            type: "info",
            message: `Payment status: ${paymentIntent.status}`,
          });
        }
      } else {
        setStatus({
          type: "error",
          message: "Unexpected response from Stripe. Please try again.",
        });
      }
    } catch (err) {
      console.error("Payment error caught:", err);
      setStatus({
        type: "error",
        message:
          err instanceof Error
            ? err.message
            : "An unexpected error occurred during payment",
      });
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <Card title="Payment Details">
      <form onSubmit={handleSubmit} className="space-y-6">
        {/* Payment amount info */}
        <div className="p-4 rounded-lg bg-blue-50">
          <div className="text-sm font-medium text-gray-700">
            Amount to Pay
          </div>
          <div className="mt-1 text-2xl font-bold text-blue-600">
            {(amount / 100).toFixed(2)} {currency.toUpperCase()}
          </div>
        </div>

        {/* Email */}
        <Input
          label="Email Address"
          type="email"
          placeholder="you@example.com"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          required
          disabled={isLoading}
        />

        {/* Stripe Payment Element */}
        <div className="p-4 border border-gray-300 rounded-lg">
          <PaymentElement
            options={{
              layout: "tabs",
            }}
          />
        </div>

        {/* Status messages */}
        {status && (
          <Alert type={status.type}>
            {status.message}
          </Alert>
        )}

        {/* Submit button */}
        <Button
          type="submit"
          disabled={
            isLoading || !stripe || !elements || !email
          }
          className="w-full"
        >
          {isLoading ? "Processing..." : "Pay Now"}
        </Button>

        {/* Security notice */}
        <div className="text-xs text-center text-gray-500">
          Your payment is secure and encrypted with Stripe
        </div>
      </form>
    </Card>
  );
}
