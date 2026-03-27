"use client";

import { FormEvent, useState } from "react";
import Card from "../ui/Card";
import Input from "../ui/Input";
import Button from "../ui/Button";
import Alert from "../ui/Alert";

export default function PaymentForm() {
  const [cardNumber, setCardNumber] = useState("");
  const [expiry, setExpiry] = useState("");
  const [cvc, setCvc] = useState("");
  const [status, setStatus] = useState<string | null>(null);

  const onSubmit = (event: FormEvent) => {
    event.preventDefault();
    setStatus("Payment captured (mock). Stripe integration pending.");
  };

  return (
    <Card title="Payment">
      <form className="space-y-4" onSubmit={onSubmit}>
        <Input
          label="Card Number"
          placeholder="4242 4242 4242 4242"
          value={cardNumber}
          onChange={(e) => setCardNumber(e.target.value)}
          required
        />
        <div className="grid gap-4 md:grid-cols-2">
          <Input
            label="Expiry"
            placeholder="MM/YY"
            value={expiry}
            onChange={(e) => setExpiry(e.target.value)}
            required
          />
          <Input
            label="CVC"
            placeholder="123"
            value={cvc}
            onChange={(e) => setCvc(e.target.value)}
            required
          />
        </div>

        {status ? <Alert type="success">{status}</Alert> : null}

        <Button type="submit">Pay Now</Button>
      </form>
    </Card>
  );
}
