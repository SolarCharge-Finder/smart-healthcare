import Card from "../../components/ui/Card";
import Button from "../../components/ui/Button";
import PageHeader from "../../components/ui/PageHeader";

export default function ConsultationPage() {
  return (
    <main className="mx-auto flex min-h-screen max-w-6xl flex-col gap-6 px-6 py-10">
      <PageHeader
        title="Video Consultation"
        subtitle="Connect with your doctor in a secure virtual room."
      />
      <Card title="Live Consultation (Mock)">
        <div className="grid gap-6 md:grid-cols-2">
          <div className="rounded-xl border border-gray-200 bg-gray-50 p-6 text-center">
            <p className="text-sm text-gray-600">Doctor Video Feed</p>
            <div className="mt-4 h-48 rounded-lg border border-gray-200 bg-white" />
          </div>
          <div className="rounded-xl border border-gray-200 bg-gray-50 p-6 text-center">
            <p className="text-sm text-gray-600">Patient Video Feed</p>
            <div className="mt-4 h-48 rounded-lg border border-gray-200 bg-white" />
          </div>
        </div>
        <div className="mt-6 flex gap-3">
          <Button variant="secondary">Mute</Button>
          <Button variant="secondary">Disable Video</Button>
          <Button variant="danger">End Call</Button>
        </div>
      </Card>
    </main>
  );
}
