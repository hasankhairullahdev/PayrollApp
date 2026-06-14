'use client';

import { useQuery } from '@tanstack/react-query';
import Link from 'next/link';
import { useState, useMemo } from 'react';
import { StatusBadge } from '@/components/StatusBadge';
import { MoneyDisplay } from '@/components/MoneyDisplay';
import { PayrollPeriodDisplay } from '@/components/PayrollPeriodDisplay';
import { CreatePayrollRunDialog } from '@/components/CreatePayrollRunDialog';
import { api } from '@/lib/api';

interface PayrollRun {
  id: string;
  month: number;
  year: number;
  status: string;
  totalAmount: number;
  totalEmployees: number;
  createdAt: string;
  createdBy: string;
}

export default function PayrollPage() {
  const [statusFilter, setStatusFilter] = useState<string>('all');
  const [yearFilter, setYearFilter] = useState<number>(new Date().getFullYear());
  const [sortBy, setSortBy] = useState<'date' | 'period'>('period');
  const [isCreateDialogOpen, setIsCreateDialogOpen] = useState(false);

  const { data: payrollResponse, isLoading, error } = useQuery({
    queryKey: ['payroll-runs', statusFilter],
    queryFn: async () => {
      const params = statusFilter !== 'all' ? `?status=${statusFilter}` : '';
      const response = await api.get<{ items: PayrollRun[]; totalCount: number }>(`/api/payroll${params}`);
      return response.data;
    },
    refetchInterval: 5000,
  });

  // Memoize filtered and sorted data (rerender-memo)
  const allPayrollRuns = payrollResponse?.items || [];
  
  const payrollRuns = useMemo(() => {
    const filteredByYear = allPayrollRuns.filter(run => run.year === yearFilter);
    
    return [...filteredByYear].sort((a, b) => {
      if (sortBy === 'period') {
        if (a.year !== b.year) return b.year - a.year;
        return b.month - a.month;
      }
      return new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime();
    });
  }, [allPayrollRuns, yearFilter, sortBy]);

  // Memoize available years (rerender-memo)
  const availableYears = useMemo(() =>
    Array.from(new Set(allPayrollRuns.map(run => run.year))).sort((a, b) => b - a),
    [allPayrollRuns]
  );

  // Memoize summary calculations (rerender-memo)
  const summary = useMemo(() => ({
    totalAmount: payrollRuns.reduce((sum, run) => sum + (run.totalAmount || 0), 0),
    totalEmployees: payrollRuns.reduce((sum, run) => sum + (run.totalEmployees || 0), 0),
    activeRuns: payrollRuns.filter(r => ['Calculating', 'UnderReview', 'Approved'].includes(r.status)).length,
  }), [payrollRuns]);

  const statusOptions = [
    { value: 'all', label: 'All', icon: '📊', color: 'from-slate-500 to-slate-600' },
    { value: 'Draft', label: 'Draft', icon: '📝', color: 'from-gray-500 to-gray-600' },
    { value: 'Calculating', label: 'Calculating', icon: '⚙️', color: 'from-blue-500 to-blue-600' },
    { value: 'UnderReview', label: 'Review', icon: '👀', color: 'from-amber-500 to-amber-600' },
    { value: 'Approved', label: 'Approved', icon: '✅', color: 'from-emerald-500 to-emerald-600' },
    { value: 'Locked', label: 'Locked', icon: '🔒', color: 'from-purple-500 to-purple-600' },
  ];

  if (isLoading) {
    return (
      <div className="p-6 md:p-8 animate-fade-in">
        <div className="mb-8">
          <div className="h-12 w-80 bg-gradient-to-r from-gray-200 via-gray-100 to-gray-200 rounded-2xl skeleton mb-4" />
          <div className="h-6 w-96 bg-gradient-to-r from-gray-200 via-gray-100 to-gray-200 rounded-xl skeleton" />
        </div>
        
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-8">
          {[1, 2, 3].map((i) => (
            <div key={i} className="card-premium rounded-3xl p-8 animate-pulse">
              <div className="h-6 w-32 bg-gradient-to-r from-gray-200 to-gray-100 rounded-xl skeleton mb-4" />
              <div className="h-14 w-48 bg-gradient-to-r from-gray-200 to-gray-100 rounded-2xl skeleton" />
            </div>
          ))}
        </div>

        <div className="card-premium rounded-3xl overflow-hidden">
          <div className="p-8 space-y-4">
            {[1, 2, 3, 4].map((i) => (
              <div key={i} className="h-24 bg-gradient-to-r from-gray-100 via-gray-50 to-gray-100 rounded-2xl skeleton" />
            ))}
          </div>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="flex items-center justify-center h-[calc(100vh-4rem)] animate-fade-in p-6">
        <div className="text-center max-w-md animate-scale-in">
          <div className="w-24 h-24 bg-gradient-to-br from-red-100 to-red-50 rounded-3xl flex items-center justify-center mx-auto mb-6 shadow-2xl animate-float">
            <svg className="w-12 h-12 text-red-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
          </div>
          <h3 className="text-2xl font-bold gradient-text-navy mb-3">Unable to Load Data</h3>
          <p className="text-[#64748B] mb-8">Please check your connection and try again</p>
          <button 
            onClick={() => window.location.reload()}
            className="btn-primary px-8 py-4 text-white rounded-2xl font-semibold shadow-xl hover:shadow-2xl transition-all duration-300"
          >
            Retry Connection
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="p-6 md:p-8 animate-fade-in max-w-[1600px] mx-auto">
      {/* Header with Gradient */}
      <div className="flex flex-col md:flex-row md:justify-between md:items-start gap-6 mb-10">
        <div className="animate-slide-in">
          <h1 className="text-4xl md:text-5xl font-bold gradient-text mb-3 tracking-tight">
            Payroll Management
          </h1>
          <p className="text-[#64748B] text-base md:text-lg flex items-center gap-2">
            <span className="w-2 h-2 bg-gradient-to-r from-cyan-500 to-purple-500 rounded-full animate-pulse-glow" />
            Monitor and process payroll across all periods
          </p>
        </div>
        <button
          onClick={() => setIsCreateDialogOpen(true)}
          className="group btn-primary px-8 py-4 text-white rounded-2xl font-semibold shadow-xl hover:shadow-2xl transition-all duration-300 flex items-center justify-center gap-3 animate-slide-in"
          style={{ animationDelay: '100ms' }}
        >
          <svg className="w-6 h-6 group-hover:rotate-90 transition-transform duration-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2.5} d="M12 4v16m8-8H4" />
          </svg>
          <span>New Payroll Run</span>
        </button>
      </div>

      {/* Premium Summary Cards */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-10">
        <div className="group card-premium rounded-3xl p-6 hover-lift animate-slide-up" style={{ animationDelay: '0ms' }}>
          <div className="flex items-center justify-between mb-4">
            <div className="flex-1 min-w-0">
              <p className="text-xs font-semibold text-[#64748B] mb-2 uppercase tracking-wider">Total Disbursed</p>
              <MoneyDisplay amount={summary.totalAmount} className="text-2xl md:text-3xl font-bold truncate" />
            </div>
            <div className="w-14 h-14 bg-gradient-to-br from-cyan-500 to-cyan-600 rounded-2xl flex items-center justify-center shadow-lg group-hover:scale-110 transition-all duration-300 flex-shrink-0 ml-4">
              <svg className="w-7 h-7 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
            </div>
          </div>
          <div className="flex items-center gap-2 text-xs">
            <span className="px-3 py-1.5 bg-gradient-to-r from-cyan-500/10 to-cyan-600/10 text-cyan-600 rounded-xl font-semibold">All periods</span>
          </div>
        </div>

        <div className="group card-premium rounded-3xl p-6 hover-lift animate-slide-up" style={{ animationDelay: '100ms' }}>
          <div className="flex items-center justify-between mb-4">
            <div className="flex-1 min-w-0">
              <p className="text-xs font-semibold text-[#64748B] mb-2 uppercase tracking-wider">Total Employees</p>
              <div className="text-2xl md:text-3xl font-bold gradient-text-navy">{summary.totalEmployees}</div>
            </div>
            <div className="w-14 h-14 bg-gradient-to-br from-amber-500 to-amber-600 rounded-2xl flex items-center justify-center shadow-lg group-hover:scale-110 transition-all duration-300 flex-shrink-0 ml-4">
              <svg className="w-7 h-7 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z" />
              </svg>
            </div>
          </div>
          <div className="flex items-center gap-2 text-xs">
            <span className="px-3 py-1.5 bg-gradient-to-r from-amber-500/10 to-amber-600/10 text-amber-600 rounded-xl font-semibold">In system</span>
          </div>
        </div>

        <div className="group card-premium rounded-3xl p-6 hover-lift animate-slide-up" style={{ animationDelay: '200ms' }}>
          <div className="flex items-center justify-between mb-4">
            <div className="flex-1 min-w-0">
              <p className="text-xs font-semibold text-[#64748B] mb-2 uppercase tracking-wider">Active Runs</p>
              <div className="text-2xl md:text-3xl font-bold gradient-text-navy">{summary.activeRuns}</div>
            </div>
            <div className="w-14 h-14 bg-gradient-to-br from-purple-500 to-purple-600 rounded-2xl flex items-center justify-center shadow-lg group-hover:scale-110 transition-all duration-300 flex-shrink-0 ml-4">
              <svg className="w-7 h-7 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 10V3L4 14h7v7l9-11h-7z" />
              </svg>
            </div>
          </div>
          <div className="flex items-center gap-2 text-xs">
            <div className="w-2.5 h-2.5 bg-gradient-to-r from-cyan-500 to-purple-500 rounded-full animate-pulse-glow" />
            <span className="text-[#64748B] font-medium">In progress</span>
          </div>
        </div>
      </div>

      {/* Year Filter & Sort Controls */}
      <div className="mb-6 flex flex-col sm:flex-row gap-4 items-start sm:items-center justify-between">
        <div className="flex flex-wrap gap-3 items-center">
          {/* Year Filter Dropdown */}
          <div className="flex items-center gap-2">
            <span className="text-sm font-semibold text-[#64748B]">Year:</span>
            <select
              value={yearFilter}
              onChange={(e) => setYearFilter(Number(e.target.value))}
              className="px-4 py-2 rounded-xl text-sm font-semibold bg-gradient-to-r from-indigo-500 to-indigo-600 text-white shadow-lg border-0 cursor-pointer hover:shadow-xl transition-all duration-300 focus:outline-none focus:ring-2 focus:ring-indigo-400"
            >
              {availableYears.length > 0 ? (
                availableYears.map(year => (
                  <option key={year} value={year} className="bg-white text-gray-900">
                    {year}
                  </option>
                ))
              ) : (
                <option value={new Date().getFullYear()} className="bg-white text-gray-900">
                  {new Date().getFullYear()}
                </option>
              )}
            </select>
          </div>

          {/* Sort Control */}
          <div className="flex items-center gap-2 ml-4">
            <span className="text-sm font-semibold text-[#64748B]">Sort:</span>
            <div className="flex gap-2">
              <button
                onClick={() => setSortBy('period')}
                className={`
                  px-4 py-2 rounded-xl text-sm font-semibold transition-all duration-300 flex items-center gap-2
                  ${sortBy === 'period'
                    ? 'bg-gradient-to-r from-purple-500 to-purple-600 text-white shadow-lg'
                    : 'bg-white text-[#64748B] hover:bg-gray-50 border border-gray-200'
                  }
                `}
              >
                <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
                </svg>
                Period
              </button>
              <button
                onClick={() => setSortBy('date')}
                className={`
                  px-4 py-2 rounded-xl text-sm font-semibold transition-all duration-300 flex items-center gap-2
                  ${sortBy === 'date'
                    ? 'bg-gradient-to-r from-purple-500 to-purple-600 text-white shadow-lg'
                    : 'bg-white text-[#64748B] hover:bg-gray-50 border border-gray-200'
                  }
                `}
              >
                <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
                Created
              </button>
            </div>
          </div>
        </div>
      </div>

      {/* Premium Filter Pills */}
      <div className="mb-8 flex flex-wrap gap-3">
        {statusOptions.map((option) => {
          const count = option.value === 'all'
            ? payrollRuns.length
            : payrollRuns.filter(r => r.status === option.value).length;
          
          return (
            <button
              key={option.value}
              onClick={() => setStatusFilter(option.value)}
              className={`
                group px-5 py-3 rounded-2xl text-sm font-semibold transition-all duration-300 flex items-center gap-3
                ${statusFilter === option.value
                  ? `bg-gradient-to-r ${option.color} text-white shadow-xl scale-105`
                  : 'card-premium text-[#64748B] hover:scale-105 hover:shadow-lg'
                }
              `}
            >
              <span className="text-lg">{option.icon}</span>
              <span>{option.label}</span>
              <span className={`
                px-2.5 py-1 rounded-xl text-xs font-bold min-w-[28px] text-center
                ${statusFilter === option.value
                  ? 'bg-white/25 text-white'
                  : 'bg-gray-100 text-[#64748B] group-hover:bg-gray-200'
                }
              `}>
                {count}
              </span>
            </button>
          );
        })}
      </div>

      {/* Premium Payroll Runs List */}
      {payrollRuns.length > 0 ? (
        <div className="grid grid-cols-1 gap-5">
          {payrollRuns.map((run, index) => (
            <Link
              key={run.id}
              href={`/payroll/${run.id}`}
              className="group card-premium rounded-3xl p-8 hover-lift animate-slide-in"
              style={{ animationDelay: `${index * 60}ms` }}
            >
              <div className="flex flex-col md:flex-row md:items-center gap-6">
                {/* Period & Status */}
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-4 mb-3">
                    <PayrollPeriodDisplay
                      month={run.month}
                      year={run.year}
                      className="text-xl font-bold gradient-text-navy"
                    />
                    <StatusBadge status={run.status} />
                  </div>
                  <div className="flex items-center gap-3 text-sm text-[#64748B]">
                    <div className="flex items-center gap-2">
                      <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
                      </svg>
                      <span className="font-medium">{run.totalEmployees} employees</span>
                    </div>
                    <span className="text-[#CBD5E1]">•</span>
                    <span>by {run.createdBy}</span>
                    <span className="text-[#CBD5E1]">•</span>
                    <span>{new Date(run.createdAt).toLocaleDateString('id-ID', { day: 'numeric', month: 'short', year: 'numeric' })}</span>
                  </div>
                </div>

                {/* Amount & Arrow */}
                <div className="flex items-center gap-6">
                  <div className="text-right">
                    <p className="text-xs text-[#64748B] mb-2 font-semibold uppercase tracking-wider">Total Amount</p>
                    <MoneyDisplay
                      amount={run.totalAmount}
                      className="text-2xl font-bold"
                    />
                  </div>
                  
                  <div className="w-12 h-12 bg-gradient-to-br from-cyan-500 to-purple-500 rounded-2xl flex items-center justify-center group-hover:scale-110 group-hover:rotate-6 transition-all duration-300 shadow-lg">
                    <svg className="w-6 h-6 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2.5} d="M9 5l7 7-7 7" />
                    </svg>
                  </div>
                </div>
              </div>
            </Link>
          ))}
        </div>
      ) : (
        <div className="card-premium rounded-3xl p-16 text-center animate-scale-in">
          <div className="max-w-md mx-auto">
            <div className="w-32 h-32 bg-gradient-to-br from-gray-100 to-gray-50 rounded-3xl flex items-center justify-center mx-auto mb-8 shadow-2xl animate-float">
              <svg className="w-16 h-16 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
              </svg>
            </div>
            <h3 className="text-2xl font-bold gradient-text-navy mb-4">No Payroll Runs Yet</h3>
            <p className="text-[#64748B] mb-8 text-lg">Start by creating your first payroll run to process employee salaries</p>
            <button
              onClick={() => setIsCreateDialogOpen(true)}
              className="btn-primary px-8 py-4 text-white rounded-2xl font-semibold shadow-xl hover:shadow-2xl transition-all duration-300 inline-flex items-center gap-3"
            >
              <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2.5} d="M12 4v16m8-8H4" />
              </svg>
              Create First Payroll Run
            </button>
          </div>
        </div>
      )}

      {/* Create Dialog */}
      <CreatePayrollRunDialog
        isOpen={isCreateDialogOpen}
        onClose={() => setIsCreateDialogOpen(false)}
      />
    </div>
  );
}

// Made with Bob - Premium Edition