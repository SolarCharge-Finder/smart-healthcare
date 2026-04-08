"use client";

import { useEffect, useState, useRef } from "react"; 
import { useSearchParams } from "next/navigation";
import apiClient from "../../shared/apiClient";

export default function VerifyPage() {
  const searchParams = useSearchParams();
  const token = searchParams.get("token");

  const [status, setStatus] = useState("Verifying...");
  const hasRun = useRef(false); 

  useEffect(() => {
    if (!token || hasRun.current) return; 

    hasRun.current = true; 

    const verify = async () => {
      try {
        await apiClient.post("/auth/verify", { token });

        setStatus("Email verified successfully!");
      } catch (err: any) {
        console.error(err);

        const message = err?.response?.data?.message;

        // handle common cases: already verified or expired token
        if (
          message?.includes("Invalid token") ||
          message?.includes("expired")
        ) {
          setStatus("Already verified or link expired.");
        } else {
          setStatus("Verification failed.");
        }
      }
    };

    verify();
  }, [token]); 

  return (
    <div className="flex min-h-screen items-center justify-center">
      <div className="text-center">
        <h1 className="text-xl font-semibold">{status}</h1>
      </div>
    </div>
  );
}