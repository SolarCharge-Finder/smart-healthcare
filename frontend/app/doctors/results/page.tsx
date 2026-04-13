"use client";

import { Suspense } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import Alert from "../../../components/ui/Alert";
import PageHeader from "../../../components/ui/PageHeader";
import SearchFilter from "../../../components/search/SearchFilter";
import DoctorCard from "../../../components/cards/DoctorCard";
import { useDoctorSearch } from "../../../hooks/useDoctorSearch";

function DoctorSearchResultsContent() {
  const router = useRouter();
  const params = useSearchParams();

  const doctorName = params.get("doctorName") ?? "";
  const specializationParam = params.get("specialization") ?? "";
  const specialization = doctorName ? "" : specializationParam;
  const hospitalId = params.get("hospitalId") ?? "";
  const date = params.get("date") ?? "";
  const isTelemedicineView = params.get("telemedicine") === "1";

  const search = useDoctorSearch({
    doctorName,
    specialization,
    hospitalId,
    date,
    lookAheadDays: 7,
  });

  const selectedDateResults = (search.data ?? []).filter((doctor) => doctor.date === date);
  const nextDateResults = (search.data ?? []).filter((doctor) => doctor.date !== date);

  return (
    <main className="mx-auto flex min-h-screen max-w-6xl flex-col gap-6 px-6 py-10">
      <PageHeader
        title="Doctor Search Results"
        subtitle="Choose an available slot and continue to booking."
      />

      <SearchFilter
        initialValues={{ doctorName, specialization, hospitalId, date }}
        onSearch={(values) => {
          const next = new URLSearchParams();
          if (isTelemedicineView) next.set("telemedicine", "1");
          if (values.doctorName) next.set("doctorName", values.doctorName);
          if (!values.doctorName && values.specialization) {
            next.set("specialization", values.specialization);
          }
          if (values.hospitalId) next.set("hospitalId", values.hospitalId);
          next.set("date", values.date);
          router.push(`/doctors/results?${next.toString()}`);
        }}
      />

      {search.isError ? (
        <Alert type="error">Unable to load search results.</Alert>
      ) : null}

      {search.isSuccess && search.data.length === 0 ? (
        <Alert type="info">
          No doctors are available for the selected filters from {date} to the next 7 days.
        </Alert>
      ) : null}

      {search.isSuccess && date && selectedDateResults.length === 0 && nextDateResults.length > 0 ? (
        <Alert type="info">
          No doctors are available on {date}. Showing the next available dates.
        </Alert>
      ) : null}

      {search.isSuccess && date && selectedDateResults.length > 0 && nextDateResults.length > 0 ? (
        <Alert type="info">
          Showing available doctors for {date} and upcoming available dates.
        </Alert>
      ) : null}

      <div className="grid gap-4 md:grid-cols-2">
        {(search.data ?? []).map((doctor) => (
          <DoctorCard
            key={`${doctor.doctorId}-${doctor.hospitalId}-${doctor.date}`}
            doctor={doctor}
            hideHospitalFields={isTelemedicineView}
            onBookNow={(slot) => {
              const targetSlot = slot || doctor.availableSlots[0];
              if (!targetSlot) return;

              const next = new URLSearchParams({
                doctorId: doctor.doctorId,
                doctorName: doctor.doctorName,
                hospitalId: doctor.hospitalId,
                hospitalName: doctor.hospitalName,
                specialization: doctor.specialization,
                selectedDate: doctor.date,
                selectedTimeSlot: targetSlot,
              });

              if (isTelemedicineView) {
                next.set("telemedicine", "1");
              }

              router.push(`/appointments/place?${next.toString()}`);
            }}
          />
        ))}
      </div>
    </main>
  );
}

export default function DoctorSearchResultsPage() {
  return (
    <Suspense fallback={<main className="mx-auto min-h-screen max-w-6xl px-6 py-10">Loading...</main>}>
      <DoctorSearchResultsContent />
    </Suspense>
  );
}
