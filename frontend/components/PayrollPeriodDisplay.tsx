interface PayrollPeriodDisplayProps {
  month: number;
  year: number;
  className?: string;
}

const MONTH_NAMES = [
  'Januari', 'Februari', 'Maret', 'April', 'Mei', 'Juni',
  'Juli', 'Agustus', 'September', 'Oktober', 'November', 'Desember'
];

export function PayrollPeriodDisplay({ month, year, className = '' }: PayrollPeriodDisplayProps) {
  const monthName = MONTH_NAMES[month - 1] || 'Unknown';
  
  return (
    <span className={className}>
      {monthName} {year}
    </span>
  );
}

// Made with Bob