import Hero from "./Home/hero";
import QuickActions from "./Home/QuickActions";
import Features from "./Home/Features";

export default function HomePage() {
  return (
    <main className="min-h-screen bg-gray-50">
      {/* 1. Hero — full-width gradient banner with CTA + services card */}
      <Hero />

      {/* 2. Doctor search bar — eChannelling-style (static UI, hooks wired externally) */}
      <section className="bg-blue-700 pb-8">
        <div className="container mx-auto px-6">
          <div className="bg-blue-800/60 backdrop-blur rounded-2xl border border-blue-500/40 px-6 py-5 shadow-xl">
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-5 gap-3 items-end">

              <div className="lg:col-span-1 flex flex-col gap-1">
                <label className="text-blue-200 text-xs font-semibold">Doctor Name</label>
                <input
                  type="text"
                  placeholder="Search Doctor Name"
                  className="w-full rounded-lg border border-blue-400/30 bg-white/10 text-white placeholder-blue-300 text-sm px-3 py-2.5 focus:outline-none focus:ring-2 focus:ring-white/30"
                />
              </div>

              <div className="flex flex-col gap-1">
                <label className="text-blue-200 text-xs font-semibold">Specialization</label>
                <select className="w-full rounded-lg border border-blue-400/30 bg-white/10 text-blue-200 text-sm px-3 py-2.5 focus:outline-none focus:ring-2 focus:ring-white/30">
                  <option value="">Select Specialization</option>
                  <option>Cardiologist</option>
                  <option>Dermatologist</option>
                  <option>Neurologist</option>
                  <option>Pediatrician</option>
                  <option>General Practitioner</option>
                </select>
              </div>

              <div className="flex flex-col gap-1">
                <label className="text-blue-200 text-xs font-semibold">Hospital</label>
                <select className="w-full rounded-lg border border-blue-400/30 bg-white/10 text-blue-200 text-sm px-3 py-2.5 focus:outline-none focus:ring-2 focus:ring-white/30">
                  <option value="">Select Hospital</option>
                  <option>National Hospital</option>
                  <option>Asiri Hospital</option>
                  <option>Lanka Hospital</option>
                  <option>Nawaloka Hospital</option>
                </select>
              </div>

              <div className="flex flex-col gap-1">
                <label className="text-blue-200 text-xs font-semibold">Date</label>
                <input
                  type="date"
                  className="w-full rounded-lg border border-blue-400/30 bg-white/10 text-blue-200 text-sm px-3 py-2.5 focus:outline-none focus:ring-2 focus:ring-white/30"
                />
              </div>

              <div className="flex items-end">
                <button className="w-full bg-blue-500 hover:bg-blue-400 text-white font-semibold text-sm px-6 py-2.5 rounded-lg transition-colors duration-150 shadow">
                  Search
                </button>
              </div>

            </div>

            {/* Advance search toggle */}
            <div className="flex items-center gap-2 mt-4 text-blue-200 text-xs cursor-pointer hover:text-white transition-colors w-fit mx-auto">
              <span>Advance search</span>
              <svg className="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
              </svg>
            </div>
          </div>
        </div>
      </section>

      {/* 3. Quick action cards — eChannelling grid style */}
      <QuickActions />

      {/* 4. Feature highlights — SaaS medical landing style */}
      <Features />

      {/* 5. Footer */}
      <footer className="bg-blue-800 text-white">
        <div className="container mx-auto px-6 py-12">
          <div className="grid grid-cols-1 md:grid-cols-4 gap-10">

            {/* Brand */}
            <div className="md:col-span-2">
              <div className="flex items-center gap-2 mb-4">
                <div className="w-8 h-8 bg-emerald-500 rounded-lg flex items-center justify-center text-sm font-extrabold">S</div>
                <span className="font-extrabold text-lg tracking-tight">SmartHealth</span>
              </div>
              <p className="text-blue-200 text-sm leading-relaxed max-w-xs">
                Sri Lanka&apos;s leading digital healthcare platform connecting
                patients with doctors, anytime, anywhere.
              </p>
              <div className="mt-4 space-y-1 text-blue-200 text-xs">
                <p>📞 +94 71 0 225 225</p>
                <p>✉️ info@smarthealth.lk</p>
                <p>🕐 Hotline: 7:00 AM – 9:00 PM</p>
              </div>
            </div>

            {/* Other links */}
            <div>
              <h4 className="font-semibold text-sm mb-3 text-blue-100">Other</h4>
              <ul className="space-y-2 text-blue-300 text-xs">
                {["Terms and Conditions", "FAQ", "Feedback", "Privacy Policy"].map((l) => (
                  <li key={l}>
                    <a href="#" className="hover:text-white transition-colors">{l}</a>
                  </li>
                ))}
              </ul>
            </div>

            {/* About links */}
            <div>
              <h4 className="font-semibold text-sm mb-3 text-blue-100">About</h4>
              <ul className="space-y-2 text-blue-300 text-xs">
                {["The Company", "Investor Relations", "Partners", "Awards", "Careers"].map((l) => (
                  <li key={l}>
                    <a href="#" className="hover:text-white transition-colors">{l}</a>
                  </li>
                ))}
              </ul>
            </div>

          </div>

          <div className="mt-10 pt-6 border-t border-blue-700 flex flex-col sm:flex-row items-center justify-between gap-3 text-blue-400 text-xs">
            <p>© 2025 SmartHealth. All Rights Reserved.</p>
            <div className="flex gap-4">
              {["Facebook", "Instagram", "YouTube", "LinkedIn"].map((s) => (
                <a key={s} href="#" className="hover:text-white transition-colors">{s}</a>
              ))}
            </div>
          </div>
        </div>
      </footer>
    </main>
  );
}