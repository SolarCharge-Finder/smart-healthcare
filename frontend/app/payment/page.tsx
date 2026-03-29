"use client";

import { useSearchParams } from "next/navigation";
import { Suspense, useEffect, useMemo, useRef, useState } from "react";
import { loadStripe } from "@stripe/stripe-js";
import { Elements } from "@stripe/react-stripe-js";
import PaymentForm from "../../components/forms/PaymentForm";
import PageHeader from "../../components/ui/PageHeader";
import { useStripeConfig } from "../../hooks/useStripeConfig";
import { useCreatePaymentIntent } from "../../hooks/usePayment";
import Alert from "../../components/ui/Alert";

function PaymentPageContent() {
  const searchParams = useSearchParams();
  const [appointmentId, setAppointmentId] = useState<string>("");
  const [amount, setAmount] = useState<number>(0);
  const [currency, setCurrency] = useState<string>("lkr");
  const { data: config, isLoading: configLoading, error: configError } = useStripeConfig();
  const [stripePromise, setStripePromise] = useState(null);
  const [clientSecret, setClientSecret] = useState<string | null>(null);
  const [initError, setInitError] = useState<string | null>(null);
  const [initInfo, setInitInfo] = useState<string | null>(null);
  const [isInitializing, setIsInitializing] = useState(true);
  const intentRequestedRef = useRef(false);

  const createPaymentIntent = useCreatePaymentIntent();

  useEffect(() => {
    const apt = searchParams.get("appointmentId");

    if (!apt) {
      setAppointmentId("");
      setInitError("Missing appointmentId in URL.");
      setIsInitializing(false);
      return;
    }

    setAppointmentId(apt);
    setInitError(null);
  }, [searchParams]);

  // Initialize Stripe once config is loaded
  useEffect(() => {
    if (config?.publishableKey && !stripePromise) {
      (async () => {
        const stripe = await loadStripe(config.publishableKey);
        setStripePromise(stripe as any);
      })();
    }
  }, [config, stripePromise]);

  // Create payment intent once we have appointment details and Stripe is loaded
  useEffect(() => {
    if (
      appointmentId &&
      stripePromise &&
      !clientSecret &&
      !intentRequestedRef.current
    ) {
      intentRequestedRef.current = true;
      const createIntent = async () => {
        try {
          setIsInitializing(true);
          const response = await createPaymentIntent.mutateAsync({
            appointmentId,
          });
          const normalizedStatus = (response.status || "").toLowerCase();

          if (normalizedStatus === "succeeded") {
            setInitInfo("This appointment payment is already completed.");
            setClientSecret(null);
          } else if (!response.clientSecret) {
            setInitError("Payment initialization failed: missing client secret.");
            setClientSecret(null);
            intentRequestedRef.current = false;
          } else {
            setClientSecret(response.clientSecret);
            setAmount(response.amount);
            setCurrency(response.currency);
            setInitInfo(null);
            setInitError(null);
          }
        } catch (err) {
          intentRequestedRef.current = false;
          setInitError(
            err instanceof Error
              ? err.message
              : "Failed to initialize payment"
          );
        } finally {
          setIsInitializing(false);
        }
      };

      createIntent();
    }
  }, [appointmentId, stripePromise, clientSecret, createPaymentIntent]);

  useEffect(() => {
    // Allow a fresh intent request when appointment changes.
    intentRequestedRef.current = false;
  }, [appointmentId]);

  const isReady = !configLoading && !isInitializing && stripePromise && clientSecret;
  const elementsOptions = useMemo(
    () =>
      clientSecret
        ? {
            clientSecret,
            appearance: {
              theme: "stripe" as const,
            },
          }
        : undefined,
    [clientSecret]
  );

  if (configError || initError) {
    return (
      <main className="flex flex-col max-w-6xl min-h-screen gap-6 px-6 py-10 mx-auto">
        <PageHeader
          title="Secure Payment"
          subtitle="Complete your payment with trusted providers."
        />
        <Alert type="error">
          {configError ? "Failed to load payment configuration." : initError}
        </Alert>
      </main>
    );
  }

  if (initInfo) {
    return (
      <main className="flex flex-col max-w-6xl min-h-screen gap-6 px-6 py-10 mx-auto">
        <PageHeader
          title="Secure Payment"
          subtitle="Complete your payment with trusted providers."
        />
        <Alert type="info">{initInfo}</Alert>
      </main>
    );
  }

  if (!isReady) {
    return (
      <main className="flex flex-col max-w-6xl min-h-screen gap-6 px-6 py-10 mx-auto">
        <PageHeader
          title="Secure Payment"
          subtitle="Complete your payment with trusted providers."
        />
        <div className="flex items-center justify-center min-h-96">
          <div className="text-center">
            <div className="inline-block mb-4">
              <div className="w-8 h-8 border-4 border-blue-200 rounded-full border-t-blue-600 animate-spin"></div>
            </div>
            <div className="text-gray-600">Initializing payment system...</div>
          </div>
        </div>
      </main>
    );
  }

  return (
    <main className="flex flex-col max-w-6xl min-h-screen gap-6 px-6 py-10 mx-auto">
      <PageHeader
        title="Secure Payment"
        subtitle="Complete your payment with trusted providers."
      />
      <div className="max-w-xl">
        <Elements 
          stripe={stripePromise} 
          key={clientSecret}
          options={elementsOptions}
        >
          <PaymentForm
            appointmentId={appointmentId}
            amount={amount}
            currency={currency}
          />
        </Elements>
      </div>
    </main>
  );
}

export default function PaymentPage() {
  return (
    <Suspense
      fallback={
        <main className="flex flex-col max-w-6xl min-h-screen gap-6 px-6 py-10 mx-auto">
          <PageHeader
            title="Secure Payment"
            subtitle="Complete your payment with trusted providers."
          />
          <div className="flex items-center justify-center min-h-96">
            <div className="text-gray-600">Loading payment details...</div>
          </div>
        </main>
      }
    >
      <PaymentPageContent />
    </Suspense>
  );
}
