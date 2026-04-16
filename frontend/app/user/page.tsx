'use client';

import { useRouter } from 'next/navigation';
import Link from 'next/link';
import { useState } from 'react';

import Button from '../../components/ui/Button';
import Card from '../../components/ui/Card';
import PageHeader from '../../components/ui/PageHeader';

import { useAuthContext } from '../../modules/auth/AuthContext';
import {
  useCompleteAsAdmin,
  useCompleteAsDoctor,
  useCompleteAsPatient,
} from '../../modules/profile/useCompleteProfile';
import { useRefreshUser } from '@/modules/auth/useAuthUser';

import DoctorDashboard from '@/modules/doctors/DoctorDashboard';
import AdminDashboard from '@/modules/admin/AdminDashboard';

export default function UserPage() {
  const router = useRouter();
  const { user, logout, setUser } = useAuthContext();

  const refreshUser = useRefreshUser();

  const patientMutation = useCompleteAsPatient();
  const doctorMutation = useCompleteAsDoctor();
  const adminMutation = useCompleteAsAdmin();

  // Doctor dialog state
  const [showDoctorDialog, setShowDoctorDialog] = useState(false);
  const [doctorForm, setDoctorForm] = useState({
    fullName: '',
    specialization: '',
    hospital: '',
  });

  const onLogout = () => {
    logout();
    router.push('/auth');
  };

  const handlePatientSetup = async () => {
    if (!user) return;

    try {
      await patientMutation.mutateAsync({
        fullName: user.name,
        email: user.email,
      });

      const refreshed = await refreshUser.mutateAsync();
      setUser(refreshed);
    } catch (error) {
      console.error('Patient setup failed:', error);
    }
  };

  const handleDoctorSubmit = async () => {
    try {
      await doctorMutation.mutateAsync(doctorForm);

      const refreshed = await refreshUser.mutateAsync();
      setUser(refreshed);

      setShowDoctorDialog(false);
    } catch (error) {
      console.error('Doctor setup failed:', error);
    }
  };

  const handleAdminSetup = async () => {
    if (!user) return;

    try {
      await adminMutation.mutateAsync({
        fullName: user.name,
      });

      const refreshed = await refreshUser.mutateAsync();
      setUser(refreshed);
    } catch (error) {
      console.error('Admin setup failed:', error);
    }
  };

  return (
    <main className="mx-auto flex min-h-screen max-w-4xl flex-col gap-6 px-6 py-10">
      {' '}
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
            <Link href="/auth">
              <Button>Go to Login</Button>
            </Link>
          </div>
        </Card>
      ) : (
        <>
          {/* Account Info */}
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
              <Link href="/appointments">
                <Button variant="secondary">My Appointments</Button>
              </Link>
            </div>
          </Card>

          {/* role based dashboards */}
          {user.role === 'Doctor' && <DoctorDashboard />}
          {user.role === 'Admin' && <AdminDashboard />}

          {/* Account Setup */}
          {user.role === 'Undefined' && (
            <Card title="Complete Account Setup">
              <p className="text-sm text-gray-600 mb-4">Register to continue using SmartHealth.</p>

              <div className="flex flex-col gap-3">
                <Button onClick={handlePatientSetup} disabled={patientMutation.isPending}>
                  {patientMutation.isPending ? 'Setting up...' : 'Register as Patient'}
                </Button>

                <Button variant="secondary" onClick={() => setShowDoctorDialog(true)}>
                  Register as Doctor
                </Button>

                <Button
                  variant="secondary"
                  onClick={handleAdminSetup}
                  disabled={adminMutation.isPending}
                >
                  {adminMutation.isPending ? 'Setting up...' : 'Register as Admin'}
                </Button>
              </div>
            </Card>
          )}

          {/* Doctor Dialog */}
          {showDoctorDialog && (
            <div className="fixed inset-0 flex items-center justify-center bg-black/50">
              <div className="bg-white p-6 rounded-xl w-full max-w-md">
                <h2 className="text-lg font-semibold mb-4">Doctor Registration</h2>

                <input
                  className="w-full border p-2 mb-3"
                  placeholder="Full Name"
                  value={doctorForm.fullName}
                  onChange={(e) =>
                    setDoctorForm({
                      ...doctorForm,
                      fullName: e.target.value,
                    })
                  }
                />

                <select
                  className="w-full border p-2 mb-3"
                  value={doctorForm.specialization}
                  onChange={(e) =>
                    setDoctorForm({
                      ...doctorForm,
                      specialization: e.target.value,
                    })
                  }
                >
                  <option value="">Select Specialization</option>
                  <option value="General">General</option>
                  <option value="Cardiology">Cardiology</option>
                  <option value="Dermatology">Dermatology</option>
                </select>

                <select
                  className="w-full border p-2 mb-4"
                  value={doctorForm.hospital}
                  onChange={(e) =>
                    setDoctorForm({
                      ...doctorForm,
                      hospital: e.target.value,
                    })
                  }
                >
                  <option value="">Select Hospital</option>
                  <option value="City Hospital">City Hospital</option>
                  <option value="National Hospital">National Hospital</option>
                </select>

                <div className="flex gap-2">
                  <Button onClick={handleDoctorSubmit} disabled={doctorMutation.isPending}>
                    {doctorMutation.isPending ? 'Saving...' : 'Submit'}
                  </Button>

                  <Button variant="secondary" onClick={() => setShowDoctorDialog(false)}>
                    Cancel
                  </Button>
                </div>
              </div>
            </div>
          )}
        </>
      )}
    </main>
  );
}
