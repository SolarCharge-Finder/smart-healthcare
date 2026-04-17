'use client';

import { useDoctorProfile } from './hooks/useDoctorProfile';
import { useDoctorAvailability } from './hooks/useDoctorAvailability';
import { useUpdateDoctorFee } from './hooks/useUpdateDoctorFee';
import Card from '@/components/ui/Card';
import Alert from '@/components/ui/Alert';
import Button from '@/components/ui/Button';
import { useState, useEffect } from 'react';

export default function DoctorDashboard() {
  // hook init
  const doctorProfile = useDoctorProfile();
  const updateFee = useUpdateDoctorFee();

  // safe fallback until data loads
  const doctorId = doctorProfile.data?.id ?? '';

  const { availabilityQuery, createAvailability, deactivateAvailability } =
    useDoctorAvailability(doctorId);

  // form state
  const [form, setForm] = useState({
    hospital: '',
    startTime: '',
    endTime: '',
    isRecurring: false,
    dayOfWeek: 0,
  });

  const [fee, setFee] = useState<number>(0);

  useEffect(() => {
    if (doctorProfile.data) {
      setForm((prev) => ({
        ...prev,
        hospital: doctorProfile.data.hospital,
      }));
    }
  }, [doctorProfile.data]);

  // handle loading
  if (doctorProfile.isLoading) {
    return <Alert type="info">Loading doctor profile...</Alert>;
  }

  if (!doctorProfile.data) {
    return <Alert type="error">Doctor profile not found.</Alert>;
  }

  const handleSubmit = async () => {
    await createAvailability.mutateAsync({
      hospital: form.hospital,
      startTime: new Date(form.startTime).toISOString(),
      endTime: new Date(form.endTime).toISOString(),
      isRecurring: form.isRecurring,
      dayOfWeek: form.isRecurring ? form.dayOfWeek : null,
    });
  };

  const handleFeeUpdate = async () => {
    if (!doctorId) return;

    await updateFee.mutateAsync({
      doctorId,
      fee,
    });
  };

  return (
    <Card title="Doctor Dashboard">
      {/* add availability */}
      <div className="mb-3">
        <h3 className="font-semibold mb-2">Add Availability</h3>

        {/* <input
          className="w-full border p-2 mb-2"
          placeholder="Hospital"
          value={form.hospital}
          onChange={(e) => setForm({ ...form, hospital: e.target.value })}
        /> */}

        <input
          type="datetime-local"
          className="w-full border p-2 mb-2"
          onChange={(e) => setForm({ ...form, startTime: e.target.value })}
        />

        <input
          type="datetime-local"
          className="w-full border p-2 mb-2"
          onChange={(e) => setForm({ ...form, endTime: e.target.value })}
        />

        <label className="flex items-center gap-2 mb-2">
          <input
            type="checkbox"
            onChange={(e) => setForm({ ...form, isRecurring: e.target.checked })}
          />
          Recurring
        </label>

        {form.isRecurring && (
          <input
            type="number"
            min={0}
            max={6}
            className="w-full border p-2 mb-2"
            placeholder="Day of week (0-6)"
            onChange={(e) => setForm({ ...form, dayOfWeek: Number(e.target.value) })}
          />
        )}

        <Button onClick={handleSubmit} disabled={createAvailability.isPending}>
          {createAvailability.isPending ? 'Saving...' : 'Add Availability'}
        </Button>
      </div>

      {/* availability list */}
      <div className="mb-3">
        <h3 className="font-semibold mb-2">Your Availability</h3>

        {availabilityQuery.isLoading && <Alert type="info">Loading availability...</Alert>}

        {availabilityQuery.data?.map((slot) => (
          <div key={slot.id} className="border p-3 rounded mb-2 flex justify-between items-center">
            <div>
              <p>{slot.hospital}</p>
              <p className="text-sm text-gray-500">
                {new Date(slot.startTime).toLocaleString()} -{' '}
                {new Date(slot.endTime).toLocaleString()}
              </p>
            </div>

            <Button
              variant="danger"
              disabled={!slot.isActive || deactivateAvailability.isPending}
              className={!slot.isActive ? 'opacity-50 cursor-not-allowed' : ''}
              onClick={() =>
                deactivateAvailability.mutate({
                  availabilityId: slot.id,
                })
              }
            >
              {slot.isActive ? 'Deactivate' : 'Inactive'}
            </Button>
          </div>
        ))}
      </div>

      {/* add /edit consultation fee*/}
      <div className="mb-3">
        <h3 className="font-semibold mb-2">Consultation Fee</h3>

        <input
          type="number"
          className="w-full border p-2 mb-2"
          placeholder="Enter fee"
          value={fee}
          min={0}
          onChange={(e) => setFee(Number(e.target.value))}
        />

        <Button onClick={handleFeeUpdate} disabled={updateFee?.isPending}>
          {updateFee?.isPending ? 'Updating...' : 'Update Fee'}
        </Button>
      </div>
    </Card>
  );
}
