'use client';

import Link from 'next/link';
import { useQuery } from '@tanstack/react-query';
import Card from '../../components/ui/Card';
import PageHeader from '../../components/ui/PageHeader';
import Alert from '../../components/ui/Alert';
import api from '../../lib/api';

//types api layer
type ApprovedDoctorDto = {
  id: string;
  userId: string;
  fullName: string;
  specialization: string;
  hospital: string;
  isApproved: boolean;
};

// types ui model
type DoctorListItem = {
  id: string;
  name: string;
  specialization: string;
  hospital: string;
};

//mapper
function mapApprovedDoctor(dto: ApprovedDoctorDto): DoctorListItem {
  return {
    id: dto.id,
    name: dto.fullName,
    specialization: dto.specialization,
    hospital: dto.hospital,
  };
}

//utils
function getTodayLocalDateString() {
  const now = new Date();
  const year = now.getFullYear();
  const month = String(now.getMonth() + 1).padStart(2, '0');
  const day = String(now.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

//Component
export default function DoctorsPage() {
  const defaultDate = getTodayLocalDateString();

  const doctorsQuery = useQuery<DoctorListItem[]>({
    queryKey: ['approved-doctors'],
    queryFn: async () => {
      const { data } = await api.get<ApprovedDoctorDto[]>('/doctors/approved');
      return data.map(mapApprovedDoctor);
    },
  });

  return (
    <main className="mx-auto flex min-h-screen max-w-6xl flex-col gap-6 px-6 py-10">
      <PageHeader
        title="Find Doctors"
        subtitle="Browse approved doctors available in the system."
      />

      {doctorsQuery.isLoading && <Alert type="info">Loading doctors...</Alert>}

      {doctorsQuery.isError && (
        <Alert type="error">Unable to load doctors from the database.</Alert>
      )}

      {doctorsQuery.isSuccess && doctorsQuery.data.length === 0 && (
        <Alert type="info">No doctors found.</Alert>
      )}

      <Alert type="info">Select a doctor to view available dates and time slots.</Alert>

      <div className="grid gap-4 md:grid-cols-2">
        {(doctorsQuery.data ?? []).map((doctor) => (
          <Link
            key={doctor.id}
            href={`/doctors/results?doctorId=${doctor.id}&date=${defaultDate}`}
            className="block rounded-2xl transition hover:-translate-y-0.5"
          >
            <Card title={doctor.name}>
              <div className="text-sm text-gray-600">
                <p>Specialization: {doctor.specialization}</p>
                <p>Hospital: {doctor.hospital}</p>
                <p>Doctor ID: {doctor.id}</p>

                <p className="mt-3 font-medium text-blue-600">View Available Times</p>
              </div>
            </Card>
          </Link>
        ))}
      </div>
    </main>
  );
}
