"use client";

import { FormEvent, useState } from "react";
import { useRouter } from "next/navigation";
import Card from "../ui/Card";
import Input from "../ui/Input";
import Button from "../ui/Button";
import Alert from "../ui/Alert";
import { useAuth } from "../../modules/auth/useAuth";

export default function LoginForm() {
  const { login } = useAuth();
  const router = useRouter();

  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");

  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const onSubmit = async (event: FormEvent) => {
    event.preventDefault();
    setError(null);
    setMessage(null);

    try {
      const data = await login(email, password);

      setMessage("Login successful.");

      // redirect based on role - for now just go to home
      // if (data.role === "Admin") router.push("/admin");
      // else if (data.role === "Doctor") router.push("/doctor");
      // else router.push("/patient"); - i mean this will prolly just be normal home ig so delete this later

      router.push("/"); // temporary redirect to home after login (role based routing later maybe)
    } catch (err: any) {
      console.error(err);
      setError("Invalid email or password.");
    }
  };

  return (
    <Card title="Already have an account? Log in">
      <form className="space-y-4" onSubmit={onSubmit}>
        <Input
          label="Email"
          type="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          required
        />
        <Input
          label="Password"
          type="password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          required
        />

        {message && <Alert type="success">{message}</Alert>}
        {error && <Alert type="error">{error}</Alert>}

        <Button type="submit">Login</Button>
      </form>
    </Card>
  );
}