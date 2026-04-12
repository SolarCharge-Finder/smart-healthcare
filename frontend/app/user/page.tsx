"use client";

import { useRouter } from "next/navigation";
import Link from "next/link";
import Button from "../../components/ui/Button";
import Card from "../../components/ui/Card";
import PageHeader from "../../components/ui/PageHeader";
import { useAuthContext } from "../../modules/auth/AuthContext";

export default function UserPage() {
  const router = useRouter();
  const { user, logout } = useAuthContext();

  const onLogout = () => {
    logout();
    router.push("/auth");
  };

  return (
    <main className="mx-auto flex min-h-screen max-w-4xl flex-col gap-6 px-6 py-10">
      <PageHeader
        title="User Profile"
        subtitle="View your account details and manage your session."
      />

      {!user ? (
        <Card title="Not Logged In">
          <p className="text-sm text-gray-600">
            You are not logged in. Please sign in to view your profile.
          </p>
          <div className="mt-4">
            <Link href="/auth" className="inline-block">
              <Button>Go to Login</Button>
            </Link>
          </div>
        </Card>
      ) : (
        <Card title="Account Information">
          <div className="grid gap-2 text-sm text-gray-700">
            <p>
              <span className="font-semibold">Name:</span> {user.name}
            </p>
            <p>
              <span className="font-semibold">Email:</span> {user.email}
            </p>
            <p>
              <span className="font-semibold">Role:</span> {user.role}
            </p>
          </div>

          <div className="mt-5 flex gap-3">
            <Button onClick={onLogout} variant="danger">
              Logout
            </Button>
            <Link href="/appointments" className="inline-block">
              <Button variant="secondary">My Appointments</Button>
            </Link>
          </div>
        </Card>
      )}
    </main>
  );
}
