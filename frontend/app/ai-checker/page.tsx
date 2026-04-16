import SymptomCheckerForm from '../../components/forms/SymptomCheckerForm';
import PageHeader from '../../components/ui/PageHeader';

export default function AiCheckerPage() {
  return (
    <main className="mx-auto flex min-h-screen w-full max-w-[1400px] flex-col gap-6 px-4 py-8 sm:px-6 lg:px-10">
      <PageHeader
        title="AI Symptom Checker"
        subtitle="Describe symptoms to receive instant guidance."
      />
      <div className="w-full">
        <SymptomCheckerForm />
      </div>
    </main>
  );
}
