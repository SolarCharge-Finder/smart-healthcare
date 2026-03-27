import Link from "next/link";
import Card from "../../components/ui/Card";
import Button from "../../components/ui/Button";
import PageHeader from "../../components/ui/PageHeader";

const items = [
  {
    title: "Book Appointment",
    description: "Reserve a slot with your preferred doctor.",
    href: "/appointments/book",
    cta: "Book Now"
  },
  {
    title: "Check Availability",
    description: "Find available time slots by date range.",
    href: "/appointments/availability",
    cta: "Check Slots"
  },
  {
    title: "Appointment History",
    description: "Review and cancel upcoming appointments.",
    href: "/appointments/history",
    cta: "View History"
  }
];

export default function AppointmentsLandingPage() {
  return (
    <main className="mx-auto flex min-h-screen max-w-6xl flex-col gap-6 px-6 py-10">
      <PageHeader
        title="Appointments"
        subtitle="Manage bookings, check availability, and review history."
      />

      <div className="grid gap-4 md:grid-cols-3">
        {items.map((item) => (
          <Card key={item.title} title={item.title}>
            <p className="text-sm text-gray-600">{item.description}</p>
            <Link href={item.href} className="mt-4 inline-block">
              <Button variant="secondary">{item.cta}</Button>
            </Link>
          </Card>
        ))}
      </div>
    </main>
  );
}
