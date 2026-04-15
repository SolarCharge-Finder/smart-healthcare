import api from "./api";

export interface SymptomAnalysisRequest {
  symptoms: string;
  patientId?: string;
  sessionToken?: string;
}

export interface SymptomAnalysisResponse {
  success: boolean;
  analysis?: string;
  possibleConditions?: string[];
  confidenceScore?: number;
  recommendedSpecialty?: string;
  urgency?: string;
  disclaimer?: string;
  tokensUsed?: number;
  costUsd?: number;
  modelUsed?: string;
  responseTimeMs?: number;
  correlationId?: string;
  error?: string;
}

type RawSymptomAnalysisResponse = {
  success?: unknown;
  analysis?: unknown;
  possibleConditions?: unknown;
  confidenceScore?: unknown;
  recommendedSpecialty?: unknown;
  urgency?: unknown;
  disclaimer?: unknown;
  tokensUsed?: unknown;
  costUsd?: unknown;
  modelUsed?: unknown;
  responseTimeMs?: unknown;
  correlationId?: unknown;
  error?: unknown;
};

const isObject = (value: unknown): value is Record<string, unknown> =>
  typeof value === "object" && value !== null;

const toStringOrUndefined = (value: unknown): string | undefined =>
  typeof value === "string" && value.trim().length > 0 ? value.trim() : undefined;

const toNumberOrUndefined = (value: unknown): number | undefined =>
  typeof value === "number" && Number.isFinite(value) ? value : undefined;

const sanitizePlainText = (value: string | undefined): string | undefined => {
  if (!value) {
    return undefined;
  }

  const cleaned = value
    .replace(/\*\*/g, "")
    .replace(/__+/g, "")
    .replace(/`+/g, "")
    .replace(/\s+/g, " ")
    .trim();

  return cleaned.length > 0 ? cleaned : undefined;
};

const normalizeUrgency = (value: unknown): string | undefined => {
  if (typeof value !== "string") {
    return undefined;
  }

  const normalized = value.trim().toLowerCase();
  if (normalized === "low") return "Low";
  if (normalized === "medium") return "Medium";
  if (normalized === "high") return "High";
  if (normalized === "emergency") return "Emergency";
  return undefined;
};

const normalizeConditions = (value: unknown): string[] | undefined => {
  if (!Array.isArray(value)) {
    return undefined;
  }

  const sanitized = value
    .filter((item): item is string => typeof item === "string")
    .map((item) => item.trim())
    .filter((item) => item.length > 0)
    .slice(0, 8);

  return sanitized.length > 0 ? sanitized : undefined;
};

const mapAnalyzeResponse = (raw: unknown): SymptomAnalysisResponse => {
  if (!isObject(raw)) {
    return {
      success: false,
      error: "Invalid AI response format received from server.",
    };
  }

  const data = raw as RawSymptomAnalysisResponse;
  const success = data.success === true;
  const correlationId = toStringOrUndefined(data.correlationId);

  if (!success) {
    return {
      success: false,
      error: toStringOrUndefined(data.error) || "Analysis failed. Please try again.",
      correlationId,
    };
  }

  const confidenceRaw = toNumberOrUndefined(data.confidenceScore);
  const confidenceScore =
    confidenceRaw === undefined ? undefined : Math.max(0, Math.min(1, confidenceRaw));

  return {
    success: true,
    analysis: sanitizePlainText(toStringOrUndefined(data.analysis)),
    possibleConditions: normalizeConditions(data.possibleConditions),
    confidenceScore,
    recommendedSpecialty: toStringOrUndefined(data.recommendedSpecialty),
    urgency: normalizeUrgency(data.urgency),
    disclaimer: sanitizePlainText(toStringOrUndefined(data.disclaimer)),
    tokensUsed: toNumberOrUndefined(data.tokensUsed),
    costUsd: toNumberOrUndefined(data.costUsd),
    modelUsed: toStringOrUndefined(data.modelUsed),
    responseTimeMs: toNumberOrUndefined(data.responseTimeMs),
    correlationId,
  };
};

export const analyzeSymptoms = async (
  request: SymptomAnalysisRequest
): Promise<SymptomAnalysisResponse> => {
  try {
    const response = await api.post(
      "/api/ai/analyze",
      request,
      {
        timeout: 30000, // 30 second timeout
      }
    );

    return mapAnalyzeResponse(response.data);
  } catch (error: any) {
    console.error("AI analysis error:", error);

    // Return error response consistent with backend format
    return {
      success: false,
      error:
        error.response?.data?.error ||
        error.message ||
        "Failed to analyze symptoms. Please try again.",
      correlationId: error.response?.data?.correlationId,
    };
  }
};

export const getAiHealth = async (): Promise<{ status: string }> => {
  try {
    const response = await api.get("/api/ai/health");
    return response.data;
  } catch (error) {
    console.error("Health check error:", error);
    return { status: "unavailable" };
  }
};

export const getMetrics = async (): Promise<string> => {
  try {
    const response = await api.get("/metrics", {
      responseType: "text",
    });
    return response.data;
  } catch (error) {
    console.error("Metrics fetch error:", error);
    return "";
  }
};
