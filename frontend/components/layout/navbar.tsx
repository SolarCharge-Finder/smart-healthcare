"use client";

import Link from "next/link";
import Button from "../ui/Button";
import { useAuthContext } from "../../modules/auth/AuthContext";

export default function Navbar() {
  const { user, logout } = useAuthContext();

  return (
    <nav className="border-b border-gray-200 bg-white">
      <div className="mx-auto flex max-w-6xl items-center justify-between px-6 py-4">
        
        <Link href="/" className="text-lg font-bold text-blue-700">
          SmartHealth
        </Link>

        {user ? (
          <div className="flex items-center gap-4">
            <span className="text-sm font-medium text-gray-700">
              {user.name}
            </span>

            <Button onClick={logout}>
              Logout
            </Button>
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