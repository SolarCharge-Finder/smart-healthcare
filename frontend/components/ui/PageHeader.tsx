type Props = {
  title: string;
  subtitle?: string;
};

export default function PageHeader({ title, subtitle }: Props) {
  return (
    <header className="rounded-2xl border border-blue-100 bg-gradient-to-r from-blue-50 via-white to-white px-6 py-5 shadow-sm">
      <span className="inline-flex items-center gap-2 rounded-full bg-white px-3 py-1 text-xs font-semibold text-blue-700 shadow-sm">
        <span className="h-2 w-2 rounded-full bg-emerald-500" />
        SmartHealth
      </span>
      <h1 className="mt-3 text-2xl font-semibold text-gray-900">{title}</h1>
      {subtitle ? <p className="mt-1 text-sm text-gray-500">{subtitle}</p> : null}
    </header>
  );
}
