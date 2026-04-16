'use client';

import { useMemo, useState } from 'react';
import { AxiosError } from 'axios';
import { useAvailability } from '../../hooks/useAvailability';
import Card from '../ui/Card';
import Input from '../ui/Input';
import Button from '../ui/Button';
import Alert from '../ui/Alert';
import Spinner from '../ui/Spinner';

export default function AvailabilityForm() {
  const [doctorId, setDoctorId] = useState('');
  const [fromDate, setFromDate] = useState('');
  const [toDate, setToDate] = useState('');
  const availability = useAvailability(doctorId, fromDate, toDate);
  const rangeInvalid = Boolean(fromDate && toDate) && new Date(fromDate) > new Date(toDate);
  const errorMessage = (() => {
    const err = availability.error as AxiosError | null | undefined;
    const data = err?.response?.data;
    return typeof data === 'string' ? data : null;
  })();

  const slots = useMemo(() => {
    if (!availability.data) return [];
    return availability.data.map((slot) => new Date(slot).toLocaleString());
  }, [availability.data]);

  return (
    <Card title="Check Availability">
      <div className="space-y-4">
        <Input
          label="Doctor ID"
          placeholder="UUID"
          value={doctorId}
          onChange={(e) => setDoctorId(e.target.value)}
        />
        <div className="grid gap-4 md:grid-cols-2">
          <Input
            label="From"
            type="date"
            value={fromDate}
            onChange={(e) => setFromDate(e.target.value)}
          />
          <Input
            label="To"
            type="date"
            value={toDate}
            onChange={(e) => setToDate(e.target.value)}
          />
        </div>

        <Button
          type="button"
          variant="secondary"
          onClick={() => availability.refetch()}
          disabled={!doctorId || !fromDate || !toDate || rangeInvalid || availability.isFetching}
        >
          {availability.isFetching ? (
            <span className="flex items-center gap-2">
              <Spinner />
              Loading...
            </span>
          ) : (
            'Check Slots'
          )}
        </Button>

        {availability.isError ? (
          <Alert type="error">
            {errorMessage ?? 'Unable to fetch availability. Please try again.'}
          </Alert>
        ) : null}

        {rangeInvalid ? <Alert type="error">End date must be after start date.</Alert> : null}

        {availability.isSuccess && slots.length === 0 ? (
          <Alert type="info">No available slots in this range.</Alert>
        ) : null}

        {slots.length > 0 ? (
          <div className="flex flex-wrap gap-2">
            {slots.map((slot) => (
              <button
                key={slot}
                type="button"
                className="rounded-md border border-gray-200 bg-white px-3 py-2 text-sm text-gray-700 transition hover:bg-blue-50"
              >
                {slot}
              </button>
            ))}
          </div>
        ) : null}
      </div>
    </Card>
  );
}
