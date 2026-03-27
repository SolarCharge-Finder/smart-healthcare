import { InputHTMLAttributes } from "react";

type Props = InputHTMLAttributes<HTMLInputElement> & {
  label?: string;
};

export default function Input({ label, className, ...props }: Props) {
  return (
    <label className="block space-y-1">
      {label ? (
        <span className="text-sm font-medium text-gray-700">{label}</span>
      ) : null}
      <input
        className={`w-full rounded-lg border border-gray-300 bg-white p-3 text-sm outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-500 invalid:border-red-300 invalid:ring-1 invalid:ring-red-200 ${
          className ?? ""
        }`}
        {...props}
      />
    </label>
  );
}
