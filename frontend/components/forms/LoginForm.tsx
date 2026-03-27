"use client";

import { FormEvent, useState } from "react";
import Card from "../ui/Card";
import Input from "../ui/Input";
import Button from "../ui/Button";
import Alert from "../ui/Alert";

export default function LoginForm() {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [message, setMessage] = useState<string | null>(null);

  const onSubmit = (event: FormEvent) => {
    event.preventDefault();
    const userJson = localStorage.getItem("smarthealth:user");
    const user = userJson ? JSON.parse(userJson) : null;

    if (user && user.email === email) {
      localStorage.setItem("smarthealth:session", "active");
      setMessage("Mock login successful.");
    } else {
      setMessage("User not found. Register first.");
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

        {message ? <Alert type="info">{message}</Alert> : null}

        <Button type="submit">Login</Button>
      </form>
    </Card>
  );
}
