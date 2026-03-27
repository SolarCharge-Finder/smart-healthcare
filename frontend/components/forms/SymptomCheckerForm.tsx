"use client";

import { FormEvent, useState } from "react";
import Card from "../ui/Card";
import Button from "../ui/Button";

export default function SymptomCheckerForm() {
  const [symptoms, setSymptoms] = useState("");
  const [result, setResult] = useState<string | null>(null);

  const onSubmit = (event: FormEvent) => {
    event.preventDefault();
    setResult(
      "Possible condition: Seasonal flu. Recommended specialty: General Physician. Urgency: Moderate."
    );
  };

  return (
    <Card title="AI Symptom Checker">
      <form className="space-y-4" onSubmit={onSubmit}>
        <label className="block space-y-1">
          <span className="text-sm font-medium text-gray-700">
            Symptoms
          </span>
          <textarea
            className="w-full rounded-lg border border-gray-300 bg-white p-3 text-sm outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-500"
            rows={6}
            value={symptoms}
            onChange={(e) => setSymptoms(e.target.value)}
            placeholder="Describe your symptoms..."
            required
          />
        </label>

        {result ? (
          <div className="rounded-lg border border-blue-200 bg-blue-50 p-4 text-sm text-blue-800">
            {result}
          </div>
        ) : null}

        <Button type="submit">Check Symptoms</Button>
      </form>
    </Card>
  );
}
