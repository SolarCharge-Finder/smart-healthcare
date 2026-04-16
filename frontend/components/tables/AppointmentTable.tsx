'use client';

import { useState } from 'react';
import Link from 'next/link';
import { useQuery } from '@tanstack/react-query';
import { AxiosError } from 'axios';
import { useAppointments } from '../../hooks/useAppointments';
import { useCancelAppointment } from '../../hooks/useCancelAppointment';
import { getPaymentByAppointmentId } from '../../hooks/usePayment';
import { useAuthContext } from '../../modules/auth/AuthContext';
import { generateRecipePdf } from '../../lib/recipePdf';
import Card from '../ui/Card';
import Button from '../ui/Button';
import Alert from '../ui/Alert';
import Spinner from '../ui/Spinner';

function isVideoConsultationPayment(appointmentId: string, hospitalFee: number) {
  if (hospitalFee === 0) return true;

  if (typeof window === 'undefined') return false;

  try {
    const raw = window.localStorage.getItem('video-consultation-payments');
    if (!raw) return false;

    const parsed = JSON.parse(raw);
    return Boolean(parsed && typeof parsed === 'object' && parsed[appointmentId]);
  } catch {
    return false;
  }
}

export default function AppointmentTable() {
  const { user } = useAuthContext();
  const appointments = useAppointments();
  const cancelAppointment = useCancelAppointment();
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [selectedAppointmentId, setSelectedAppointmentId] = useState<string | null>(null);

  const visibleAppointments =
    appointments.data?.filter((appointment) => {
      const status = appointment.status.toUpperCase();
      return status !== 'CANCELLED';
    }) ?? [];

  const selectedAppointment = visibleAppointments.find(
    (appointment) => appointment.id === selectedAppointmentId,
  );

  const selectedIsVideoConsultation = selectedAppointment
    ? isVideoConsultationPayment(selectedAppointment.id, selectedAppointment.hospitalFee)
    : false;

  const selectedAppointmentNumber = selectedAppointment?.appointmentNumber ?? null;

  const paymentDetails = useQuery({
    queryKey: ['payment-by-appointment', selectedAppointmentId],
    enabled: Boolean(selectedAppointmentId),
    queryFn: async () => getPaymentByAppointmentId(selectedAppointmentId as string),
  });

  const handleCancel = async (id: string) => {
    setMessage(null);
    setError(null);

    try {
      await cancelAppointment.mutateAsync(id);
      setMessage('Appointment cancelled successfully.');
    } catch (err) {
      const axiosError = err as AxiosError;
      const responseMessage =
        typeof axiosError.response?.data === 'string' ? axiosError.response.data : null;

      if (responseMessage) {
        setError(responseMessage);
      } else if (axiosError.response?.status === 500) {
        setError('Server error. Please try again later.');
      } else {
        setError('Unable to cancel appointment.');
      }
    }
  };

  const getDisplayStatus = (status: string) => {
    const normalized = status.toUpperCase();

    if (normalized === 'PENDING_PAYMENT') {
      return 'Pending';
    }

    if (normalized === 'CONFIRMED' || normalized === 'PAID' || normalized === 'SUCCEEDED') {
      return 'Confirmed';
    }

    return status;
  };

  const isConfirmedStatus = (status: string) => {
    const normalized = status.toUpperCase();
    return normalized === 'CONFIRMED' || normalized === 'PAID' || normalized === 'SUCCEEDED';
  };

  const formatPaymentAmount = (amount: number, currency: string) => {
    const majorAmount = amount / 100;
    return `${majorAmount.toFixed(2)} ${currency.toUpperCase()}`;
  };

  const downloadRecipePdf = async () => {
    if (!selectedAppointment || !paymentDetails.data) return;
    const paidAt = new Date(paymentDetails.data.updatedAt).toLocaleString();

    const userRows: Array<[string, string]> = [
      ['User name', selectedAppointment.guestUser?.fullName ?? user?.name ?? '-'],
      ['Email', selectedAppointment.guestUser?.email ?? user?.email ?? '-'],
      ['Patient ID', selectedAppointment.patientId],
      ['User ID', selectedAppointment.userId ?? selectedAppointment.guestUserId ?? '-'],
    ];

    const doctorRows: Array<[string, string]> = [
      ['Doctor', selectedAppointment.doctorName],
      ['Specialization', selectedAppointment.specialization],
      ['Hospital', selectedAppointment.hospitalName],
      ['Date and time', new Date(selectedAppointment.slotTime).toLocaleString()],
      ['Booking Reference', selectedAppointment.bookingReferenceId],
    ];

    const paymentRows: Array<[string, string]> = [
      ['My Appointment Number', selectedAppointmentNumber ? `#${selectedAppointmentNumber}` : '-'],
      ['Payment ID', paymentDetails.data.id],
      ['Status', paymentDetails.data.status.toUpperCase()],
      ['Amount', formatPaymentAmount(paymentDetails.data.amount, paymentDetails.data.currency)],
      ['Paid at', paidAt],
    ];

    await generateRecipePdf({
      fileId: selectedAppointment.id,
      userDetails: userRows,
      doctorDetails: doctorRows,
      paymentSummary: paymentRows,
    });
  };

  return (
    <Card title="Appointment History">
      <div className="space-y-4">
        {appointments.isLoading ? (
          <div className="flex items-center gap-2 text-sm text-gray-600">
            <Spinner />
            Loading appointments...
          </div>
        ) : null}

        {appointments.isError ? <Alert type="error">Failed to load appointments.</Alert> : null}

        {message ? <Alert type="success">{message}</Alert> : null}
        {error ? <Alert type="error">{error}</Alert> : null}

        <div className="rounded-xl overflow-hidden border border-gray-200 shadow-sm">
          <div className="overflow-x-auto">
            <table className="w-full text-left text-sm">
              <thead className="bg-gray-100 font-semibold text-gray-700">
                <tr>
                  <th className="px-4 py-3">Appointment No</th>
                  <th className="px-4 py-3">Appointment ID</th>
                  <th className="px-4 py-3">Doctor ID</th>
                  <th className="px-4 py-3">Patient ID</th>
                  <th className="px-4 py-3">Slot Time</th>
                  <th className="px-4 py-3">Status</th>
                  <th className="px-4 py-3">Created At</th>
                  <th className="px-4 py-3">Actions</th>
                </tr>
              </thead>
              <tbody>
                {visibleAppointments.map((appointment) => (
                  <tr key={appointment.id} className="border-t border-gray-100 hover:bg-gray-50">
                    <td className="px-4 py-3 font-semibold text-blue-700">
                      #{appointment.appointmentNumber}
                    </td>
                    <td className="px-4 py-3 font-mono text-xs">{appointment.id}</td>
                    <td className="px-4 py-3 font-mono text-xs">{appointment.doctorId}</td>
                    <td className="px-4 py-3 font-mono text-xs">{appointment.patientId}</td>
                    <td className="px-4 py-3">{new Date(appointment.slotTime).toLocaleString()}</td>
                    <td className="px-4 py-3">
                      <span className="rounded-full bg-gray-100 px-2 py-1 text-xs text-gray-700">
                        {getDisplayStatus(appointment.status)}
                      </span>
                    </td>
                    <td className="px-4 py-3">
                      {new Date(appointment.createdAt).toLocaleString()}
                    </td>
                    <td className="px-4 py-3">
                      <div className="flex gap-2">
                        {appointment.status.toUpperCase() === 'PENDING_PAYMENT' ? (
                          <Link
                            href={`/payment?appointmentId=${appointment.id}`}
                            className="inline-block"
                          >
                            <Button variant="secondary">Pay Now</Button>
                          </Link>
                        ) : null}

                        {isConfirmedStatus(appointment.status) ? (
                          <Button
                            variant="secondary"
                            onClick={() => {
                              setSelectedAppointmentId(appointment.id);
                              setMessage(null);
                              setError(null);
                            }}
                          >
                            View
                          </Button>
                        ) : (
                          <Button
                            variant="danger"
                            onClick={() => handleCancel(appointment.id)}
                            disabled={cancelAppointment.isPending}
                          >
                            {cancelAppointment.isPending ? 'Cancelling...' : 'Cancel'}
                          </Button>
                        )}
                      </div>
                    </td>
                  </tr>
                ))}

                {appointments.isSuccess && visibleAppointments.length === 0 ? (
                  <tr className="border-t border-gray-100">
                    <td className="px-4 py-6 text-center text-sm text-gray-500" colSpan={8}>
                      No active appointments found.
                    </td>
                  </tr>
                ) : null}
              </tbody>
            </table>
          </div>
        </div>

        {selectedAppointment ? (
          <div
            className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 px-4"
            onClick={() => setSelectedAppointmentId(null)}
          >
            <div className="w-full max-w-3xl" onClick={(event) => event.stopPropagation()}>
              <Card title="Payment Summary">
                <div className="space-y-3 text-sm text-gray-700">
                  <div className="grid gap-2 md:grid-cols-2">
                    <p>
                      <span className="font-semibold">My Appointment Number:</span>{' '}
                      {selectedAppointmentNumber ? `#${selectedAppointmentNumber}` : '-'}
                    </p>
                    <p>
                      <span className="font-semibold">User name:</span>{' '}
                      {selectedAppointment.guestUser?.fullName ?? user?.name ?? '-'}
                    </p>
                    <p>
                      <span className="font-semibold">Email:</span>{' '}
                      {selectedAppointment.guestUser?.email ?? user?.email ?? '-'}
                    </p>
                    <p>
                      <span className="font-semibold">Doctor name:</span>{' '}
                      {selectedAppointment.doctorName}
                    </p>
                    <p>
                      <span className="font-semibold">Specialization:</span>{' '}
                      {selectedAppointment.specialization}
                    </p>
                    <p>
                      <span className="font-semibold">Hospital:</span>{' '}
                      {selectedAppointment.hospitalName}
                    </p>
                    <p>
                      <span className="font-semibold">Date and time:</span>{' '}
                      {new Date(selectedAppointment.slotTime).toLocaleString()}
                    </p>
                  </div>

                  <div className="rounded-lg border border-gray-200 bg-gray-50 p-3">
                    <p className="mb-2 text-sm font-semibold text-gray-800">Payment details</p>

                    {paymentDetails.isLoading ? (
                      <div className="flex items-center gap-2 text-sm text-gray-600">
                        <Spinner />
                        Loading payment details...
                      </div>
                    ) : null}

                    {paymentDetails.isError ? (
                      <Alert type="error">
                        Unable to load payment details for this appointment.
                      </Alert>
                    ) : null}

                    {paymentDetails.isSuccess ? (
                      <div className="grid gap-2 md:grid-cols-2">
                        <p>
                          <span className="font-semibold">Payment ID:</span>{' '}
                          {paymentDetails.data.id}
                        </p>
                        <p>
                          <span className="font-semibold">Status:</span>{' '}
                          {paymentDetails.data.status}
                        </p>
                        <p>
                          <span className="font-semibold">Amount:</span>{' '}
                          {formatPaymentAmount(
                            paymentDetails.data.amount,
                            paymentDetails.data.currency,
                          )}
                        </p>
                        <p>
                          <span className="font-semibold">Paid at:</span>{' '}
                          {new Date(paymentDetails.data.updatedAt).toLocaleString()}
                        </p>
                      </div>
                    ) : null}
                  </div>

                  <div className="flex justify-end gap-2">
                    {selectedIsVideoConsultation ? (
                      <Link
                        href={`/consultation/${selectedAppointment.id}`}
                        className="inline-block"
                      >
                        <Button disabled={!paymentDetails.data || paymentDetails.isLoading}>
                          Join Consultation
                        </Button>
                      </Link>
                    ) : (
                      <Button
                        onClick={downloadRecipePdf}
                        disabled={!paymentDetails.data || paymentDetails.isLoading}
                      >
                        Download recipe
                      </Button>
                    )}
                    <Button variant="secondary" onClick={() => setSelectedAppointmentId(null)}>
                      Close
                    </Button>
                  </div>
                </div>
              </Card>
            </div>
          </div>
        ) : null}
      </div>
    </Card>
  );
}
