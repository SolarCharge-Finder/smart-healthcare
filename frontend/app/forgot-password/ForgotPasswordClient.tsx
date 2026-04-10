"use client";

import { useState } from "react";
import apiClient from "../../shared/apiClient";
import Card from "../../components/ui/Card";
import Input from "../../components/ui/Input";
import Button from "../../components/ui/Button";
import Alert from "../../components/ui/Alert";

export default function ForgotPasswordClient() {
  const [email, setEmail] = useState("");
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    setMessage(null);
    setError(null);

    try {
      await apiClient.post("/auth/forgot-password", { email });

      setMessage(
        "If an account with that email exists, a reset link has been sent."
      );
    } catch (err: any) {
      console.error(err);
      setError("Something went wrong. Please try again.");
    }
  };

  return (
    <div className="flex min-h-screen items-center justify-center">
        <div className="w-full max-w-md">
            <Card title="Forgot Password">
                <form className="space-y-4" onSubmit={handleSubmit}>
                    <Input
                        label="Email"
                        type="email"
                        value={email}
                        onChange={(e) => setEmail(e.target.value)}
                        required
                    />

                    {message && <Alert type="success">{message}</Alert>}
                    {error && <Alert type="error">{error}</Alert>}

                    <Button type="submit">Send Reset Link</Button>
                </form>
            </Card>
        </div>
    </div>
  );
}