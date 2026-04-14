import { redirect } from "next/navigation";

type ConsultationRouteProps = {
  params: {
    appointmentId: string;
  };
};

export default function ConsultationRoutePage({ params }: ConsultationRouteProps) {
  redirect(`/consultation?appointmentId=${encodeURIComponent(params.appointmentId)}`);
}
