import Button from '../ui/Button';
import Card from '../ui/Card';
import { DoctorSearchResult } from '../../types/doctor';

type Props = {
  doctor: DoctorSearchResult;
  onBookNow: (slot: string) => void;
  hideHospitalFields?: boolean;
};

function formatToAmPm(slot: string) {
  if (!slot) return '-';

  const [hoursRaw, minutesRaw] = slot.split(':');
  const hours = Number(hoursRaw);
  const minutes = Number(minutesRaw ?? '0');

  if (Number.isNaN(hours) || Number.isNaN(minutes)) {
    return slot;
  }

  const period = hours >= 12 ? 'PM' : 'AM';
  const hour12 = hours % 12 === 0 ? 12 : hours % 12;
  const minuteText = String(minutes).padStart(2, '0');

  return `${hour12}:${minuteText} ${period}`;
}

export default function DoctorCard({ doctor, onBookNow, hideHospitalFields = false }: Props) {
  const selectedSlot = doctor.availableSlots[0] ?? '';

  return (
    <Card title={doctor.doctorName}>
      <div className="space-y-2 text-sm text-gray-700">
        <p>
          <span className="font-semibold">Doctor ID:</span> {doctor.doctorId}
        </p>
        {!hideHospitalFields ? (
          <p>
            <span className="font-semibold">Hospital:</span> {doctor.hospitalName}
          </p>
        ) : null}
        <p>
          <span className="font-semibold">Specialization:</span> {doctor.specialization}
        </p>
        <p>
          <span className="font-semibold">Date:</span> {doctor.date}
        </p>
        <p>
          <span className="font-semibold">Time:</span> {formatToAmPm(selectedSlot)}
        </p>
        <p>
          <span className="font-semibold">Total Fee:</span> Rs. {doctor.pricing.totalFee.toFixed(2)}
        </p>
        <p>
          <span className="font-semibold">Doctor Fee:</span> Rs.{' '}
          {doctor.pricing.doctorFee.toFixed(2)}
        </p>
        {!hideHospitalFields ? (
          <p>
            <span className="font-semibold">Hospital Fee:</span> Rs.{' '}
            {doctor.pricing.hospitalFee.toFixed(2)}
          </p>
        ) : null}
        <p>
          <span className="font-semibold">eChannelling Fee:</span> Rs.{' '}
          {doctor.pricing.eChannellingFee.toFixed(2)}
        </p>
      </div>

      <div className="mt-4">
        <Button type="button" onClick={() => onBookNow(selectedSlot)} disabled={!selectedSlot}>
          Book Now
        </Button>
      </div>
    </Card>
  );
}
