import { Suspense } from 'react';
import VerifyClient from './VerifyClient';

//split into server component for suspense and client component for hooks and api calls

export default function VerifyPage() {
  return (
    <Suspense fallback={<div className="text-center mt-10">Verifying...</div>}>
      <VerifyClient />
    </Suspense>
  );
}
