import LoginForm from '../../components/forms/LoginForm';
import PageHeader from '../../components/ui/PageHeader';

export default function LoginPage() {
  return (
    <main className="mx-auto flex min-h-screen max-w-6xl flex-col gap-6 px-6 py-10">
      <PageHeader title="Login" subtitle="Access your appointments and upcoming consultations." />
      <div className="max-w-xl">
        <LoginForm />
      </div>
    </main>
  );
}
