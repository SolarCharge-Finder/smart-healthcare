import { useMutation } from "@tanstack/react-query";
import axios from "axios";
import { telemedicineApi } from "../lib/api";
import { TelemedicineSessionResponse, CreateSessionRequest } from "../types/telemedicine";

function extractTelemedicineError(error: unknown, fallback: string): Error {
  if (axios.isAxiosError(error)) {
    const responseData = error.response?.data;
    const detail = error.response?.data?.error;
    const message =
      (typeof detail === "string" && detail.trim()) ||
      (typeof responseData === "string" &&
        responseData.trim() &&
        !responseData.includes("<!DOCTYPE html") &&
        !responseData.includes("<html") &&
        responseData.trim()) ||
      (error.response?.status === 404
        ? "Telemedicine service could not find the requested resource. Please retry in a few seconds."
        : error.response?.status === 503
          ? "Telemedicine service is temporarily unavailable. Please retry shortly."
          : undefined) ||
      (typeof error.message === "string" && error.message.trim()) ||
      fallback;
    return new Error(message);
  }
  if (error instanceof Error && error.message.trim()) return new Error(error.message);
  return new Error(fallback);
}

/**
 * Create a new telemedicine session (POST /telemedicine/session).
 * TelemedicineService validates appointment is "Paid" before generating tokens.
 */
export function useCreateTelemedicineSession() {
  return useMutation<TelemedicineSessionResponse, Error, CreateSessionRequest>({
    mutationFn: async (payload) => {
      try {
        const { data } = await telemedicineApi.post<TelemedicineSessionResponse>(
          "/telemedicine/session",
          payload
        );
        return data;
      } catch (error) {
        throw extractTelemedicineError(error, "Failed to create telemedicine session.");
      }
    },
  });
}
