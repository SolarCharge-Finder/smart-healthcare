"use client";

import { useState } from "react";
import { AxiosError } from "axios";
import { useAppointments } from "../../hooks/useAppointments";
import { useCancelAppointment } from "../../hooks/useCancelAppointment";
import Card from "../ui/Card";
import Button from "../ui/Button";
import Alert from "../ui/Alert";
import Spinner from "../ui/Spinner";

export default function AppointmentTable() {
  const appointments = useAppointments();
  const cancelAppointment = useCancelAppointment();
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const handleCancel = async (id: string) => {
    setMessage(null);
    setError(null);

    try {
      await cancelAppointment.mutateAsync(id);
      setMessage("Appointment cancelled successfully.");
    } catch (err) {
      const axiosError = err as AxiosError;
      const responseMessage =
        typeof axiosError.response?.data === "string"
          ? axiosError.response.data
          : null;

      if (responseMessage) {
        setError(responseMessage);
      } else if (axiosError.response?.status === 500) {
        setError("Server error. Please try again later.");
      } else {
        setError("Unable to cancel appointment.");
      }
    }
  };

  return (
    <Card title="Appointment History">
      <div className="space-y-4">
        {appointments.isLoading ? (
          <div className="flex items-center gap-2 text-sm text-gray-600">
            <Spinner />
            Loading appointments...
          </div>
        ) : null}

        {appointments.isError ? (
          <Alert type="error">Failed to load appointments.</Alert>
        ) : null}

        {message ? <Alert type="success">{message}</Alert> : null}
        {error ? <Alert type="error">{error}</Alert> : null}

        <div className="rounded-xl overflow-hidden border border-gray-200 shadow-sm">
          <div className="overflow-x-auto">
            <table className="w-full text-left text-sm">
              <thead className="bg-gray-100 font-semibold text-gray-700">
                <tr>
                  <th className="px-4 py-3">Appointment ID</th>
                  <th className="px-4 py-3">Doctor ID</th>
                  <th className="px-4 py-3">Patient ID</th>
                  <th className="px-4 py-3">Slot Time</th>
                  <th className="px-4 py-3">Status</th>
                  <th className="px-4 py-3">Created At</th>
                  <th className="px-4 py-3">Actions</th>
                </tr>
              </thead>
              <tbody>
                {appointments.data?.map((appointment) => (
                  <tr
                    key={appointment.id}
                    className="border-t border-gray-100 hover:bg-gray-50"
                  >
                    <td className="px-4 py-3 font-mono text-xs">
                      {appointment.id}
                    </td>
                    <td className="px-4 py-3 font-mono text-xs">
                      {appointment.doctorId}
                    </td>
                    <td className="px-4 py-3 font-mono text-xs">
                      {appointment.patientId}
                    </td>
                    <td className="px-4 py-3">
                      {new Date(appointment.slotTime).toLocaleString()}
                    </td>
                    <td className="px-4 py-3">
                      <span className="rounded-full bg-gray-100 px-2 py-1 text-xs text-gray-700">
                        {appointment.status}
                      </span>
                    </td>
                    <td className="px-4 py-3">
                      {new Date(appointment.createdAt).toLocaleString()}
                    </td>
                    <td className="px-4 py-3">
                      <Button
                        variant="danger"
                        onClick={() => handleCancel(appointment.id)}
                        disabled={cancelAppointment.isPending}
                      >
                        {cancelAppointment.isPending ? "Cancelling..." : "Cancel"}
                      </Button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </Card>
  );
}
