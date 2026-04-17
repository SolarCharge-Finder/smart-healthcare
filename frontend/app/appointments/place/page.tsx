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
import GuestForm, { GuestFormValue } from '../../../components/forms/GuestForm';
import { useCreateAppointment } from '../../../hooks/useCreateAppointment';
import api from '@/lib/api';
import { Appointment } from '../../../types/appointment';
import { useAuthContext } from '../../../modules/auth/AuthContext';
import { authStorage } from '../../../modules/auth/infra/authStorage';
import { useDoctorDetails } from '@/modules/doctors/hooks/useDoctorSearch';

function normalizeHospitalId(name: string) {
return name.trim().toUpperCase().replace(/\s+/g, '-');
}

function parseUserIdFromToken(token: string | null): string | null {
if (!token) return null;

try {
const payload = token.split('.')[1];
const decoded = JSON.parse(atob(payload));
return decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'];
} catch {
return null;
}
}

function PlaceAppointmentContent() {
const params = useSearchParams();
const { user } = useAuthContext();
const createAppointment = useCreateAppointment();

const doctorId = params.get('doctorId') ?? '';
const date = params.get('date') ?? '';
const time = params.get('time') ?? '';

// build UTC slot
const slotTime =
  date && time ? new Date(`${date}T${time}Z`).toISOString() : '';

const doctorQuery = useDoctorDetails(doctorId);

const doctorName = doctorQuery.data?.doctorName ?? '';
const hospitalName = doctorQuery.data?.hospitalName ?? '';
const specialization = doctorQuery.data?.specialization ?? '';

const doctorFeeQuery = useQuery({
queryKey: ['doctor-fee', doctorId],
enabled: !!doctorId,
queryFn: async () => {
const { data } = await api.get(`/doctors/${doctorId}/fee`);
return data.fee as number;
},
});

const myAppointments = useQuery<Appointment[]>({
queryKey: ['appointments'],
enabled: !!user,
queryFn: async () => {
const { data } = await api.get('/appointments');
return data;
},
});

const [guest, setGuest] = useState<GuestFormValue | null>(null);
const [error, setError] = useState<string | null>(null);
const [bookedAppointment, setBookedAppointment] = useState<Appointment | null>(null);

const isLoggedIn = !!user;
const canProceed = isLoggedIn || guest !== null;

const myCount = useMemo(() => {
return (
myAppointments.data?.filter(
(a) => a.status?.toUpperCase() !== 'CANCELLED'
).length ?? 0
);
}, [myAppointments.data]);

useEffect(() => {
setError(null);
setBookedAppointment(null);
}, [doctorId, date, time]);

const onBook = async () => {
setError(null);

 
if (!slotTime || !hospitalName) {
  setError('Invalid booking details.');
  return;
}

try {
  const userId = parseUserIdFromToken(authStorage.getToken());
  const hospitalId = normalizeHospitalId(hospitalName);

  const appointment = await createAppointment.mutateAsync({
    userId: isLoggedIn ? userId ?? undefined : undefined,
    doctorId,
    hospitalId,
    slotTime,
    doctorName,
    hospitalName,
    specialization,
    guest: isLoggedIn ? undefined : guest ?? undefined,
  });

  setBookedAppointment(appointment);
} catch (err) {
  if (axios.isAxiosError(err)) {
    const msg =
      err.response?.data?.message ||
      (typeof err.response?.data === 'string'
        ? err.response.data
        : null);

    if (msg) {
      setError(msg);
      return;
    }
  }

  setError('Booking failed.');
}
 

};

// guard
if (!doctorId || !date || !time) {
return ( <main className="p-6"> <Alert type="error">Missing booking details.</Alert> </main>
);
}

if (doctorQuery.isLoading) {
return <Alert type="info">Loading doctor details...</Alert>;
}

return ( <main className="mx-auto max-w-6xl p-6 space-y-6"> <PageHeader title="Place Appointment" subtitle="Confirm booking" />

 
  <Card title="Details">
    <div className="grid gap-2 text-sm">
      <p><b>Doctor:</b> {doctorName}</p>
      <p><b>Hospital:</b> {hospitalName}</p>
      <p><b>Specialization:</b> {specialization}</p>
      <p><b>Date:</b> {date}</p>
      <p><b>Time:</b> {time}</p>
      {isLoggedIn && <p><b>My Bookings:</b> {myCount}</p>}
    </div>
  </Card>

  {!isLoggedIn && !guest && (
    <Card title="Guest Details">
      <GuestForm onSubmit={setGuest} />
    </Card>
  )}

  {canProceed && doctorFeeQuery.data !== undefined && (
    <Card title="Consultation Fee">
      <p>Rs. {doctorFeeQuery.data}</p>
    </Card>
  )}

  {error && <Alert type="error">{error}</Alert>}

  {bookedAppointment && (
    <Alert type="success">
      Appointment booked. No: {bookedAppointment.appointmentNumber}
    </Alert>
  )}

  {canProceed && (
    <div className="flex justify-between">
      <div>
        {bookedAppointment ? (
          <Link href={`/payment?appointmentId=${bookedAppointment.id}`}>
            <Button variant="secondary">Pay Now</Button>
          </Link>
        ) : (
          <Button disabled>Pay Now</Button>
        )}
      </div>

      <Button
        onClick={onBook}
        disabled={createAppointment.isPending || !!bookedAppointment}
      >
        {createAppointment.isPending
          ? 'Booking...'
          : bookedAppointment
          ? 'Booked'
          : 'Book Appointment'}
      </Button>
    </div>
  )}
</main>
 

);
}

export default function Page() {
return (
<Suspense fallback={<div>Loading...</div>}> <PlaceAppointmentContent /> </Suspense>
);
}
