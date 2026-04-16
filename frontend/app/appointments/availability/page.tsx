import AvailabilityForm from '../../../components/forms/AvailabilityForm';
import PageHeader from '../../../components/ui/PageHeader';

export default function AvailabilityPage() {
  return (
    <main className="mx-auto flex min-h-screen max-w-6xl flex-col gap-6 px-6 py-10">
      <PageHeader title="Check Availability" subtitle="View available time slots before booking." />
      <div className="max-w-3xl">
        <AvailabilityForm />
      </div>
    </main>
  );
}
