import { useQuery } from "@tanstack/react-query";
import api from "../lib/api";
import { StripeConfigResponse } from "../types/payment";

export function useStripeConfig() {
  return useQuery<StripeConfigResponse>({
    queryKey: ["stripe-config"],
    queryFn: async () => {
      const { data } = await api.get<StripeConfigResponse>(
        "/payments/config"
      );
      return data;
    },
    staleTime: Infinity, // Config never changes during session
  });
}
