import SymptomCheckerForm from "../../components/forms/SymptomCheckerForm";
import PageHeader from "../../components/ui/PageHeader";

export default function AiCheckerPage() {
  return (
    <main className="mx-auto flex min-h-screen max-w-6xl flex-col gap-6 px-6 py-10">
      <PageHeader
        title="AI Symptom Checker"
        subtitle="Describe symptoms to receive instant guidance."
      />
      <div className="max-w-3xl">
        <SymptomCheckerForm />
      </div>
    </main>
  );
}
