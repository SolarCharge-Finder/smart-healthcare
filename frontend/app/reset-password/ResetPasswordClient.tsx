"use client";

import { useState } from "react";
import { useSearchParams } from "next/navigation";
import apiClient from "../../shared/apiClient";

import Card from "../../components/ui/Card";
import Input from "../../components/ui/Input";
import Button from "../../components/ui/Button";
import Alert from "../../components/ui/Alert";


export default function ResetPasswordClient() {
  const searchParams = useSearchParams();
  const token = searchParams.get("token");

  const [password, setPassword] = useState("");
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const handleReset = async (e: React.FormEvent) => {
    e.preventDefault();

    setMessage(null);
    setError(null);

    if (!token) {
      setError("Invalid or missing reset token.");
      return;
    }

    try {
      await apiClient.post("/auth/reset-password", {
        token,
        newPassword: password,
      });

      setMessage("Password reset successful!");
    } catch (err: any) {
      console.error(err);

      const msg = err?.response?.data?.message;

      if (
        msg?.includes("Invalid token") ||
        msg?.includes("expired")
      ) {
        setError("Reset link is invalid or expired.");
      } else {
        setError("Password reset failed.");
      }
    }
  };

  return (
    <div className="flex min-h-screen items-center justify-center">
      <div className="w-full max-w-md">
        <Card title="Reset Password">
          <form className="space-y-6" onSubmit={handleReset}>
            <Input
              label="New Password"
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
            />

            {message && <Alert type="success">{message}</Alert>}
            {error && <Alert type="error">{error}</Alert>}

            <Button type="submit" className="w-full">
              Reset Password
            </Button>
          </form>
        </Card>
      </div>
    </div>
  );
}