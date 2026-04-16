import Link from 'next/link';
import PageHeader from '../../../components/ui/PageHeader';

export default function BookAppointmentPage() {
  return (
    <main className="mx-auto flex min-h-screen max-w-6xl flex-col gap-6 px-6 py-10">
      <PageHeader
        title="Choose Your Service"
        subtitle="Select how you want to continue your care journey."
      />

      <div className="flex flex-1 items-center justify-center">
        <div className="grid w-full max-w-3xl gap-5 md:grid-cols-2">
          <Link
            href="/doctors"
            className="group rounded-2xl bg-blue-700 p-8 shadow-lg transition duration-200 hover:-translate-y-1 hover:bg-blue-800"
          >
            <div className="mb-4 inline-flex h-12 w-12 items-center justify-center rounded-full border border-white/40 bg-white/10">
              <svg
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                strokeWidth="2"
                className="h-6 w-6 text-white"
                aria-hidden="true"
              >
                <path d="M19 21H5a2 2 0 0 1-2-2v-6a4 4 0 0 1 4-4h10a4 4 0 0 1 4 4v6a2 2 0 0 1-2 2Z" />
                <circle cx="12" cy="5" r="3" />
              </svg>
            </div>
            <h2 className="text-2xl font-semibold text-white">Book Appointment</h2>
          </Link>

          <Link
            href="/consultation"
            className="group rounded-2xl bg-blue-700 p-8 shadow-lg transition duration-200 hover:-translate-y-1 hover:bg-blue-800"
          >
            <div className="mb-4 inline-flex h-12 w-12 items-center justify-center rounded-full border border-white/40 bg-white/10">
              <svg
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                strokeWidth="2"
                className="h-6 w-6 text-white"
                aria-hidden="true"
              >
                <rect x="3" y="6" width="14" height="12" rx="2" />
                <path d="M17 10l4-2v8l-4-2z" />
              </svg>
            </div>
            <h2 className="text-2xl font-semibold text-white">Video Consultation</h2>
          </Link>
        </div>
      </div>
    </main>
  );
}
