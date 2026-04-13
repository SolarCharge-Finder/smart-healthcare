"use client";

import { FormEvent, useMemo, useState } from "react";
import Alert from "../ui/Alert";
import Button from "../ui/Button";
import { useDoctorFilterOptions } from "../../hooks/useDoctorSearch";

type SearchValues = {
  doctorName: string;
  specialization: string;
  hospitalId: string;
  date: string;
};

type Props = {
  initialValues?: Partial<SearchValues>;
  onSearch: (values: SearchValues) => void;
};

function todayIso() {
  const now = new Date();
  const year = now.getFullYear();
  const month = String(now.getMonth() + 1).padStart(2, "0");
  const day = String(now.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}

export default function SearchFilter({ initialValues, onSearch }: Props) {
  const [values, setValues] = useState<SearchValues>({
    doctorName: initialValues?.doctorName ?? "",
    specialization: initialValues?.specialization ?? "",
    hospitalId: initialValues?.hospitalId ?? "",
    date: initialValues?.date ?? todayIso(),
  });

  const filters = useDoctorFilterOptions();

  const hospitalOptions = useMemo(
    () => filters.data?.hospitals ?? [],
    [filters.data]
  );

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault();
    onSearch(values);
  };

  return (
    <section className="rounded-2xl border border-blue-500/40 bg-blue-700 p-4 shadow-xl">
      <form onSubmit={handleSubmit} className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-5">
        <label className="flex flex-col gap-1">
          <span className="text-xs font-semibold text-blue-100">Doctor Name</span>
          <select
            className="w-full rounded-lg border border-blue-400/30 bg-blue-600 px-3 py-2.5 text-sm text-white outline-none focus:ring-2 focus:ring-white/40"
            value={values.doctorName}
            onChange={(e) => setValues((prev) => ({ ...prev, doctorName: e.target.value }))}
          >
            <option value="">All Doctors</option>
            {(filters.data?.doctorNames ?? []).map((name) => (
              <option key={name} value={name}>
                {name}
              </option>
            ))}
          </select>
        </label>

        <label className="flex flex-col gap-1">
          <span className="text-xs font-semibold text-blue-100">Specialization</span>
          <select
            className="w-full rounded-lg border border-blue-400/30 bg-blue-600 px-3 py-2.5 text-sm text-white outline-none focus:ring-2 focus:ring-white/40"
            value={values.specialization}
            onChange={(e) => setValues((prev) => ({ ...prev, specialization: e.target.value }))}
          >
            <option value="">All Specializations</option>
            {(filters.data?.specializations ?? []).map((spec) => (
              <option key={spec} value={spec}>
                {spec}
              </option>
            ))}
          </select>
        </label>

        <label className="flex flex-col gap-1">
          <span className="text-xs font-semibold text-blue-100">Hospital</span>
          <select
            className="w-full rounded-lg border border-blue-400/30 bg-blue-600 px-3 py-2.5 text-sm text-white outline-none focus:ring-2 focus:ring-white/40"
            value={values.hospitalId}
            onChange={(e) => setValues((prev) => ({ ...prev, hospitalId: e.target.value }))}
          >
            <option value="">All Hospitals</option>
            {hospitalOptions.map((hospital) => (
              <option key={hospital.id} value={hospital.id}>
                {hospital.name}
              </option>
            ))}
          </select>
        </label>

        <label className="flex flex-col gap-1">
          <span className="text-xs font-semibold text-blue-100">Date</span>
          <input
            type="date"
            min={todayIso()}
            className="w-full rounded-lg border border-blue-400/30 bg-blue-600 px-3 py-2.5 text-sm text-white outline-none focus:ring-2 focus:ring-white/40"
            value={values.date}
            onChange={(e) => setValues((prev) => ({ ...prev, date: e.target.value }))}
            required
          />
        </label>

        <div className="flex items-end">
          <Button className="w-full" type="submit" disabled={filters.isLoading}>
            Search
          </Button>
        </div>
      </form>

      {filters.isError ? (
        <div className="mt-3">
          <Alert type="error">Failed to load doctor filters.</Alert>
        </div>
      ) : null}
    </section>
  );
}
