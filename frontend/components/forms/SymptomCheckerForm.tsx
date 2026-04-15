"use client";

import { FormEvent, useMemo, useRef, useState } from "react";
import Card from "../ui/Card";
import Button from "../ui/Button";
import { analyzeSymptoms, SymptomAnalysisResponse } from "@/lib/aiService";

const getUrgencyStyles = (urgency?: string) => {
  switch (urgency) {
    case "Low":
      return {
        badge: "bg-emerald-100 text-emerald-800 border-emerald-300",
        bar: "bg-emerald-500",
        width: "30%",
      };
    case "Medium":
      return {
        badge: "bg-amber-100 text-amber-800 border-amber-300",
        bar: "bg-amber-500",
        width: "55%",
      };
    case "High":
      return {
        badge: "bg-orange-100 text-orange-800 border-orange-300",
        bar: "bg-orange-600",
        width: "78%",
      };
    case "Emergency":
      return {
        badge: "bg-rose-100 text-rose-800 border-rose-300",
        bar: "bg-rose-600",
        width: "100%",
      };
    default:
      return {
        badge: "bg-slate-100 text-slate-700 border-slate-300",
        bar: "bg-slate-400",
        width: "0%",
      };
  }
};

const getUrgencyMessage = (urgency?: string) => {
  switch (urgency) {
    case "Low":
      return "Usually safe to monitor at home. Seek care if symptoms get worse.";
    case "Medium":
      return "You should plan a doctor visit soon, especially if symptoms continue.";
    case "High":
      return "You should seek medical care today.";
    case "Emergency":
      return "Get emergency care now or call emergency services immediately.";
    default:
      return "Urgency could not be classified.";
  }
};

const getConfidenceMessage = (confidencePercent: number) => {
  if (confidencePercent >= 80) {
    return "High confidence. The symptom pattern strongly matches this result.";
  }

  if (confidencePercent >= 50) {
    return "Moderate confidence. This is a useful direction, but not a diagnosis.";
  }

  return "Lower confidence. Share more symptom details and follow up with a clinician.";
};

const extractNextSteps = (analysis?: string): string[] => {
  if (!analysis) {
    return [];
  }

  const marker = "Next Steps:";
  const index = analysis.indexOf(marker);
  if (index === -1) {
    return [];
  }

  return analysis
    .slice(index + marker.length)
    .split("-")
    .map((step) => step.trim())
    .filter((step) => step.length > 0)
    .slice(0, 5);
};

const removeNextStepsFromSummary = (analysis?: string): string => {
  if (!analysis) {
    return "No summary available.";
  }

  if (analysis.toUpperCase().includes("OFFLINE ANALYSIS")) {
    return "The AI service is temporarily unavailable. This result is a backup estimate and should be confirmed by a healthcare professional.";
  }

  const marker = "Next Steps:";
  const index = analysis.indexOf(marker);
  const summary = index === -1 ? analysis : analysis.slice(0, index);
  return summary.trim() || "No summary available.";
};

const normalizeForComparison = (value: string): string =>
  value.toLowerCase().replace(/[^a-z0-9]/g, "");

const buildAssessmentText = (
  summary: string,
  possibleConditions: string[] | undefined,
  recommendedSpecialty: string | undefined,
  urgency: string | undefined
): string => {
  if (!possibleConditions || possibleConditions.length === 0) {
    return summary;
  }

  const summaryNormalized = normalizeForComparison(summary);
  const conditionsJoined = possibleConditions.join(", ");
  const conditionsNormalized = normalizeForComparison(conditionsJoined);

  if (summaryNormalized === conditionsNormalized) {
    const specialty = recommendedSpecialty || "General Medicine";
    const urgencyText = urgency || "Not classified";
    return `Based on the symptom pattern, the likely causes are listed below. Please consult ${specialty} for proper evaluation. Current urgency level is ${urgencyText}.`;
  }

  return summary;
};

export default function SymptomCheckerForm() {
  const [symptoms, setSymptoms] = useState("");
  const [result, setResult] = useState<SymptomAnalysisResponse | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const textareaRef = useRef<HTMLTextAreaElement | null>(null);

  const urgencyValue = result?.urgency;
  const urgencyStyles = getUrgencyStyles(urgencyValue);
  const confidencePercent = Math.round((result?.confidenceScore ?? 0) * 100);
  const summaryText = useMemo(() => removeNextStepsFromSummary(result?.analysis), [result?.analysis]);
  const assessmentText = useMemo(
    () =>
      buildAssessmentText(
        summaryText,
        result?.possibleConditions,
        result?.recommendedSpecialty,
        urgencyValue
      ),
    [summaryText, result?.possibleConditions, result?.recommendedSpecialty, urgencyValue]
  );
  const nextSteps = useMemo(() => extractNextSteps(result?.analysis), [result?.analysis]);

  const autoResizeTextarea = () => {
    const textarea = textareaRef.current;
    if (!textarea) {
      return;
    }

    textarea.style.height = "auto";
    textarea.style.height = `${Math.min(textarea.scrollHeight, 420)}px`;
  };

  const onSubmit = async (event: FormEvent) => {
    event.preventDefault();
    setLoading(true);
    setError(null);
    setResult(null);

    try {
      const response = await analyzeSymptoms({ symptoms });
      setResult(response);

      if (!response.success) {
        setError(response.error || "Analysis failed. Please try again.");
      }
    } catch (err: any) {
      setError(err.message || "An unexpected error occurred");
      console.error("Analysis error:", err);
    } finally {
      setLoading(false);
    }
  };

  return (
    <Card title="AI Clinical Triage Assistant">
      <form className="space-y-6" onSubmit={onSubmit}>
        <div className="rounded-2xl border border-sky-100 bg-gradient-to-r from-sky-50 via-white to-cyan-50 p-5 text-base text-slate-700">
          <div className="font-semibold text-slate-900">Structured Clinical Intake</div>
          <div className="mt-1">Provide symptom details to generate a structured triage response with urgency and likely conditions.</div>
        </div>

        <label className="block space-y-1">
          <span className="text-base font-semibold text-slate-800">Symptoms</span>
          <textarea
            ref={textareaRef}
            className="min-h-[130px] w-full rounded-2xl border border-slate-300 bg-white p-4 text-base leading-7 text-slate-900 outline-none focus:border-cyan-600 focus:ring-2 focus:ring-cyan-500"
            rows={4}
            value={symptoms}
            onChange={(e) => {
              setSymptoms(e.target.value);
              autoResizeTextarea();
            }}
            placeholder="Describe symptom onset, severity, duration, associated symptoms, and known risk factors."
            required
            minLength={10}
            maxLength={2000}
          />
          <div className="text-xs text-slate-500">
            {symptoms.length}/2000 characters
          </div>
        </label>

        {loading && (
          <div className="rounded-xl border border-cyan-200 bg-cyan-50 p-4 text-sm text-cyan-900">
            <div className="flex items-center gap-2">
              <div className="h-4 w-4 animate-spin rounded-full border-2 border-cyan-600 border-t-transparent"></div>
              Running structured clinical analysis...
            </div>
          </div>
        )}

        {error && (
          <div className="rounded-xl border border-rose-200 bg-rose-50 p-4 text-sm text-rose-800">
            <div className="font-semibold">Analysis Error</div>
            <div>{error}</div>
            {result?.correlationId && (
              <div className="mt-2 text-xs text-rose-700">Correlation ID: {result.correlationId}</div>
            )}
          </div>
        )}

        {result && result.success && (
          <div className="space-y-5 rounded-2xl border border-slate-200 bg-slate-50 p-6 text-base text-slate-800">
            <div className="grid gap-4 lg:grid-cols-2">
              <div className="rounded-2xl border border-slate-200 bg-white p-5">
                <div className="mb-1 text-xs font-semibold uppercase tracking-wide text-slate-500">How serious is this right now?</div>
                <div className="flex items-center gap-2">
                  <span className={`rounded-full border px-3 py-1 text-sm font-semibold ${urgencyStyles.badge}`}>
                    {urgencyValue || "Not classified"}
                  </span>
                </div>
                <div className="mt-3 h-2.5 w-full overflow-hidden rounded-full bg-slate-200">
                  <div
                    className={`h-full rounded-full ${urgencyStyles.bar}`}
                    style={{ width: urgencyStyles.width }}
                  />
                </div>
                <p className="mt-3 text-sm text-slate-700">{getUrgencyMessage(urgencyValue)}</p>
              </div>

              <div className="rounded-2xl border border-slate-200 bg-white p-5">
                <div className="mb-1 text-xs font-semibold uppercase tracking-wide text-slate-500">How sure is this result?</div>
                <div className="text-3xl font-bold text-sky-700">{confidencePercent}%</div>
                <div className="mt-3 h-2.5 w-full overflow-hidden rounded-full bg-slate-200">
                  <div
                    className="h-full rounded-full bg-sky-600"
                    style={{ width: `${confidencePercent}%` }}
                  />
                </div>
                <p className="mt-3 text-sm text-slate-700">{getConfidenceMessage(confidencePercent)}</p>
              </div>
            </div>

            <div>
              <div className="text-xs font-semibold uppercase tracking-wide text-slate-600">Assessment</div>
              <p className="mt-1 rounded-xl border border-slate-200 bg-white p-4 text-base leading-7 text-slate-800">
                {assessmentText}
              </p>
            </div>

            {nextSteps.length > 0 && (
              <div>
                <div className="mb-2 text-xs font-semibold uppercase tracking-wide text-slate-600">What You Should Do Next</div>
                <ul className="space-y-2 rounded-xl border border-slate-200 bg-white p-4 text-base text-slate-800">
                  {nextSteps.map((step) => (
                    <li key={step} className="flex gap-2">
                      <span className="mt-2 inline-block h-2 w-2 rounded-full bg-cyan-600" />
                      <span>{step}</span>
                    </li>
                  ))}
                </ul>
              </div>
            )}

            {result.possibleConditions && result.possibleConditions.length > 0 && (
              <div>
                <div className="mb-2 text-xs font-semibold uppercase tracking-wide text-slate-600">Possible Causes</div>
                <div className="flex flex-wrap gap-2">
                  {result.possibleConditions.map((condition) => (
                    <span
                      key={condition}
                      className="rounded-full border border-cyan-200 bg-cyan-100 px-3 py-1 text-sm font-medium text-cyan-900"
                    >
                      {condition}
                    </span>
                  ))}
                </div>
              </div>
            )}

            {result.recommendedSpecialty && (
              <div>
                <div className="text-xs font-semibold uppercase tracking-wide text-slate-600">Recommended Specialty</div>
                <p className="mt-1 rounded-xl border border-slate-200 bg-white p-4 text-base text-slate-800">{result.recommendedSpecialty}</p>
              </div>
            )}

            {result.disclaimer && (
              <div className="rounded-lg border border-amber-200 bg-amber-50 p-3 text-sm text-amber-900">
                <span className="font-semibold">Important:</span> {result.disclaimer}
              </div>
            )}

            <details className="rounded-xl border border-slate-200 bg-white p-4 text-sm text-slate-600">
              <summary className="cursor-pointer font-semibold text-slate-700">Technical details (for support team)</summary>
              <div className="mt-3 grid gap-2 md:grid-cols-2 xl:grid-cols-4">
                {typeof result.responseTimeMs === "number" && (
                  <div>Response Time: {result.responseTimeMs} ms</div>
                )}
                {typeof result.tokensUsed === "number" && (
                  <div>Tokens Used: {result.tokensUsed}</div>
                )}
                {typeof result.costUsd === "number" && (
                  <div>Cost (USD): {result.costUsd.toFixed(6)}</div>
                )}
                {result.modelUsed && <div>Model: {result.modelUsed}</div>}
                {result.correlationId && (
                  <div className="break-all md:col-span-2 xl:col-span-4">Correlation ID: {result.correlationId}</div>
                )}
              </div>
            </details>
          </div>
        )}

        <div className="flex flex-wrap gap-3">
          <Button type="submit" disabled={loading || symptoms.length < 10}>
            {loading ? "Analyzing..." : "Run Triage"}
          </Button>
          {symptoms && (
            <Button
              type="button"
              variant="secondary"
              onClick={() => {
                setSymptoms("");
                setResult(null);
                setError(null);
                if (textareaRef.current) {
                  textareaRef.current.style.height = "auto";
                }
              }}
            >
              Reset Input
            </Button>
          )}
        </div>
      </form>
    </Card>
  );
}
