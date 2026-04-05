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
  urgencyLevel?: string;
  disclaimer?: string;
  tokensUsed?: number;
  costUsd?: number;
  modelUsed?: string;
  responseTimeMs?: number;
  correlationId?: string;
  error?: string;
}

export const analyzeSymptoms = async (
  request: SymptomAnalysisRequest
): Promise<SymptomAnalysisResponse> => {
  try {
    const response = await api.post<SymptomAnalysisResponse>(
      "/api/ai/analyze",
      request,
      {
        timeout: 30000, // 30 second timeout
      }
    );

    return response.data;
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
