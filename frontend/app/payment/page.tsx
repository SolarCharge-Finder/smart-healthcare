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

function isSuccessfulPaymentStatus(status: string | undefined | null) {
  const normalized = (status ?? "").toLowerCase();
  return normalized === "succeeded" || normalized === "success" || normalized === "complete" || normalized === "completed";
}

function PaymentPageContent() {
  const searchParams = useSearchParams();
  const [appointmentId, setAppointmentId] = useState<string>("");
  const [amount, setAmount] = useState<number>(0);
  const [currency, setCurrency] = useState<string>("lkr");
  const [paymentId, setPaymentId] = useState<string>("");
  const { data: config, isLoading: configLoading, error: configError } = useStripeConfig();
  const [stripePromise, setStripePromise] = useState(null);
  const [clientSecret, setClientSecret] = useState<string | null>(null);
  const [initError, setInitError] = useState<string | null>(null);
  const [initInfo, setInitInfo] = useState<string | null>(null);
  const [isInitializing, setIsInitializing] = useState(true);
  // Single stable ref tracks which appointmentId has been processed to prevent double-fire
  const processedAppointmentRef = useRef<string | null>(null);
  const inFlightRef = useRef(false);

  const { mutateAsync: createPaymentIntentAsync } = useCreatePaymentIntent();

  // Step 1: Read appointmentId from URL
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

  // Step 2: Initialize Stripe once config is loaded
  useEffect(() => {
    if (config?.publishableKey && !stripePromise) {
      (async () => {
        const stripe = await loadStripe(config.publishableKey);
        setStripePromise(stripe as any);
      })();
    }
  }, [config, stripePromise]);

  // Step 3: Create payment intent — guarded by processedAppointmentRef so it fires exactly once
  // per unique appointmentId, avoiding the previous race condition where the reset effect was
  // clearing clientSecret after intent was already created.
  useEffect(() => {
    if (
      !appointmentId ||
      !stripePromise ||
      inFlightRef.current ||
      processedAppointmentRef.current === appointmentId
    ) {
      return;
    }

    // Mark as in-flight for this appointmentId
    processedAppointmentRef.current = appointmentId;
    inFlightRef.current = true;

    // Reset display state for a fresh appointment
    setClientSecret(null);
    setAmount(0);
    setCurrency("lkr");
    setPaymentId("");
    setInitInfo(null);
    setInitError(null);
    setIsInitializing(true);

    const createIntent = async () => {
      try {
        const response = await createPaymentIntentAsync({ appointmentId });

        if (isSuccessfulPaymentStatus(response.status)) {
          setInitInfo("This appointment payment is already completed. Your consultation is ready.");
          setClientSecret(null);
        } else if (!response.clientSecret) {
          setInitError("Payment initialization failed: missing client secret.");
          setClientSecret(null);
        } else {
          setPaymentId(response.paymentId);
          setClientSecret(response.clientSecret);
          setAmount(response.amount);
          setCurrency(response.currency);
          setInitInfo(null);
          setInitError(null);
        }
      } catch (err) {
        console.error("Failed to create payment intent", err);
        setInitError(
          err instanceof Error
            ? err.message
            : "Failed to initialize payment"
        );
        // Allow retry navigation by clearing processed ref
        processedAppointmentRef.current = null;
      } finally {
        inFlightRef.current = false;
        setIsInitializing(false);
      }
    };

    createIntent();
  }, [appointmentId, stripePromise, createPaymentIntentAsync]);

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
            paymentId={paymentId}
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
