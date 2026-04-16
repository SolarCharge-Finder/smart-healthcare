import Button from '../ui/Button';
import Card from '../ui/Card';
import { DoctorSearchResult } from '../../types/doctor';

type Props = {
  doctor: DoctorSearchResult;
  onViewAvailability: () => void;
  hideHospitalFields?: boolean;
};

export default function DoctorCard({
  doctor,
  onViewAvailability,
  hideHospitalFields = false,
}: Props) {
  return (
    <Card title={doctor.doctorName}>
      <div className="space-y-2 text-sm text-gray-700">
        <p>
          <span className="font-semibold">Doctor ID:</span> {doctor.doctorId}
        </p>

        {!hideHospitalFields && (
          <p>
            <span className="font-semibold">Hospital:</span> {doctor.hospitalName}
          </p>
        )}

        <p>
          <span className="font-semibold">Specialization:</span> {doctor.specialization}
        </p>
      </div>

      <div className="mt-4">
        <Button type="button" onClick={onViewAvailability}>
          View Availability
        </Button>
      </div>
    </Card>
  );
}
