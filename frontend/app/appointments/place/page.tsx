'use client';

import { Suspense, useEffect, useMemo, useState } from 'react';
import { useSearchParams } from 'next/navigation';
import Link from 'next/link';
import { useQuery } from '@tanstack/react-query';
import axios from 'axios';
import Alert from '../../../components/ui/Alert';
import Button from '../../../components/ui/Button';
import Card from '../../../components/ui/Card';
import PageHeader from '../../../components/ui/PageHeader';
import PaymentSummary from '../../../components/booking/PaymentSummary';
import GuestForm, { GuestFormValue } from '../../../components/forms/GuestForm';
import { useCreateAppointment } from '../../../hooks/useCreateAppointment';
import { useDoctorAvailability } from '../../../hooks/useDoctorSearch';
import api from '../../../lib/api';
import { Appointment } from '../../../types/appointment';
import { useAuthContext } from '../../../modules/auth/AuthContext';
import { authStorage } from '../../../modules/auth/authStorage';
import { useDoctorAvailabilityByDate } from '@/hooks/useDoctorAvailabilityByDate';

type JwtPayload = {
  [key: string]: unknown;
};

function parseUserIdFromToken(token: string | null): string | null {
  if (!token) return null;

  try {
    const payload = token.split('.')[1];
    if (!payload) return null;

    const normalized = payload.replace(/-/g, '+').replace(/_/g, '/');

    const padded = normalized.padEnd(Math.ceil(normalized.length / 4) * 4, '=');

    const decoded = JSON.parse(atob(padded)) as JwtPayload;

    const claim = decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'];

    return typeof claim === 'string' ? claim : null;
  } catch {
    return null;
  }
}

function PlaceAppointmentContent() {
  const params = useSearchParams();
  const { user } = useAuthContext();
  const createAppointment = useCreateAppointment();

  const doctorId = params.get('doctorId') ?? '';
  const doctorName = params.get('doctorName') ?? '';
  const hospitalId = params.get('hospitalId') ?? '';
  const hospitalName = params.get('hospitalName') ?? '';
  const specialization = params.get('specialization') ?? '';
  const selectedDate = params.get('selectedDate') ?? '';
  const selectedTimeSlot = params.get('selectedTimeSlot') ?? '';
  const isTelemedicineFlow = params.get('telemedicine') === '1';

  const slotTime = useMemo(() => {
    if (!selectedDate || !selectedTimeSlot) return '';
    return `${selectedDate}T${selectedTimeSlot}:00Z`;
  }, [selectedDate, selectedTimeSlot]);

  const availability = useDoctorAvailabilityByDate(doctorId, selectedDate);

  const pricing = useQuery({
    queryKey: ['appointment-pricing', doctorId, hospitalId],
    enabled: Boolean(doctorId && hospitalId),
    queryFn: async () => {
      const { data } = await api.get('/appointments/pricing', {
        params: {
          doctorId,
          hospitalId,
        },
      });
      return data as {
        doctorFee: number;
        hospitalFee: number;
        eChannellingFee: number;
        discount: number;
        totalFee: number;
      };
    },
  });

  const myAppointments = useQuery<Appointment[]>({
    queryKey: ['appointments', 'my-booked-count'],
    enabled: Boolean(user),
    queryFn: async () => {
      const { data } = await api.get<Appointment[]>('/appointments');
      return data;
    },
  });

  const [guest, setGuest] = useState<GuestFormValue | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [bookedAppointment, setBookedAppointment] = useState<Appointment | null>(null);

  const isLoggedIn = Boolean(user);
  const canShowSummary = isLoggedIn || guest !== null;
  const slotAvailable = availability.data?.availableSlots?.includes(selectedTimeSlot) ?? false;
  const slotUnavailable = availability.isSuccess && !slotAvailable;

  const pricingSummary = useMemo(() => {
    if (!pricing.data) return null;

    const effectiveHospitalFee = isTelemedicineFlow ? 0 : pricing.data.hospitalFee;
    const effectiveTotal =
      pricing.data.doctorFee +
      effectiveHospitalFee +
      pricing.data.eChannellingFee -
      pricing.data.discount;

    return {
      doctorFee: pricing.data.doctorFee,
      hospitalFee: effectiveHospitalFee,
      eChannellingFee: pricing.data.eChannellingFee,
      discount: pricing.data.discount,
      totalFee: effectiveTotal,
    };
  }, [pricing.data, isTelemedicineFlow]);

  const myBookedAppointmentsCount = useMemo(() => {
    return (
      myAppointments.data?.filter((appointment) => appointment.status.toUpperCase() !== 'CANCELLED')
        .length ?? 0
    );
  }, [myAppointments.data]);

  useEffect(() => {
    setError(null);
    setBookedAppointment(null);
  }, [doctorId, hospitalId, selectedDate, selectedTimeSlot]);

  const onBook = async () => {
    setError(null);

    if (!slotTime) {
      setError('Invalid appointment date or time.');
      return;
    }

    if (slotUnavailable) {
      setError('Selected slot is no longer available. Please pick another slot.');
      return;
    }

    try {
      const userId = parseUserIdFromToken(authStorage.getToken());

      if (isLoggedIn && !userId) {
        setError('Your session is invalid. Please log in again.');
        return;
      }

      const appointment = await createAppointment.mutateAsync({
        userId: isLoggedIn ? (userId ?? undefined) : undefined,
        doctorId,
        doctorName,
        hospitalId,
        hospitalName,
        specialization,
        slotTime,
        guest: isLoggedIn ? undefined : (guest ?? undefined),
      });
      setBookedAppointment(appointment);
    } catch (err) {
      if (axios.isAxiosError(err)) {
        const responseData = err.response?.data;
        if (typeof responseData === 'string' && responseData.trim()) {
          setError(responseData);
          return;
        }

        const message = (responseData as { message?: string } | undefined)?.message;
        if (typeof message === 'string' && message.trim()) {
          setError(message);
          return;
        }
      }

      setError('Unable to book appointment. Please try another slot.');
    }
  };

  if (!doctorId || !doctorName || !hospitalId || !selectedDate || !selectedTimeSlot) {
    return (
      <main className="mx-auto flex min-h-screen max-w-4xl flex-col gap-6 px-6 py-10">
        <Alert type="error">
          Missing booking details. Please search and select a doctor again.
        </Alert>
      </main>
    );
  }

  return (
    <main className="mx-auto flex min-h-screen max-w-6xl flex-col gap-6 px-6 py-10">
      <PageHeader
        title="Place Appointment"
        subtitle="Review selected doctor details and confirm booking."
      />

      <Card title="Selected Doctor Information">
        <div className="grid gap-2 text-sm text-gray-700 md:grid-cols-2">
          <p>
            <span className="font-semibold">Doctor:</span> {doctorName}
          </p>
          <p>
            <span className="font-semibold">Hospital:</span> {hospitalName}
          </p>
          <p>
            <span className="font-semibold">Specialization:</span> {specialization}
          </p>
          <p>
            <span className="font-semibold">Date:</span> {selectedDate}
          </p>
          <p>
            <span className="font-semibold">Time Slot:</span> {selectedTimeSlot}
          </p>
          {isLoggedIn ? (
            <p>
              <span className="font-semibold">My Booked Appointments:</span>{' '}
              {myBookedAppointmentsCount}
            </p>
          ) : null}
        </div>
      </Card>

      {!isLoggedIn && !guest ? (
        <Card title="Guest Details">
          <GuestForm onSubmit={setGuest} />
        </Card>
      ) : null}

      {canShowSummary && pricingSummary ? (
        <PaymentSummary
          doctorFee={pricingSummary.doctorFee}
          hospitalFee={pricingSummary.hospitalFee}
          eChannellingFee={pricingSummary.eChannellingFee}
          discount={pricingSummary.discount}
          totalFee={pricingSummary.totalFee}
          hideHospitalFee={isTelemedicineFlow}
        />
      ) : null}

      {error ? <Alert type="error">{error}</Alert> : null}

      {bookedAppointment ? (
        <Alert type="success">
          Appointment booked successfully. Appointment Number: {bookedAppointment.appointmentNumber}
          . Click Pay Now to complete payment.
        </Alert>
      ) : null}

      {slotUnavailable ? (
        <Alert type="error">
          This slot has already been taken. Please return to results and pick another slot.
        </Alert>
      ) : null}

      {canShowSummary ? (
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div>
            {bookedAppointment ? (
              <Link
                href={`/payment?appointmentId=${bookedAppointment.id}&flow=${isTelemedicineFlow ? 'video' : 'book'}`}
                className="inline-block"
              >
                <Button variant="secondary">Pay Now</Button>
              </Link>
            ) : (
              <Button variant="secondary" disabled>
                Pay Now
              </Button>
            )}
          </div>

          <Button
            onClick={onBook}
            disabled={
              createAppointment.isPending ||
              pricing.isLoading ||
              availability.isLoading ||
              slotUnavailable ||
              Boolean(bookedAppointment)
            }
          >
            {createAppointment.isPending
              ? 'Booking...'
              : bookedAppointment
                ? 'Booked'
                : 'Book Appointment'}
          </Button>
        </div>
      ) : null}
    </main>
  );
}

export default function PlaceAppointmentPage() {
  return (
    <Suspense
      fallback={<main className="mx-auto min-h-screen max-w-6xl px-6 py-10">Loading...</main>}
    >
      <PlaceAppointmentContent />
    </Suspense>
  );
}
