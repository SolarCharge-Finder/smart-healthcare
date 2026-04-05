"use client";

import { FormEvent, useState } from "react";
import Card from "../ui/Card";
import Button from "../ui/Button";
import { analyzeSymptoms, SymptomAnalysisResponse } from "@/lib/aiService";

export default function SymptomCheckerForm() {
  const [symptoms, setSymptoms] = useState("");
  const [result, setResult] = useState<SymptomAnalysisResponse | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

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
    <Card title="AI Symptom Checker">
      <form className="space-y-4" onSubmit={onSubmit}>
        <label className="block space-y-1">
          <span className="text-sm font-medium text-gray-700">Symptoms</span>
          <textarea
            className="w-full rounded-lg border border-gray-300 bg-white p-3 text-sm outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-500"
            rows={6}
            value={symptoms}
            onChange={(e) => setSymptoms(e.target.value)}
            placeholder="Describe your symptoms in detail (minimum 10 characters)..."
            required
            minLength={10}
            maxLength={2000}
          />
        </label>

        {/* Loading State */}
        {loading && (
          <div className="rounded-lg border border-blue-200 bg-blue-50 p-4 text-sm text-blue-800">
            <div className="flex items-center gap-2">
              <div className="h-4 w-4 animate-spin rounded-full border-2 border-blue-500 border-t-transparent"></div>
              Analyzing your symptoms...
            </div>
          </div>
        )}

        {/* Error State */}
        {error && (
          <div className="rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-800">
            <div className="font-semibold">Error</div>
            <div>{error}</div>
          </div>
        )}

        {/* Success Result */}
        {result && result.success && (
          <div className="space-y-3 rounded-lg border border-green-200 bg-green-50 p-4 text-sm text-green-800">
            <div className="font-semibold text-green-900">Analysis Results</div>

            {result.analysis && (
              <div>
                <div className="font-medium">Assessment:</div>
                <p className="mt-1">{result.analysis}</p>
              </div>
            )}

            {result.recommendedSpecialty && (
              <div>
                <div className="font-medium">Recommended Specialty:</div>
                <p className="mt-1">{result.recommendedSpecialty}</p>
              </div>
            )}

            {result.urgencyLevel && (
              <div>
                <div className="font-medium">Urgency Level:</div>
                <p className="mt-1 font-semibold text-orange-600">
                  {result.urgencyLevel}
                </p>
              </div>
            )}

            {result.disclaimer && (
              <div className="border-t border-green-200 pt-2 text-xs italic">
                ⚠️ {result.disclaimer}
              </div>
            )}

            {result.responseTimeMs && (
              <div className="border-t border-green-200 pt-2 text-xs">
                Response time: {result.responseTimeMs}ms | Model: {result.modelUsed}
              </div>
            )}
          </div>
        )}

        <div className="flex gap-2">
          <Button type="submit" disabled={loading || symptoms.length < 10}>
            {loading ? "Analyzing..." : "Check Symptoms"}
          </Button>
          {symptoms && (
            <Button
              type="button"
              onClick={() => {
                setSymptoms("");
                setResult(null);
                setError(null);
              }}
              className="bg-gray-400 hover:bg-gray-500"
            >
              Clear
            </Button>
          )}
        </div>
      </form>
    </Card>
  );
}
