'use client';

import { FormEvent, useState } from 'react';
import Input from '../ui/Input';
import Button from '../ui/Button';
import Alert from '../ui/Alert';

export type GuestFormValue = {
  fullName: string;
  email: string;
  phoneNumber: string;
  area: string;
  nicOrPassport: string;
};

type Props = {
  onSubmit: (value: GuestFormValue) => void;
};

const initialState: GuestFormValue = {
  fullName: '',
  email: '',
  phoneNumber: '',
  area: '',
  nicOrPassport: '',
};

export default function GuestForm({ onSubmit }: Props) {
  const [value, setValue] = useState<GuestFormValue>(initialState);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault();
    setError(null);

    if (!value.fullName.trim()) return setError('Full name is required.');
    if (!value.email.trim()) return setError('Email is required.');
    if (!value.phoneNumber.trim()) return setError('Phone number is required.');
    if (!value.area.trim()) return setError('Area is required.');
    if (!value.nicOrPassport.trim()) return setError('NIC or passport number is required.');

    onSubmit(value);
  };

  return (
    <form className="space-y-4" onSubmit={handleSubmit}>
      <Input
        label="Full Name"
        value={value.fullName}
        onChange={(e) => setValue((prev) => ({ ...prev, fullName: e.target.value }))}
        required
      />
      <Input
        label="Email"
        type="email"
        value={value.email}
        onChange={(e) => setValue((prev) => ({ ...prev, email: e.target.value }))}
        required
      />
      <Input
        label="Phone Number"
        value={value.phoneNumber}
        onChange={(e) => setValue((prev) => ({ ...prev, phoneNumber: e.target.value }))}
        required
      />
      <Input
        label="Area"
        value={value.area}
        onChange={(e) => setValue((prev) => ({ ...prev, area: e.target.value }))}
        required
      />
      <Input
        label="NIC or Passport Number"
        value={value.nicOrPassport}
        onChange={(e) => setValue((prev) => ({ ...prev, nicOrPassport: e.target.value }))}
        required
      />

      {error ? <Alert type="error">{error}</Alert> : null}

      <Button type="submit">Continue to Summary</Button>
    </form>
  );
}
