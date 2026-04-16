'use client';

import { Suspense } from 'react';
import { useParams, useRouter, useSearchParams } from 'next/navigation';
import { useDoctorAvailability } from '../../../hooks/useDoctorAvailability';
import Alert from '../../../components/ui/Alert';
import PageHeader from '../../../components/ui/PageHeader';
import Card from '../../../components/ui/Card';
import Button from '../../../components/ui/Button';

// availability type (adjust if needed)
type Availability = {
  id: string;
  doctorId: string;
  hospital: string;
  startTime: string;
  endTime: string;
  isActive: boolean;
  isRecurring: boolean;
  dayOfWeek: number | null;
};

// group slots by date
function groupByDate(slots: Availability[]) {
  const map: Record<string, Availability[]> = {};

  slots.forEach((slot) => {
    const date = new Date(slot.startTime).toISOString().split('T')[0];

    if (!map[date]) map[date] = [];
    map[date].push(slot);
  });

  return map;
}

// format time nicely
function formatTime(dateString: string) {
  return new Date(dateString).toLocaleTimeString([], {
    hour: '2-digit',
    minute: '2-digit',
  });
}

function DoctorAvailabilityContent() {
  const router = useRouter();
  const params = useParams();
  const searchParams = useSearchParams();

  const doctorId = params.doctorId as string;
  const selectedDate = searchParams.get('date');

  const { availabilityQuery } = useDoctorAvailability(doctorId);

  if (!doctorId) {
    return <Alert type="error">Invalid doctor.</Alert>;
  }

  if (availabilityQuery.isLoading) {
    return <Alert type="info">Loading availability...</Alert>;
  }

  if (availabilityQuery.isError) {
    return <Alert type="error">Failed to load availability.</Alert>;
  }

  const allSlots = availabilityQuery.data ?? [];

  // only active slots
  const activeSlots = allSlots.filter((s: Availability) => s.isActive);

  if (activeSlots.length === 0) {
    return <Alert type="info">No available slots.</Alert>;
  }

  const grouped = groupByDate(activeSlots);
  const dates = Object.keys(grouped).sort();

  return (
    <main className="mx-auto flex min-h-screen max-w-6xl flex-col gap-6 px-6 py-10">
      <PageHeader title="Available Time Slots" subtitle="Select a time to continue booking." />

      {/* selected date first */}
      {selectedDate && grouped[selectedDate] && (
        <Card title={`Selected Date: ${selectedDate}`}>
          <div className="flex flex-wrap gap-2">
            {grouped[selectedDate].map((slot) => (
              <Button
                key={slot.id}
                onClick={() => {
                  const next = new URLSearchParams({
                    doctorId: slot.doctorId,
                    availabilityId: slot.id,
                    date: selectedDate,
                    time: slot.startTime,
                  });

                  router.push(`/appointments/place?${next.toString()}`);
                }}
              >
                {formatTime(slot.startTime)}
              </Button>
            ))}
          </div>
        </Card>
      )}

      {/* other dates */}
      <div className="grid gap-4 md:grid-cols-2">
        {dates
          .filter((d) => d !== selectedDate)
          .map((date) => (
            <Card key={date} title={date}>
              <div className="flex flex-wrap gap-2">
                {grouped[date].map((slot) => (
                  <Button
                    key={slot.id}
                    variant="secondary"
                    onClick={() => {
                      const next = new URLSearchParams({
                        doctorId: slot.doctorId,
                        availabilityId: slot.id,
                        date,
                        time: slot.startTime,
                      });

                      router.push(`/appointments/place?${next.toString()}`);
                    }}
                  >
                    {formatTime(slot.startTime)}
                  </Button>
                ))}
              </div>
            </Card>
          ))}
      </div>
    </main>
  );
}

export default function DoctorAvailabilityPage() {
  return (
    <Suspense fallback={<div>Loading...</div>}>
      <DoctorAvailabilityContent />
    </Suspense>
  );
}
