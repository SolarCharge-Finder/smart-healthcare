import PaymentForm from "../../components/forms/PaymentForm";
import PageHeader from "../../components/ui/PageHeader";

export default function PaymentPage() {
  return (
    <main className="mx-auto flex min-h-screen max-w-6xl flex-col gap-6 px-6 py-10">
      <PageHeader
        title="Secure Payment"
        subtitle="Complete your payment with trusted providers."
      />
      <div className="max-w-xl">
        <PaymentForm />
      </div>
    </main>
  );
}
