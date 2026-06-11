interface MoneyDisplayProps {
  amount: number;
  className?: string;
  showSign?: boolean;
}

export function MoneyDisplay({ amount, className = '', showSign = false }: MoneyDisplayProps) {
  const formatted = new Intl.NumberFormat('id-ID', {
    style: 'currency',
    currency: 'IDR',
    minimumFractionDigits: 0,
    maximumFractionDigits: 0,
  }).format(amount);

  const isNegative = amount < 0;
  const displayValue = showSign && !isNegative ? `+${formatted}` : formatted;

  return (
    <span 
      className={`money-display inline-flex items-center ${className}`}
      style={{
        fontVariantNumeric: 'tabular-nums',
      }}
    >
      {displayValue}
    </span>
  );
}

// Made with Bob