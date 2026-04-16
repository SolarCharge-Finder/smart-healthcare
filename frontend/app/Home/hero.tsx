'use client';

import Link from 'next/link';

const services = [
  { label: 'Auth Service', icon: '🔐' },
  { label: 'Doctor Service', icon: '👨‍⚕️' },
  { label: 'Appointment Service', icon: '📅' },
  { label: 'AI Symptom Service', icon: '🧠' },
  { label: 'Payment Service', icon: '💳' },
  { label: 'Notification Service', icon: '🔔' },
  { label: 'Telemedicine Service', icon: '📹' },
];

export default function Hero() {
  return (
    <section className="relative overflow-hidden bg-gradient-to-br from-blue-700 via-blue-600 to-blue-500">
      {/* decorative blobs */}
      <div className="absolute -top-24 -right-24 w-96 h-96 rounded-full bg-white/5 blur-3xl pointer-events-none" />
      <div className="absolute bottom-0 left-10 w-72 h-72 rounded-full bg-blue-400/20 blur-2xl pointer-events-none" />

      <div className="relative container mx-auto px-6 py-16 md:py-20">
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-12 items-center">
          {/* ── LEFT ── */}
          <div className="space-y-7">
            <div className="inline-flex items-center gap-2 bg-white/15 backdrop-blur-sm border border-white/25 rounded-full px-4 py-1.5 text-xs font-semibold text-white tracking-wide">
              <span className="w-2 h-2 bg-emerald-400 rounded-full animate-pulse" />
              Sri Lanka&apos;s Trusted Digital Health Platform
            </div>

            <h1 className="text-4xl md:text-5xl font-extrabold text-white leading-tight tracking-tight">
              SmartHealth <span className="text-blue-200">Digital Healthcare</span> Platform
            </h1>

            <p className="text-blue-100 text-base md:text-lg leading-relaxed max-w-lg">
              Book appointments, consult doctors online, and get AI‑powered health guidance — all
              from one secure platform.
            </p>

            <div className="flex flex-wrap gap-3 pt-1">
              <Link
                href="/doctors"
                className="inline-flex items-center gap-2 bg-white text-blue-700 font-semibold px-6 py-3 rounded-xl shadow-lg hover:shadow-xl hover:bg-blue-50 transition-all duration-200 text-sm"
              >
                🔍 Find Doctor
              </Link>
              <Link
                href="/appointments/book"
                className="inline-flex items-center gap-2 bg-blue-800/50 backdrop-blur border border-white/25 text-white font-semibold px-6 py-3 rounded-xl hover:bg-blue-800/70 transition-all duration-200 text-sm"
              >
                📅 Book Appointment
              </Link>
              <Link
                href="/ai-checker"
                className="inline-flex items-center gap-2 bg-emerald-500 text-white font-semibold px-6 py-3 rounded-xl hover:bg-emerald-600 shadow hover:shadow-md transition-all duration-200 text-sm"
              >
                🧠 Check Symptoms
              </Link>
            </div>

            <div className="flex gap-8 pt-4 border-t border-white/20">
              {[
                { value: '50K+', label: 'Patients' },
                { value: '2,000+', label: 'Doctors' },
                { value: '98%', label: 'Satisfaction' },
              ].map((s) => (
                <div key={s.label}>
                  <p className="text-white font-bold text-xl">{s.value}</p>
                  <p className="text-blue-200 text-xs mt-0.5">{s.label}</p>
                </div>
              ))}
            </div>
          </div>

          {/* ── RIGHT — Services Card ── */}
          <div className="rounded-2xl shadow-2xl bg-white/10 backdrop-blur-md border border-white/20 p-7">
            <div className="flex items-center justify-between mb-5">
              <p className="text-xs font-bold uppercase tracking-widest text-blue-200">
                Platform Services
              </p>
              <span className="text-xs bg-emerald-500 text-white px-2.5 py-0.5 rounded-full font-semibold">
                All Systems Live
              </span>
            </div>

            <div className="grid grid-cols-1 gap-2.5">
              {services.map((s) => (
                <div
                  key={s.label}
                  className="flex items-center gap-3 bg-white/10 hover:bg-white/20 transition-colors duration-150 rounded-xl px-4 py-2.5"
                >
                  <span className="text-lg leading-none">{s.icon}</span>
                  <span className="text-sm font-medium text-white flex-1">{s.label}</span>
                  <span className="flex items-center gap-1.5 text-emerald-300 text-[11px] font-semibold">
                    <span className="w-1.5 h-1.5 bg-emerald-400 rounded-full" />
                    Online
                  </span>
                </div>
              ))}
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}
