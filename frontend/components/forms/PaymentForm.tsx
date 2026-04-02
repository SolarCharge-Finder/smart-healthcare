"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import {
  PaymentElement,
  useStripe,
  useElements,
} from "@stripe/react-stripe-js";
import Card from "../ui/Card";
import Button from "../ui/Button";
import Alert from "../ui/Alert";
import Input from "../ui/Input";


interface PaymentFormProps {
  appointmentId: string;
  amount: number;
  currency: string;
}

export default function PaymentForm({
  appointmentId,
  amount,
  currency,
}: PaymentFormProps) {
  const stripe = useStripe();
  const elements = useElements();
  const router = useRouter();
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
      
      // Confirm payment with Stripe
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
        
        if (paymentIntent.status === "succeeded") {
          setStatus({
            type: "success",
            message: "✅ Payment successful! Redirecting to your video consultation...",
          });
          setEmail("");
          // Give the payment webhook a short moment to confirm the appointment before redirecting.
          setTimeout(() => {
            router.push(`/consultation?appointmentId=${appointmentId}`);
          }, 3000);

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
        <div className="rounded-lg bg-blue-50 p-4">
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
        <div className="rounded-lg border border-gray-300 p-4">
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
        <div className="text-center text-xs text-gray-500">
          Your payment is secure and encrypted with Stripe
        </div>
      </form>
    </Card>
  );
}
