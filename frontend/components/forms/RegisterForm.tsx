"use client";

import { FormEvent, useState } from "react";
import Card from "../ui/Card";
import Input from "../ui/Input";
import Button from "../ui/Button";
import Alert from "../ui/Alert";

export default function RegisterForm() {
  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [success, setSuccess] = useState(false);

  const onSubmit = (event: FormEvent) => {
    event.preventDefault();
    const user = { name, email };
    localStorage.setItem("smarthealth:user", JSON.stringify(user));
    setSuccess(true);
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

        {success ? (
          <Alert type="success">Mock account created successfully.</Alert>
        ) : null}

        <Button type="submit">Register</Button>
      </form>
    </Card>
  );
}
