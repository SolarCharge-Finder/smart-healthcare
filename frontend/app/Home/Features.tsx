const features = [
  {
    emoji: '🧠',
    title: 'AI-Powered Symptom Checker',
    description:
      'Describe your symptoms and our AI engine maps them to possible conditions, recommending the right specialist for you.',
    badge: 'AI',
    badgeCls: 'bg-purple-100 text-purple-700',
    cardBorder: 'border-purple-100',
    iconBg: 'bg-purple-50',
  },
  {
    emoji: '🔒',
    title: 'Secure Appointment Booking',
    description:
      'End-to-end encrypted scheduling with instant SMS/email confirmation, automated reminders, and easy rescheduling.',
    badge: 'Secure',
    badgeCls: 'bg-emerald-100 text-emerald-700',
    cardBorder: 'border-emerald-100',
    iconBg: 'bg-emerald-50',
  },
  {
    emoji: '📹',
    title: 'Video Consultation',
    description:
      'Connect face-to-face with licensed doctors via HD, low-latency video calls — no travel needed, ever.',
    badge: 'Live',
    badgeCls: 'bg-blue-100 text-blue-700',
    cardBorder: 'border-blue-100',
    iconBg: 'bg-blue-50',
  },
  {
    emoji: '💳',
    title: 'Online Payments',
    description:
      'Pay consultation fees securely online, download invoices, and manage your full medical billing history.',
    badge: 'Instant',
    badgeCls: 'bg-amber-100 text-amber-700',
    cardBorder: 'border-amber-100',
    iconBg: 'bg-amber-50',
  },
];

export default function Features() {
  return (
    <section className="bg-white py-16 border-t border-gray-100">
      <div className="container mx-auto px-6">
        {/* Header */}
        <div className="text-center mb-12">
          <span className="inline-block bg-blue-50 text-blue-600 text-xs font-bold uppercase tracking-widest px-4 py-1.5 rounded-full mb-4">
            Why SmartHealth
          </span>
          <h2 className="text-2xl md:text-3xl font-bold text-gray-800 mb-3">
            Everything you need for modern healthcare
          </h2>
          <p className="text-gray-500 max-w-xl mx-auto text-sm leading-relaxed">
            Built for patients and providers alike — secure, intelligent, and accessible on any
            device.
          </p>
        </div>

        {/* Feature cards */}
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-5">
          {features.map((f) => (
            <div
              key={f.title}
              className={`relative rounded-2xl border ${f.cardBorder} bg-gray-50 p-6 hover:shadow-md transition-shadow duration-200`}
            >
              {/* Badge */}
              <span
                className={`absolute top-4 right-4 text-[10px] font-bold uppercase tracking-wider px-2 py-0.5 rounded-full ${f.badgeCls}`}
              >
                {f.badge}
              </span>

              {/* Icon */}
              <div
                className={`w-12 h-12 rounded-xl flex items-center justify-center text-2xl mb-4 ${f.iconBg}`}
              >
                {f.emoji}
              </div>

              <h3 className="font-semibold text-gray-800 text-sm mb-2 pr-12">{f.title}</h3>
              <p className="text-gray-500 text-xs leading-relaxed">{f.description}</p>
            </div>
          ))}
        </div>

        {/* CTA strip — same dark-blue banner style as eChannelling footer area */}
        <div className="mt-14 rounded-2xl bg-gradient-to-r from-blue-700 to-blue-600 text-white px-8 py-8 flex flex-col md:flex-row items-center justify-between gap-6">
          <div>
            <h3 className="font-bold text-lg">Ready to take control of your health?</h3>
            <p className="text-blue-200 text-sm mt-1">
              Join thousands of patients already using SmartHealth.
            </p>
          </div>

          {/* Mini stats */}
          <div className="flex gap-10 text-center shrink-0">
            {[
              { value: '50K+', label: 'Patients' },
              { value: '2,000+', label: 'Doctors' },
              { value: '24/7', label: 'Support' },
            ].map((s) => (
              <div key={s.label}>
                <p className="text-white font-extrabold text-2xl">{s.value}</p>
                <p className="text-blue-200 text-xs mt-0.5">{s.label}</p>
              </div>
            ))}
          </div>
        </div>
      </div>
    </section>
  );
}
