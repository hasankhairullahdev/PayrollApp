"use client";

import { useMemo } from "react";
import { PieChart, Pie, Cell, ResponsiveContainer, Legend, Tooltip } from "recharts";

interface PayrollSummaryChartsProps {
  totals: {
    basicSalary: number;
    allowances: number;
    overtime: number;
    bpjs: number;
    pph21: number;
    gross: number;
    deductions: number;
    net: number;
  };
}

const INCOME_COLORS = {
  basicSalary: "#14b8a6", // teal-500
  allowances: "#06b6d4", // cyan-500
  overtime: "#8b5cf6", // violet-500
};

const DEDUCTION_COLORS = {
  bpjs: "#f59e0b", // amber-500
  pph21: "#ef4444", // red-500
};

export function PayrollSummaryCharts({ totals }: PayrollSummaryChartsProps) {
  // Memoize income data (rerender-memo)
  const incomeData = useMemo(() => {
    const data = [];
    if (totals.basicSalary > 0) {
      data.push({
        name: "Basic Salary",
        value: totals.basicSalary,
        color: INCOME_COLORS.basicSalary,
      });
    }
    if (totals.allowances > 0) {
      data.push({
        name: "Allowances",
        value: totals.allowances,
        color: INCOME_COLORS.allowances,
      });
    }
    if (totals.overtime > 0) {
      data.push({
        name: "Overtime",
        value: totals.overtime,
        color: INCOME_COLORS.overtime,
      });
    }
    return data;
  }, [totals.basicSalary, totals.allowances, totals.overtime]);

  // Memoize deduction data (rerender-memo)
  const deductionData = useMemo(() => {
    const data = [];
    if (totals.bpjs > 0) {
      data.push({
        name: "BPJS",
        value: totals.bpjs,
        color: DEDUCTION_COLORS.bpjs,
      });
    }
    if (totals.pph21 > 0) {
      data.push({
        name: "PPh 21",
        value: totals.pph21,
        color: DEDUCTION_COLORS.pph21,
      });
    }
    return data;
  }, [totals.bpjs, totals.pph21]);

  // Memoize net breakdown (rerender-memo)
  const netBreakdownData = useMemo(() => [
    {
      name: "Net Salary",
      value: totals.net,
      color: "#10b981", // emerald-500
    },
    {
      name: "Deductions",
      value: totals.deductions,
      color: "#ef4444", // red-500
    },
  ], [totals.net, totals.deductions]);

  const formatCurrency = (value: number) => {
    return `Rp ${value.toLocaleString("id-ID")}`;
  };

  const formatPercentage = (value: number, total: number) => {
    const percentage = ((value / total) * 100).toFixed(1);
    return `${percentage}%`;
  };

  return (
    <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
      {/* Income Breakdown */}
      <div className="card-premium rounded-3xl p-6">
        <h3 className="text-lg font-semibold text-[#1E3A5F] mb-4 flex items-center gap-2">
          <div className="w-8 h-8 bg-gradient-to-br from-teal-500 to-teal-600 rounded-lg flex items-center justify-center">
            <svg className="w-4 h-4 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
            </svg>
          </div>
          Income Breakdown
        </h3>
        
        <div className="h-64">
          <ResponsiveContainer width="100%" height="100%">
            <PieChart>
              <Pie
                data={incomeData}
                cx="50%"
                cy="50%"
                labelLine={false}
                label={({ name, percent }) => `${name} ${((percent || 0) * 100).toFixed(0)}%`}
                outerRadius={80}
                fill="#8884d8"
                dataKey="value"
              >
                {incomeData.map((entry, index) => (
                  <Cell key={`cell-${index}`} fill={entry.color} />
                ))}
              </Pie>
              <Tooltip
                formatter={(value) => formatCurrency(Number(value))}
                contentStyle={{
                  backgroundColor: "white",
                  border: "2px solid #e5e7eb",
                  borderRadius: "12px",
                  padding: "8px 12px",
                }}
              />
            </PieChart>
          </ResponsiveContainer>
        </div>

        <div className="mt-4 space-y-2">
          {incomeData.map((item) => (
            <div key={item.name} className="flex items-center justify-between text-sm">
              <div className="flex items-center gap-2">
                <div
                  className="w-3 h-3 rounded-full"
                  style={{ backgroundColor: item.color }}
                />
                <span className="text-[#64748B]">{item.name}</span>
              </div>
              <div className="text-right">
                <div className="font-semibold text-[#1E3A5F]">
                  {formatCurrency(item.value)}
                </div>
                <div className="text-xs text-[#94A3B8]">
                  {formatPercentage(item.value, totals.gross)}
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>

      {/* Deduction Breakdown */}
      <div className="card-premium rounded-3xl p-6">
        <h3 className="text-lg font-semibold text-[#1E3A5F] mb-4 flex items-center gap-2">
          <div className="w-8 h-8 bg-gradient-to-br from-red-500 to-red-600 rounded-lg flex items-center justify-center">
            <svg className="w-4 h-4 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M20 12H4" />
            </svg>
          </div>
          Deduction Breakdown
        </h3>
        
        <div className="h-64">
          <ResponsiveContainer width="100%" height="100%">
            <PieChart>
              <Pie
                data={deductionData}
                cx="50%"
                cy="50%"
                labelLine={false}
                label={({ name, percent }) => `${name} ${((percent || 0) * 100).toFixed(0)}%`}
                outerRadius={80}
                fill="#8884d8"
                dataKey="value"
              >
                {deductionData.map((entry, index) => (
                  <Cell key={`cell-${index}`} fill={entry.color} />
                ))}
              </Pie>
              <Tooltip
                formatter={(value) => formatCurrency(Number(value))}
                contentStyle={{
                  backgroundColor: "white",
                  border: "2px solid #e5e7eb",
                  borderRadius: "12px",
                  padding: "8px 12px",
                }}
              />
            </PieChart>
          </ResponsiveContainer>
        </div>

        <div className="mt-4 space-y-2">
          {deductionData.map((item) => (
            <div key={item.name} className="flex items-center justify-between text-sm">
              <div className="flex items-center gap-2">
                <div
                  className="w-3 h-3 rounded-full"
                  style={{ backgroundColor: item.color }}
                />
                <span className="text-[#64748B]">{item.name}</span>
              </div>
              <div className="text-right">
                <div className="font-semibold text-[#1E3A5F]">
                  {formatCurrency(item.value)}
                </div>
                <div className="text-xs text-[#94A3B8]">
                  {formatPercentage(item.value, totals.deductions)}
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>

      {/* Net vs Deductions */}
      <div className="card-premium rounded-3xl p-6">
        <h3 className="text-lg font-semibold text-[#1E3A5F] mb-4 flex items-center gap-2">
          <div className="w-8 h-8 bg-gradient-to-br from-emerald-500 to-emerald-600 rounded-lg flex items-center justify-center">
            <svg className="w-4 h-4 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
            </svg>
          </div>
          Net vs Deductions
        </h3>
        
        <div className="h-64">
          <ResponsiveContainer width="100%" height="100%">
            <PieChart>
              <Pie
                data={netBreakdownData}
                cx="50%"
                cy="50%"
                labelLine={false}
                label={({ name, percent }) => `${name} ${((percent || 0) * 100).toFixed(0)}%`}
                outerRadius={80}
                fill="#8884d8"
                dataKey="value"
              >
                {netBreakdownData.map((entry, index) => (
                  <Cell key={`cell-${index}`} fill={entry.color} />
                ))}
              </Pie>
              <Tooltip
                formatter={(value) => formatCurrency(Number(value))}
                contentStyle={{
                  backgroundColor: "white",
                  border: "2px solid #e5e7eb",
                  borderRadius: "12px",
                  padding: "8px 12px",
                }}
              />
            </PieChart>
          </ResponsiveContainer>
        </div>

        <div className="mt-4 space-y-2">
          {netBreakdownData.map((item) => (
            <div key={item.name} className="flex items-center justify-between text-sm">
              <div className="flex items-center gap-2">
                <div
                  className="w-3 h-3 rounded-full"
                  style={{ backgroundColor: item.color }}
                />
                <span className="text-[#64748B]">{item.name}</span>
              </div>
              <div className="text-right">
                <div className="font-semibold text-[#1E3A5F]">
                  {formatCurrency(item.value)}
                </div>
                <div className="text-xs text-[#94A3B8]">
                  {formatPercentage(item.value, totals.gross)}
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}

// Made with Bob
