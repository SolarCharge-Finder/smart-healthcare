import Hero from "./Home/hero";
import QuickActions from "./Home/QuickActions";
import Features from "./Home/Features";
import HomeSearchSection from "./Home/HomeSearchSection";

export default function HomePage() {
  return (
    <main className="min-h-screen bg-gray-50">
      {/* 1. Hero — full-width gradient banner with CTA + services card */}
      <Hero />

      <HomeSearchSection />

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