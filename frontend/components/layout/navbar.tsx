'use client';

import Link from 'next/link';
import Button from '../ui/Button';
import NotificationBell from '../notifications/NotificationBell';
import { useAuthContext } from '../../modules/auth/AuthContext';

export default function Navbar() {
  const { user } = useAuthContext();

  return (
    <nav className="border-b border-gray-200 bg-white">
      <div className="mx-auto flex max-w-6xl items-center justify-between px-6 py-4">
        <Link href="/" className="text-lg font-bold text-blue-700">
          SmartHealth
        </Link>

        {user ? (
          <div className="flex items-center gap-3">
            <NotificationBell />

            <Link
              href="/user"
              className="flex items-center gap-2 rounded-lg px-2 py-1 text-sm font-medium text-gray-700 hover:bg-gray-100"
            >
              <span className="inline-flex h-8 w-8 items-center justify-center rounded-full bg-blue-100 text-blue-700">
                U
              </span>
              <span>{user.name}</span>
            </Link>
          </div>
        ) : (
          <Link href="/auth">
            <Button>Sign In</Button>
          </Link>
        )}
      </div>
    </nav>
  );
}
