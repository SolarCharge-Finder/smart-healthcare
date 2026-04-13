import { useQuery } from "@tanstack/react-query";
import { paymentApi } from "../lib/api";
import { StripeConfigResponse } from "../types/payment";

export function useStripeConfig() {
  return useQuery<StripeConfigResponse>({
    queryKey: ["stripe-config"],
    queryFn: async () => {
      const { data } = await paymentApi.get<StripeConfigResponse>(
        "/payments/config"
      );
      return data;
    },
    staleTime: Infinity, // Config never changes during session
  });
}
