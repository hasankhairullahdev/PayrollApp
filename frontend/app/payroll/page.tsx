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
  const [searchQuery, setSearchQuery] = useState('');
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
    let filtered = allPayrollRuns.filter(run => run.year === yearFilter);
    
    // Search filter
    if (searchQuery) {
      const query = searchQuery.toLowerCase();
      filtered = filtered.filter(run => 
        run.createdBy.toLowerCase().includes(query) ||
        `${run.month}`.includes(query) ||
        `${run.year}`.includes(query)
      );
    }
    
    // Sort by period (year desc, month desc)
    return [...filtered].sort((a, b) => {
      if (a.year !== b.year) return b.year - a.year;
      return b.month - a.month;
    });
  }, [allPayrollRuns, yearFilter, searchQuery]);

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

        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {[1, 2, 3].map((i) => (
            <div key={i} className="card-premium rounded-3xl p-6 animate-pulse">
              <div className="h-24 bg-gradient-to-r from-gray-100 via-gray-50 to-gray-100 rounded-2xl skeleton" />
            </div>
          ))}
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
      {/* Header */}
      <div className="flex flex-col md:flex-row md:justify-between md:items-start gap-6 mb-10">
        <div className="animate-slide-in">
          <h1 className="text-4xl md:text-5xl font-bold mb-3 tracking-tight bg-gradient-to-r from-teal-600 to-emerald-600 bg-clip-text text-transparent">
            Payroll Management
          </h1>
          <p className="text-[#64748B] text-base md:text-lg flex items-center gap-2">
            <span className="w-2 h-2 bg-gradient-to-r from-teal-500 to-emerald-500 rounded-full animate-pulse-glow" />
            Monitor and process payroll across all periods
          </p>
        </div>
        <button
          onClick={() => setIsCreateDialogOpen(true)}
          className="group px-8 py-4 text-white bg-gradient-to-r from-teal-600 to-emerald-600 rounded-2xl font-semibold shadow-xl hover:shadow-2xl hover:scale-105 transition-all duration-300 flex items-center justify-center gap-3 animate-slide-in"
          style={{ animationDelay: '100ms' }}
        >
          <svg className="w-6 h-6 group-hover:rotate-90 transition-transform duration-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2.5} d="M12 4v16m8-8H4" />
          </svg>
          <span>New Payroll Run</span>
        </button>
      </div>

      {/* Summary Cards */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-10">
        <div className="group card-premium rounded-3xl p-6 hover-lift animate-slide-up" style={{ animationDelay: '0ms' }}>
          <div className="flex items-center justify-between mb-4">
            <div className="flex-1 min-w-0">
              <p className="text-xs font-semibold text-[#64748B] mb-2 uppercase tracking-wider">Total Disbursed</p>
              <MoneyDisplay amount={summary.totalAmount} className="text-2xl md:text-3xl font-bold truncate" />
            </div>
            <div className="w-14 h-14 bg-gradient-to-br from-teal-500 to-teal-600 rounded-2xl flex items-center justify-center shadow-lg group-hover:scale-110 transition-all duration-300 flex-shrink-0 ml-4">
              <svg className="w-7 h-7 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
            </div>
          </div>
          <div className="flex items-center gap-2 text-xs">
            <span className="px-3 py-1.5 bg-gradient-to-r from-teal-500/10 to-teal-600/10 text-teal-600 rounded-xl font-semibold">All periods</span>
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
            <div className="w-14 h-14 bg-gradient-to-br from-emerald-500 to-emerald-600 rounded-2xl flex items-center justify-center shadow-lg group-hover:scale-110 transition-all duration-300 flex-shrink-0 ml-4">
              <svg className="w-7 h-7 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 10V3L4 14h7v7l9-11h-7z" />
              </svg>
            </div>
          </div>
          <div className="flex items-center gap-2 text-xs">
            <div className="w-2.5 h-2.5 bg-gradient-to-r from-teal-500 to-emerald-500 rounded-full animate-pulse-glow" />
            <span className="text-[#64748B] font-medium">In progress</span>
          </div>
        </div>
      </div>

      {/* Search and Filters */}
      <div className="mb-8 space-y-4">
        {/* Search Bar */}
        <div className="relative">
          <input
            type="text"
            placeholder="Search by creator, month, or year..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            className="w-full px-5 py-4 pl-12 bg-white border-2 border-gray-200 rounded-2xl text-[#1E3A5F] placeholder-[#94A3B8] focus:outline-none focus:border-teal-500 focus:ring-4 focus:ring-teal-500/10 transition-all"
          />
          <svg className="absolute left-4 top-1/2 -translate-y-1/2 w-5 h-5 text-[#94A3B8]" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
          </svg>
          {searchQuery && (
            <button
              onClick={() => setSearchQuery('')}
              className="absolute right-4 top-1/2 -translate-y-1/2 text-[#94A3B8] hover:text-[#64748B] transition-colors"
            >
              <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          )}
        </div>

        {/* Filter Tabs */}
        <div className="flex flex-wrap items-center gap-3">
          {/* Year Filter */}
          <div className="flex items-center gap-2">
            <span className="text-sm font-semibold text-[#64748B]">Year:</span>
            <select
              value={yearFilter}
              onChange={(e) => setYearFilter(Number(e.target.value))}
              className="px-4 py-2.5 rounded-xl text-sm font-semibold bg-white border-2 border-gray-200 text-[#1E3A5F] cursor-pointer hover:border-teal-500 focus:outline-none focus:border-teal-500 focus:ring-4 focus:ring-teal-500/10 transition-all"
            >
              {availableYears.length > 0 ? (
                availableYears.map(year => (
                  <option key={year} value={year}>
                    {year}
                  </option>
                ))
              ) : (
                <option value={new Date().getFullYear()}>
                  {new Date().getFullYear()}
                </option>
              )}
            </select>
          </div>

          {/* Status Filter Tabs */}
          <div className="flex-1 flex flex-wrap gap-2">
            {[
              { value: 'all', label: 'All', color: 'from-slate-500 to-slate-600' },
              { value: 'Draft', label: 'Draft', color: 'from-gray-500 to-gray-600' },
              { value: 'Calculating', label: 'Calculating', color: 'from-blue-500 to-blue-600' },
              { value: 'UnderReview', label: 'Review', color: 'from-amber-500 to-amber-600' },
              { value: 'Approved', label: 'Approved', color: 'from-emerald-500 to-emerald-600' },
              { value: 'Locked', label: 'Locked', color: 'from-purple-500 to-purple-600' },
            ].map((status) => {
              const count = status.value === 'all'
                ? payrollRuns.length
                : payrollRuns.filter(r => r.status === status.value).length;
              
              return (
                <button
                  key={status.value}
                  onClick={() => setStatusFilter(status.value)}
                  className={`
                    px-4 py-2.5 rounded-xl text-sm font-semibold transition-all duration-300 flex items-center gap-2
                    ${statusFilter === status.value
                      ? `bg-gradient-to-r ${status.color} text-white shadow-lg scale-105`
                      : 'bg-white text-[#64748B] border-2 border-gray-200 hover:border-teal-500 hover:scale-105'
                    }
                  `}
                >
                  <span>{status.label}</span>
                  <span className={`
                    px-2 py-0.5 rounded-lg text-xs font-bold min-w-[24px] text-center
                    ${statusFilter === status.value
                      ? 'bg-white/25 text-white'
                      : 'bg-gray-100 text-[#64748B]'
                    }
                  `}>
                    {count}
                  </span>
                </button>
              );
            })}
          </div>
        </div>
      </div>

      {/* Payroll Runs Grid */}
      {payrollRuns.length > 0 ? (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {payrollRuns.map((run, index) => (
            <Link
              key={run.id}
              href={`/payroll/${run.id}`}
              className="group card-premium rounded-3xl p-6 hover-lift animate-slide-in"
              style={{ animationDelay: `${index * 50}ms` }}
            >
              {/* Header */}
              <div className="flex items-start justify-between mb-4">
                <div className="flex-1 min-w-0">
                  <PayrollPeriodDisplay
                    month={run.month}
                    year={run.year}
                    className="text-xl font-bold gradient-text-navy mb-2"
                  />
                  <StatusBadge status={run.status} />
                </div>
                <div className="w-10 h-10 bg-gradient-to-br from-teal-500 to-emerald-500 rounded-xl flex items-center justify-center group-hover:scale-110 group-hover:rotate-6 transition-all duration-300 shadow-lg flex-shrink-0 ml-3">
                  <svg className="w-5 h-5 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2.5} d="M9 5l7 7-7 7" />
                  </svg>
                </div>
              </div>

              {/* Stats */}
              <div className="space-y-3 mb-4">
                <div className="flex items-center justify-between py-2 border-b border-gray-100">
                  <span className="text-sm text-[#64748B] flex items-center gap-2">
                    <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z" />
                    </svg>
                    Employees
                  </span>
                  <span className="font-semibold text-[#1E3A5F]">{run.totalEmployees}</span>
                </div>
                <div className="flex items-center justify-between py-2">
                  <span className="text-sm text-[#64748B] flex items-center gap-2">
                    <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                    </svg>
                    Total Amount
                  </span>
                  <MoneyDisplay amount={run.totalAmount} className="font-bold text-[#1E3A5F]" />
                </div>
              </div>

              {/* Footer */}
              <div className="pt-3 border-t border-gray-100 flex items-center justify-between text-xs text-[#94A3B8]">
                <span>by {run.createdBy}</span>
                <span>{new Date(run.createdAt).toLocaleDateString('id-ID', { day: 'numeric', month: 'short' })}</span>
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
            <h3 className="text-2xl font-bold gradient-text-navy mb-4">
              {searchQuery ? 'No Results Found' : 'No Payroll Runs Yet'}
            </h3>
            <p className="text-[#64748B] mb-8 text-lg">
              {searchQuery 
                ? 'Try adjusting your search or filters'
                : 'Start by creating your first payroll run to process employee salaries'
              }
            </p>
            {!searchQuery && (
              <button
                onClick={() => setIsCreateDialogOpen(true)}
                className="px-8 py-4 text-white bg-gradient-to-r from-teal-600 to-emerald-600 rounded-2xl font-semibold shadow-xl hover:shadow-2xl hover:scale-105 transition-all duration-300 inline-flex items-center gap-3"
              >
                <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2.5} d="M12 4v16m8-8H4" />
                </svg>
                Create First Payroll Run
              </button>
            )}
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

// Made with Bob
