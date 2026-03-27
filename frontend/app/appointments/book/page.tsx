import BookingForm from "../../../components/forms/BookingForm";
import PageHeader from "../../../components/ui/PageHeader";

export default function BookAppointmentPage() {
  return (
    <main className="mx-auto flex min-h-screen max-w-6xl flex-col gap-6 px-6 py-10">
      <PageHeader
        title="Book Appointment"
        subtitle="Schedule your consultation securely."
      />
      <div className="max-w-3xl">
        <BookingForm />
      </div>
    </main>
  );
}
