import { ReactNode } from 'react';

type Props = {
  title?: string;
  children: ReactNode;
};

export default function Card({ title, children }: Props) {
  return (
    <div className="rounded-2xl border border-gray-200 bg-white p-6 shadow-sm">
      {title ? <h2 className="mb-4 text-lg font-semibold text-gray-900">{title}</h2> : null}
      {children}
    </div>
  );
}
