import { useMutation } from "@tanstack/react-query";
import { createAdmin, createDoctor, createPatient } from "./profileApi";

export function useCompleteAsPatient() {
  return useMutation({
    mutationFn: createPatient,
  });
}

export function useCompleteAsDoctor() {
  return useMutation({
    mutationFn: createDoctor,
  });
}

export function useCompleteAsAdmin() {
  return useMutation({
    mutationFn: createAdmin,
  });
}