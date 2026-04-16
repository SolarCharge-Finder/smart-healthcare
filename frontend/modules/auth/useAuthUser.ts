import { useMutation } from "@tanstack/react-query";
import { getCurrentUser } from "./authApi";

export function useRefreshUser() {
  return useMutation({
    mutationFn: getCurrentUser,
  });
}