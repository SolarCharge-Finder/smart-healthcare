import { ReactNode } from 'react';

type Props = {
  type?: 'success' | 'error' | 'info';
  children: ReactNode;
};

export default function Alert({ type = 'info', children }: Props) {
  const styles: Record<string, string> = {
    success: 'border-green-200 bg-green-50 text-green-700',
    error: 'border-red-200 bg-red-50 text-red-700',
    info: 'border-gray-200 bg-gray-50 text-gray-700',
  };

  return (
    <div className={`rounded-md border px-4 py-3 text-sm ${styles[type]}`} role="alert">
      {children}
    </div>
  );
}
