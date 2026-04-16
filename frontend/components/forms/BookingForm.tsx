'use client';

import { FormEvent, useMemo, useState } from 'react';
import { useRouter } from 'next/navigation';
import { AxiosError } from 'axios';
import { useCreateAppointment } from '../../hooks/useCreateAppointment';
import Card from '../ui/Card';
import Input from '../ui/Input';
import Button from '../ui/Button';
import Alert from '../ui/Alert';
import Spinner from '../ui/Spinner';

const buildSlots = () => {
  const slots: string[] = [];
  for (let hour = 0; hour < 24; hour += 1) {
    for (let min = 0; min < 60; min += 30) {
      const h = hour.toString().padStart(2, '0');
      const m = min.toString().padStart(2, '0');
      slots.push(`${h}:${m}`);
    }
  }
  return slots;
};

export default function BookingForm() {
  const slots = useMemo(buildSlots, []);
  const createAppointment = useCreateAppointment();
  const router = useRouter();

  const [patientId, setPatientId] = useState('');
  const [doctorId, setDoctorId] = useState('');
  const [date, setDate] = useState('');
  const [time, setTime] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);

  const onSubmit = async (event: FormEvent) => {
    event.preventDefault();
    setError(null);
    setSuccess(false);

    if (!date || !time) {
      setError('Please select a date and time slot.');
      return;
    }

    const slotTime = new Date(`${date}T${time}:00`).toISOString();

    try {
      const appointment = await createAppointment.mutateAsync({
        patientId,
        doctorId,
        slotTime,
      });
      setSuccess(true);
      setTimeout(() => router.push(`/payment?appointmentId=${appointment.id}`), 800);
    } catch (err) {
      const axiosError = err as AxiosError;
      const responseMessage =
        typeof axiosError.response?.data === 'string' ? axiosError.response.data : null;

      if (responseMessage) {
        setError(responseMessage);
      } else if (axiosError.response?.status === 409) {
        setError('Slot already booked. Please choose another time.');
      } else if (axiosError.response?.status === 500) {
        setError('Server error. Please try again later.');
      } else {
        setError('Network error. Please check your connection.');
      }
    }
  };

  return (
    <Card title="Book Appointment">
      <form className="space-y-4" onSubmit={onSubmit}>
        <Input
          label="Patient ID"
          placeholder="UUID"
          value={patientId}
          onChange={(e) => setPatientId(e.target.value)}
          required
        />
        <Input
          label="Doctor ID"
          placeholder="UUID"
          value={doctorId}
          onChange={(e) => setDoctorId(e.target.value)}
          required
        />
        <Input
          label="Date"
          type="date"
          value={date}
          onChange={(e) => setDate(e.target.value)}
          required
        />
        <label className="block space-y-1">
          <span className="text-sm font-medium text-gray-700">Time Slot</span>
          <select
            className="w-full rounded-lg border border-gray-300 bg-white p-3 text-sm outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-500"
            value={time}
            onChange={(e) => setTime(e.target.value)}
            required
          >
            <option value="">Select a time</option>
            {slots.map((slot) => (
              <option key={slot} value={slot}>
                {slot}
              </option>
            ))}
          </select>
        </label>

        {error ? <Alert type="error">{error}</Alert> : null}
        {success ? <Alert type="success">Appointment booked successfully.</Alert> : null}

        <Button type="submit" disabled={createAppointment.isPending}>
          {createAppointment.isPending ? (
            <span className="flex items-center gap-2">
              <Spinner />
              Booking...
            </span>
          ) : (
            'Book Appointment'
          )}
        </Button>
      </form>
    </Card>
  );
}
