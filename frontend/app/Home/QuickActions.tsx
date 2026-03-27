"use client";

import { useRouter } from "next/navigation";

const actions = [
  {
    emoji: "👨‍⚕️",
    iconBg: "bg-blue-50",
    iconColor: "text-blue-600",
    title: "Find Doctors",
    description: "Browse specialists and GPs by specialty, hospital, or location.",
    href: "/doctors",
    tag: null,
  },
  {
    emoji: "📅",
    iconBg: "bg-indigo-50",
    iconColor: "text-indigo-600",
    title: "Book Appointment",
    description: "Schedule an in-clinic or online appointment in seconds.",
    href: "/appointments/book",
    tag: null,
  },
  {
    emoji: "🗂️",
    iconBg: "bg-sky-50",
    iconColor: "text-sky-600",
    title: "My Appointments",
    description: "View, manage, and track your past and upcoming visits.",
    href: "/appointments/history",
    tag: null,
  },
  {
    emoji: "🧠",
    iconBg: "bg-emerald-50",
    iconColor: "text-emerald-600",
    title: "AI Symptom Checker",
    description: "Describe symptoms and get instant AI‑powered health guidance.",
    href: "/ai-checker",
    tag: "AI",
  },
];

export default function QuickActions() {
  const router = useRouter();

  return (
    <section className="bg-gray-50 py-12">
      <div className="container mx-auto px-6">

        {/* Section header */}
        <div className="mb-8">
          <h2 className="text-lg font-bold text-gray-800">Quick Access</h2>
          <div className="mt-1 h-0.5 w-12 bg-blue-600 rounded-full" />
        </div>

        {/* Card grid — matches eChannelling layout */}
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
          {actions.map((action) => (
            <button
              key={action.href}
              onClick={() => router.push(action.href)}
              className="group relative text-left bg-white border border-gray-200 rounded-xl p-5 shadow-sm hover:shadow-lg hover:-translate-y-1 transition-all duration-200 cursor-pointer"
            >
              {/* NEW badge (like eChannelling) */}
              {action.tag && (
                <span className="absolute top-3 left-3 bg-emerald-500 text-white text-[10px] font-bold uppercase px-1.5 py-0.5 rounded">
                  {action.tag}
                </span>
              )}

              {/* Info button top-right */}
              <span className="absolute top-3 right-3 w-5 h-5 flex items-center justify-center rounded-full border border-gray-200 text-gray-400 text-[11px] hover:border-blue-400 hover:text-blue-500 transition-colors">
                i
              </span>

              {/* Icon */}
              <div
                className={`w-14 h-14 rounded-2xl flex items-center justify-center text-3xl mb-4 mt-4 mx-auto ${action.iconBg}`}
              >
                {action.emoji}
              </div>

              {/* Text */}
              <div className="text-center">
                <h3 className="font-semibold text-gray-800 text-sm group-hover:text-blue-600 transition-colors">
                  {action.title}
                </h3>
                <p className="text-gray-400 text-xs mt-1 leading-relaxed">
                  {action.description}
                </p>
              </div>
            </button>
          ))}
        </div>
      </div>
    </section>
  );
}