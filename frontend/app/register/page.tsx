import RegisterForm from "../../components/forms/RegisterForm";
import PageHeader from "../../components/ui/PageHeader";

export default function RegisterPage() {
  return (
    <main className="mx-auto flex min-h-screen max-w-6xl flex-col gap-6 px-6 py-10">
      <PageHeader
        title="Create Account"
        subtitle="Set up your SmartHealth profile to manage bookings."
      />
      <div className="max-w-xl">
        <RegisterForm />
      </div>
    </main>
  );
}
