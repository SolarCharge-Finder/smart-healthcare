'use client';

import { Suspense } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import Alert from '../../../components/ui/Alert';
import PageHeader from '../../../components/ui/PageHeader';
import Card from '../../../components/ui/Card';
import Button from '../../../components/ui/Button';
import SearchFilter from '../../../components/search/SearchFilter';
import { useDoctorSearch } from '../../../hooks/useDoctorSearch';

function ResultsContent() {
  const router = useRouter();
  const params = useSearchParams();

  // extract params
  const doctorName = params.get('name') ?? '';
  const specialization = params.get('specialization') ?? '';
  const hospital = params.get('hospital') ?? '';
  const date = params.get('date') ?? '';

  const search = useDoctorSearch({
    doctorName,
    specialization,
    hospital,
    date,
    lookAheadDays: 7,
  });

  return (
    <main className="mx-auto flex min-h-screen max-w-6xl flex-col gap-6 px-6 py-10">
      <PageHeader
        title="Doctor Search Results"
        subtitle="Select a doctor to view available time slots."
      />

      {/* search filter */}
      <SearchFilter
        initialValues={{ doctorName, specialization, hospital, date }}
        onSearch={(values) => {
          const next = new URLSearchParams();

          if (values.doctorName) next.set('name', values.doctorName);
          if (values.specialization) next.set('specialization', values.specialization);
          if (values.hospital) next.set('hospital', values.hospital);

          next.set('date', values.date);

          router.push(`/doctors/results?${next.toString()}`);
        }}
      />

      {/* states */}
      {search.isLoading && <Alert type="info">Searching doctors...</Alert>}
      {search.isError && <Alert type="error">Failed to load search results.</Alert>}
      {search.isSuccess && search.data.length === 0 && (
        <Alert type="info">No doctors found for selected filters.</Alert>
      )}

      {/* results */}
      <div className="grid gap-4 md:grid-cols-2">
        {(search.data ?? []).map((doctor) => (
          <Card key={doctor.doctorId} title={doctor.doctorName}>
            <div className="text-sm text-gray-600">
              <p>Specialization: {doctor.specialization}</p>
              <p>Hospital: {doctor.hospitalName}</p>

              <div className="mt-3">
                <Button
                  onClick={() => {
                    const next = new URLSearchParams({
                      date, // preserve selected date
                    });
                    router.push(`/doctors/${doctor.doctorId}?${next.toString()}`);
                  }}
                >
                  View Availability
                </Button>
              </div>
            </div>
          </Card>
        ))}
      </div>
    </main>
  );
}

export default function DoctorResultsPage() {
  return (
    <Suspense fallback={<div>Loading...</div>}>
      <ResultsContent />
    </Suspense>
  );
}
