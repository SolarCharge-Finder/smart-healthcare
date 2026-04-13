"use client";

import { useRouter } from "next/navigation";
import SearchFilter from "../../components/search/SearchFilter";

export default function HomeSearchSection() {
  const router = useRouter();

  return (
    <section className="bg-blue-700 pb-8">
      <div className="container mx-auto px-6">
        <SearchFilter
          onSearch={(values) => {
            const params = new URLSearchParams();
            if (values.doctorName) params.set("doctorName", values.doctorName);
            if (!values.doctorName && values.specialization) {
              params.set("specialization", values.specialization);
            }
            if (values.hospitalId) params.set("hospitalId", values.hospitalId);
            params.set("date", values.date);

            router.push(`/doctors/results?${params.toString()}`);
          }}
        />
      </div>
    </section>
  );
}
