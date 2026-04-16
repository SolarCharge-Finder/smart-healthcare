'use client';

import { useSearchParams } from 'next/navigation';
import { Suspense, useEffect, useMemo, useRef, useState } from 'react';
import { loadStripe } from '@stripe/stripe-js';
import { Elements } from '@stripe/react-stripe-js';
import PaymentForm from '../../components/forms/PaymentForm';
import PageHeader from '../../components/ui/PageHeader';
import { useStripeConfig } from '../../hooks/useStripeConfig';
import { useCreatePaymentIntent } from '../../hooks/usePayment';
import Alert from '../../components/ui/Alert';
import Card from '../../components/ui/Card';
import Button from '../../components/ui/Button';
import { useQuery } from '@tanstack/react-query';
import Link from 'next/link';
import api from '../../lib/api';
import { generateRecipePdf } from '../../lib/recipePdf';
import { Payment } from '../../types/payment';
import { Appointment } from '../../types/appointment';

function isSuccessfulPaymentStatus(status: string | undefined | null) {
  const normalized = (status ?? '').toLowerCase();
  return (
    normalized === 'succeeded' ||
    normalized === 'success' ||
    normalized === 'complete' ||
    normalized === 'completed'
  );
}

function PaymentSuccessBanner() {
  return (
    <div className="rounded-xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-emerald-800">
      <div className="flex items-center gap-3">
        <span className="inline-flex h-8 w-8 items-center justify-center rounded-full bg-emerald-600 text-white">
          <svg
            xmlns="http://www.w3.org/2000/svg"
            viewBox="0 0 20 20"
            fill="currentColor"
            className="h-5 w-5"
            aria-hidden="true"
          >
            <path
              fillRule="evenodd"
              d="M16.704 5.29a1 1 0 010 1.414l-7.16 7.16a1 1 0 01-1.414 0l-3.16-3.16a1 1 0 011.415-1.414l2.452 2.452 6.452-6.452a1 1 0 011.415 0z"
              clipRule="evenodd"
            />
          </svg>
        </span>
        <p className="text-sm font-semibold">Payment successful</p>
      </div>
    </div>
  );
}

function PaymentPageContent() {
  const searchParams = useSearchParams();
  const [appointmentId, setAppointmentId] = useState<string>('');
  const [amount, setAmount] = useState<number>(0);
  const [currency, setCurrency] = useState<string>('lkr');
  const [paymentId, setPaymentId] = useState<string>('');
  const { data: config, isLoading: configLoading, error: configError } = useStripeConfig();
  const [stripePromise, setStripePromise] = useState(null);
  const [clientSecret, setClientSecret] = useState<string | null>(null);
  const [initError, setInitError] = useState<string | null>(null);
  const [initInfo, setInitInfo] = useState<string | null>(null);
  const [isInitializing, setIsInitializing] = useState(true);
  const [confirmedPayment, setConfirmedPayment] = useState<Payment | null>(null);
  const flow = searchParams.get('flow');
  const isVideoConsultationFlow = flow === 'video';
  // Single stable ref tracks which appointmentId has been processed to prevent double-fire
  const processedAppointmentRef = useRef<string | null>(null);
  const inFlightRef = useRef(false);

  const { mutateAsync: createPaymentIntentAsync } = useCreatePaymentIntent();

  const appointment = useQuery<Appointment>({
    queryKey: ['appointment', appointmentId],
    enabled: Boolean(appointmentId),
    queryFn: async () => {
      const { data } = await api.get<Appointment>(`/appointments/${appointmentId}`);
      return data;
    },
  });

  // Step 1: Read appointmentId from URL
  useEffect(() => {
    const apt = searchParams.get('appointmentId');

    if (!apt) {
      setAppointmentId('');
      setInitError('Missing appointmentId in URL.');
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
    setCurrency('lkr');
    setPaymentId('');
    setInitInfo(null);
    setInitError(null);
    setIsInitializing(true);

    const createIntent = async () => {
      try {
        const response = await createPaymentIntentAsync({ appointmentId });

        if (isSuccessfulPaymentStatus(response.status)) {
          setInitInfo('This appointment payment is already completed. Your consultation is ready.');
          setClientSecret(null);
        } else if (!response.clientSecret) {
          setInitError('Payment initialization failed: missing client secret.');
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
        console.error('Failed to create payment intent', err);
        setInitError(err instanceof Error ? err.message : 'Failed to initialize payment');
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
              theme: 'stripe' as const,
            },
          }
        : undefined,
    [clientSecret],
  );

  const downloadRecipePdf = async () => {
    if (!confirmedPayment) return;
    const amountPaid = `${(confirmedPayment.amount / 100).toFixed(2)} ${confirmedPayment.currency.toUpperCase()}`;
    const channelDateTime = appointment.data?.slotTime
      ? new Date(appointment.data.slotTime).toLocaleString()
      : '-';

    const userRows: Array<[string, string]> = [
      ['Patient ID', appointment.data?.patientId ?? '-'],
      ['User ID', appointment.data?.userId ?? appointment.data?.guestUserId ?? '-'],
      ['Guest Name', appointment.data?.guestUser?.fullName ?? '-'],
      ['Guest Email', appointment.data?.guestUser?.email ?? '-'],
      ['Guest Phone', appointment.data?.guestUser?.phoneNumber ?? '-'],
    ];

    const doctorRows: Array<[string, string]> = [
      ['Doctor', appointment.data?.doctorName ?? '-'],
      ['Specialization', appointment.data?.specialization ?? '-'],
      ['Hospital', appointment.data?.hospitalName ?? '-'],
      ['Channeling Date/Time', channelDateTime],
      ['Booking Reference', appointment.data?.bookingReferenceId ?? '-'],
    ];

    const paymentRows: Array<[string, string]> = [
      [
        'My Appointment Number',
        appointment.data?.appointmentNumber ? `#${appointment.data.appointmentNumber}` : '-',
      ],
      ['Appointment ID', appointmentId],
      ['Payment ID', confirmedPayment.id],
      ['Status', confirmedPayment.status?.toUpperCase() ?? 'CONFIRMED'],
      ['Amount Paid', amountPaid],
    ];

    await generateRecipePdf({
      fileId: appointmentId,
      userDetails: userRows,
      doctorDetails: doctorRows,
      paymentSummary: paymentRows,
    });
  };

  useEffect(() => {
    if (!confirmedPayment || !appointmentId || !isVideoConsultationFlow) return;

    if (typeof window === 'undefined') return;

    try {
      const storageKey = 'video-consultation-payments';
      const raw = window.localStorage.getItem(storageKey);
      const parsed = raw ? JSON.parse(raw) : {};

      const nextMap = parsed && typeof parsed === 'object' ? (parsed as Record<string, true>) : {};

      nextMap[appointmentId] = true;
      window.localStorage.setItem(storageKey, JSON.stringify(nextMap));
    } catch {
      // Ignore storage failures; payment flow should continue.
    }
  }, [confirmedPayment, appointmentId, isVideoConsultationFlow]);

  if (configError || initError) {
    return (
      <main className="flex flex-col max-w-6xl min-h-screen gap-6 px-6 py-10 mx-auto">
        <PageHeader
          title="Secure Payment"
          subtitle="Complete your payment with trusted providers."
        />
        <Alert type="error">
          {configError ? 'Failed to load payment configuration.' : initError}
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
        <PaymentSuccessBanner />
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
      <PageHeader title="Secure Payment" subtitle="Complete your payment with trusted providers." />

      {!confirmedPayment ? (
        <div className="max-w-xl">
          <Elements stripe={stripePromise} key={clientSecret} options={elementsOptions}>
            <PaymentForm
              appointmentId={appointmentId}
              paymentId={paymentId}
              amount={amount}
              currency={currency}
              onPaymentSuccess={setConfirmedPayment}
            />
          </Elements>
        </div>
      ) : (
        <div className="space-y-4">
          <PaymentSuccessBanner />

          <Card title="Doctor Channelling Summary">
            <div className="grid gap-2 text-sm text-gray-700 md:grid-cols-2">
              <p>
                <span className="font-semibold">Appointment ID:</span> {appointmentId}
              </p>
              <p>
                <span className="font-semibold">Appointment Number:</span>{' '}
                {appointment.data?.appointmentNumber
                  ? `#${appointment.data.appointmentNumber}`
                  : '-'}
              </p>
              <p>
                <span className="font-semibold">Payment ID:</span> {confirmedPayment.id}
              </p>
              <p>
                <span className="font-semibold">Doctor:</span> {appointment.data?.doctorName ?? '-'}
              </p>
              <p>
                <span className="font-semibold">Hospital:</span>{' '}
                {appointment.data?.hospitalName ?? '-'}
              </p>
              <p>
                <span className="font-semibold">Specialization:</span>{' '}
                {appointment.data?.specialization ?? '-'}
              </p>
              <p>
                <span className="font-semibold">Channeling Date/Time:</span>{' '}
                {appointment.data?.slotTime
                  ? new Date(appointment.data.slotTime).toLocaleString()
                  : '-'}
              </p>
              <p>
                <span className="font-semibold">Booking Reference:</span>{' '}
                {appointment.data?.bookingReferenceId ?? '-'}
              </p>
              <p>
                <span className="font-semibold">Status:</span>{' '}
                {confirmedPayment.status?.toUpperCase() ?? appointment.data?.status ?? 'CONFIRMED'}
              </p>
              <p>
                <span className="font-semibold">Amount Paid:</span>{' '}
                {(confirmedPayment.amount / 100).toFixed(2)}{' '}
                {confirmedPayment.currency.toUpperCase()}
              </p>
            </div>

            <div className="mt-5 flex flex-wrap gap-3">
              <Link href="/appointments/history" className="inline-block">
                <Button variant="secondary">View Appointment History</Button>
              </Link>
              {isVideoConsultationFlow ? (
                <Link href={`/consultation/${appointmentId}`} className="inline-block">
                  <Button>Join Consultation</Button>
                </Link>
              ) : (
                <Button onClick={downloadRecipePdf}>Download recipe</Button>
              )}
            </div>
          </Card>
        </div>
      )}
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
