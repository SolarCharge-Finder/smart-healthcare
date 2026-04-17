'use client';

import Card from '@/components/ui/Card';
import Alert from '@/components/ui/Alert';
import Button from '@/components/ui/Button';
import { useAdmin } from './hooks/useAdmin';

export default function AdminDashboard() {
  const { pendingAdminsQuery, approveAdmin, rejectAdmin, pendingDoctorsQuery, approveDoctor, rejectDoctor } =
    useAdmin();

  return (
    <Card title="Admin Dashboard">
      {/* ---------------- ADMIN REQUESTS ---------------- */}
      <div className="mb-8">
        <h3 className="font-semibold mb-2">Pending Admin Requests</h3>

        {pendingAdminsQuery.isLoading && <Alert type="info">Loading admin requests...</Alert>}

        {pendingAdminsQuery.data?.length === 0 && (
          <Alert type="info">No pending admin requests</Alert>
        )}

        {pendingAdminsQuery.data?.map((admin: any) => (
          <div key={admin.id} className="border p-3 rounded mb-2 flex justify-between items-center">
            <div>
              <p className="font-medium">{admin.fullName}</p>
              <p className="text-sm text-gray-500">{admin.id}</p>
            </div>

            <div className="flex gap-2">
              <Button
                onClick={() => approveAdmin.mutate(admin.id)}
                disabled={approveAdmin.isPending}
              >
                Approve
              </Button>

              <Button
                variant="danger"
                onClick={() => rejectAdmin.mutate(admin.id)}
                disabled={rejectAdmin.isPending}
              >
                Reject
              </Button>
            </div>
          </div>
        ))}
      </div>

      {/* ---------------- DOCTOR REQUESTS ---------------- */}
      <div>
        <h3 className="font-semibold mb-2">Pending Doctor Requests</h3>

        {pendingDoctorsQuery.isLoading && <Alert type="info">Loading doctor requests...</Alert>}

        {pendingDoctorsQuery.data?.length === 0 && (
          <Alert type="info">No pending doctor requests</Alert>
        )}

        {pendingDoctorsQuery.data?.map((doctor: any) => (
          <div
            key={doctor.id}
            className="border p-3 rounded mb-2 flex justify-between items-center"
          >
            <div>
              <p className="font-medium">{doctor.fullName}</p>
              <p className="text-sm text-gray-500">{doctor.id}</p>
              {/* <p className="text-sm text-gray-500">{doctor.specialization}</p> */}
            </div>

            <div className='flex gap-2'>
              <Button
                onClick={() => approveDoctor.mutate(doctor.id)}
                disabled={approveDoctor.isPending}
              >
                Approve
              </Button>
              <Button
                variant="danger"
                onClick={() => rejectDoctor.mutate(doctor.id)}
                disabled={rejectDoctor.isPending}
              >
                Reject
              </Button>
            </div>
          </div>
        ))}
      </div>
    </Card>
  );
}
