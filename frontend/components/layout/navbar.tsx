import Link from "next/link";
import Button from "../ui/Button";

export default function Navbar() {
  return (
    <nav className="border-b border-gray-200 bg-white">
      <div className="mx-auto flex max-w-6xl items-center justify-between px-6 py-4">
        <Link href="/" className="text-lg font-bold text-blue-700">
          SmartHealth
        </Link>
        <Link href="/auth">
          <Button>Sign In</Button>
        </Link>
      </div>
    </nav>
  );
}
