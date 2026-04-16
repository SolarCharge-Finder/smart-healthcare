'use client';

import { useRouter } from 'next/navigation';
import SearchFilter from '../../components/search/SearchFilter';

export default function HomeSearchSection() {
  const router = useRouter();

  return (
    <section className="bg-blue-700 pb-8">
      <div className="container mx-auto px-6">
        <SearchFilter
          onSearch={(values) => {
            const params = new URLSearchParams();

            if (values.doctorName) {
              params.set('name', values.doctorName);
            }

            if (values.specialization) {
              params.set('specialization', values.specialization);
            }

            if (values.hospital) {
              params.set('hospital', values.hospital); // or hospital name depending on backend
            }

            if (values.date) {
              params.set('date', values.date);
            }

            router.push(`/doctors/results?${params.toString()}`);
          }}
        />
      </div>
    </section>
  );
}
