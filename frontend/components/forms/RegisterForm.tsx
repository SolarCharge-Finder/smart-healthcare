"use client";

import { FormEvent, useState } from "react";
import Card from "../ui/Card";
import Input from "../ui/Input";
import Button from "../ui/Button";
import Alert from "../ui/Alert";

import { registerApi } from "../../modules/auth/authApi";

export default function RegisterForm() {
  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");

  const [success, setSuccess] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const onSubmit = async (event: FormEvent) => {
    event.preventDefault();
    setError(null);
    setSuccess(false);

    try {
      await registerApi({ name, email, password });

      setSuccess(true);

      // clear form
      setName("");
      setEmail("");
      setPassword("");
    } catch (err: any) {
      console.error(err);
      setError("Registration failed. Try again.");
    }
  };

  return (
    <Card title="New here? Create an account">
      <form className="space-y-4" onSubmit={onSubmit}>
        <Input
          label="Name"
          value={name}
          onChange={(e) => setName(e.target.value)}
          required
        />
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

        {success && (
          <Alert type="success">
            Account created successfully. You can now log in.
          </Alert>
        )}

        {error && <Alert type="error">{error}</Alert>}

        <Button type="submit">Register</Button>
      </form>
    </Card>
  );
}