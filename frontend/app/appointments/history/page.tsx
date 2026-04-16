import AppointmentTable from '../../../components/tables/AppointmentTable';
import PageHeader from '../../../components/ui/PageHeader';

export default function AppointmentHistoryPage() {
  return (
    <main className="mx-auto flex min-h-screen max-w-6xl flex-col gap-6 px-6 py-10">
      <PageHeader
        title="My Appointments"
        subtitle="Track upcoming visits and manage cancellations."
      />
      <AppointmentTable />
    </main>
  );
}
