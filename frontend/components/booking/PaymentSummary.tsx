import Card from "../ui/Card";

type Props = {
  doctorFee: number;
  hospitalFee: number;
  eChannellingFee: number;
  discount: number;
  totalFee: number;
  hideHospitalFee?: boolean;
};

function money(value: number) {
  return `LKR ${value.toFixed(2)}`;
}

export default function PaymentSummary({
  doctorFee,
  hospitalFee,
  eChannellingFee,
  discount,
  totalFee,
  hideHospitalFee = false,
}: Props) {
  return (
    <Card title="Payment Details">
      <div className="space-y-2 text-sm text-gray-700">
        <div className="flex items-center justify-between">
          <span>Doctor Fee</span>
          <span>{money(doctorFee)}</span>
        </div>
        {!hideHospitalFee ? (
          <div className="flex items-center justify-between">
            <span>Hospital Fee</span>
            <span>{money(hospitalFee)}</span>
          </div>
        ) : null}
        <div className="flex items-center justify-between">
          <span>eChannelling Fee</span>
          <span>{money(eChannellingFee)}</span>
        </div>
        <div className="flex items-center justify-between">
          <span>Discount</span>
          <span>- {money(discount)}</span>
        </div>
        <div className="mt-3 border-t border-gray-200 pt-3 text-base font-semibold text-gray-900">
          <div className="flex items-center justify-between">
            <span>Total Fee</span>
            <span>{money(totalFee)}</span>
          </div>
        </div>
      </div>
    </Card>
  );
}
