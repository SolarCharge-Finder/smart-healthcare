"use client";

import { useMemo, useState } from "react";
import Card from "../../components/ui/Card";
import PageHeader from "../../components/ui/PageHeader";

const doctors = [
  {
    id: "D-1001",
    name: "Dr. Ayesha Perera",
    specialization: "Cardiology",
    location: "Colombo"
  },
  {
    id: "D-1002",
    name: "Dr. Nimal Silva",
    specialization: "Dermatology",
    location: "Kandy"
  },
  {
    id: "D-1003",
    name: "Dr. Shanika Fernando",
    specialization: "Pediatrics",
    location: "Galle"
  }
];

export default function DoctorsPage() {
  const [specialization, setSpecialization] = useState("");
  const [location, setLocation] = useState("");

  const filtered = useMemo(() => {
    return doctors.filter((doctor) => {
      const specOk = specialization
        ? doctor.specialization === specialization
        : true;
      const locOk = location ? doctor.location === location : true;
      return specOk && locOk;
    });
  }, [specialization, location]);

  return (
    <main className="mx-auto flex min-h-screen max-w-6xl flex-col gap-6 px-6 py-10">
      <PageHeader
        title="Find Doctors"
        subtitle="Browse by specialization or location."
      />

      <Card title="Search">
        <div className="grid gap-4 md:grid-cols-2">
          <label className="block space-y-1">
            <span className="text-sm font-medium text-gray-700">
              Specialization
            </span>
            <select
              className="w-full rounded-lg border border-gray-300 bg-white p-3 text-sm outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-500"
              value={specialization}
              onChange={(e) => setSpecialization(e.target.value)}
            >
              <option value="">All</option>
              <option value="Cardiology">Cardiology</option>
              <option value="Dermatology">Dermatology</option>
              <option value="Pediatrics">Pediatrics</option>
            </select>
          </label>
          <label className="block space-y-1">
            <span className="text-sm font-medium text-gray-700">
              Location
            </span>
            <select
              className="w-full rounded-lg border border-gray-300 bg-white p-3 text-sm outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-500"
              value={location}
              onChange={(e) => setLocation(e.target.value)}
            >
              <option value="">All</option>
              <option value="Colombo">Colombo</option>
              <option value="Kandy">Kandy</option>
              <option value="Galle">Galle</option>
            </select>
          </label>
        </div>
      </Card>

      <div className="grid gap-4 md:grid-cols-2">
        {filtered.map((doctor) => (
          <Card key={doctor.id} title={doctor.name}>
            <div className="text-sm text-gray-600">
              <p>Specialization: {doctor.specialization}</p>
              <p>Location: {doctor.location}</p>
              <p>Doctor ID: {doctor.id}</p>
            </div>
          </Card>
        ))}
      </div>
    </main>
  );
}
