"use client";

import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import Card from "../../components/ui/Card";
import PageHeader from "../../components/ui/PageHeader";
import Alert from "../../components/ui/Alert";
import api from "../../lib/api";

type DoctorListItem = {
  doctorId: string;
  doctorName: string;
  specialization: string;
  hospitalId: string;
  hospitalName: string;
  nextAvailableSlot: string;
};

function getTodayLocalDateString() {
  const now = new Date();
  const year = now.getFullYear();
  const month = String(now.getMonth() + 1).padStart(2, "0");
  const day = String(now.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}

export default function DoctorsPage() {
  const defaultDate = getTodayLocalDateString();

  const doctors = useQuery<DoctorListItem[]>({
    queryKey: ["doctor-list"],
    queryFn: async () => {
      const { data } = await api.get<DoctorListItem[]>("/doctors");
      return data;
    },
  });

  return (
    <main className="mx-auto flex min-h-screen max-w-6xl flex-col gap-6 px-6 py-10">
      <PageHeader
        title="Find Doctors"
        subtitle="All doctors from the database."
      />

      {doctors.isLoading ? <Alert type="info">Loading doctors...</Alert> : null}

      {doctors.isError ? (
        <Alert type="error">Unable to load doctors from database.</Alert>
      ) : null}

      {doctors.isSuccess && doctors.data.length === 0 ? (
        <Alert type="info">No doctors found in database.</Alert>
      ) : null}

      <Alert type="info">
        Click a doctor card to view available hospitals, dates, and time slots.
      </Alert>

      <div className="grid gap-4 md:grid-cols-2">
        {(doctors.data ?? []).map((doctor) => (
          <Link
            key={`${doctor.doctorId}-${doctor.hospitalId}`}
            href={`/doctors/results?doctorName=${encodeURIComponent(doctor.doctorName)}&date=${defaultDate}`}
            className="block rounded-2xl transition hover:-translate-y-0.5"
          >
            <Card title={doctor.doctorName}>
              <div className="text-sm text-gray-600">
                <p>Specialization: {doctor.specialization}</p>
                <p>Hospital: {doctor.hospitalName}</p>
                <p>Doctor ID: {doctor.doctorId}</p>
                <p>
                  Next Slot: {doctor.nextAvailableSlot
                    ? new Date(doctor.nextAvailableSlot).toLocaleString()
                    : "-"}
                </p>
                <p className="mt-3 font-medium text-blue-600">View Available Times</p>
              </div>
            </Card>
          </Link>
        ))}
      </div>
    </main>
  );
}
