'use client';

import { useQuery } from '@tanstack/react-query';
import Link from 'next/link';
import { useState } from 'react';
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
  totalEmployees: number;  // Backend uses totalEmployees, not employeeCount
  createdAt: string;
  createdBy: string;
}

export default function PayrollPage() {
  const [statusFilter, setStatusFilter] = useState<string>('all');
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

  const payrollRuns = payrollResponse?.items || [];

  const statusOptions = [
    { value: 'all', label: 'All', icon: '📊' },
    { value: 'Draft', label: 'Draft', icon: '📝' },
    { value: 'Calculating', label: 'Calculating', icon: '⚙️' },
    { value: 'UnderReview', label: 'Review', icon: '👀' },
    { value: 'Approved', label: 'Approved', icon: '✅' },
    { value: 'Locked', label: 'Locked', icon: '🔒' },
  ];

  if (isLoading) {
    return (
      <div className="p-6 md:p-8 animate-fade-in">
        <div className="mb-8">
          <div className="h-10 w-64 bg-gradient-to-r from-gray-200 to-gray-100 rounded-xl skeleton mb-3" />
          <div className="h-5 w-96 bg-gradient-to-r from-gray-200 to-gray-100 rounded-lg skeleton" />
        </div>
        
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4 md:gap-6 mb-8">
          {[1, 2, 3].map((i) => (
            <div key={i} className="bg-white/60 backdrop-blur rounded-2xl p-6 shadow-lg border border-gray-100">
              <div className="h-5 w-32 bg-gradient-to-r from-gray-200 to-gray-100 rounded skeleton mb-4" />
              <div className="h-12 w-40 bg-gradient-to-r from-gray-200 to-gray-100 rounded-lg skeleton" />
            </div>
          ))}
        </div>

        <div className="bg-white/60 backdrop-blur rounded-2xl shadow-lg border border-gray-100 overflow-hidden">
          <div className="p-6 space-y-4">
            {[1, 2, 3, 4].map((i) => (
              <div key={i} className="h-20 bg-gradient-to-r from-gray-100 to-gray-50 rounded-xl skeleton" />
            ))}
          </div>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="flex items-center justify-center h-[calc(100vh-4rem)] animate-fade-in p-6">
        <div className="text-center max-w-md">
          <div className="w-20 h-20 bg-gradient-to-br from-red-100 to-red-50 rounded-3xl flex items-center justify-center mx-auto mb-6 shadow-lg">
            <svg className="w-10 h-10 text-red-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
          </div>
          <h3 className="text-xl font-bold text-[#1E3A5F] mb-3">Unable to Load Data</h3>
          <p className="text-[#64748B] mb-6">Please check your connection and try again</p>
          <button 
            onClick={() => window.location.reload()}
            className="px-6 py-3 bg-gradient-to-r from-[#0D9488] to-[#14B8A6] text-white rounded-xl font-medium shadow-lg hover:shadow-xl hover:scale-105 transition-all duration-200"
          >
            Retry
          </button>
        </div>
      </div>
    );
  }

  const totalAmount = payrollRuns.reduce((sum, run) => sum + (run.totalAmount || 0), 0);
  const totalEmployees = payrollRuns.reduce((sum, run) => sum + (run.totalEmployees || 0), 0);
  const activeRuns = payrollRuns.filter(r => ['Calculating', 'UnderReview', 'Approved'].includes(r.status)).length;

  return (
    <div className="p-6 md:p-8 animate-fade-in max-w-[1600px] mx-auto">
      {/* Header */}
      <div className="flex flex-col md:flex-row md:justify-between md:items-start gap-4 mb-8">
        <div>
          <h1 className="text-3xl md:text-4xl font-bold text-[#1E3A5F] mb-2 tracking-tight">
            Payroll Management
          </h1>
          <p className="text-[#64748B] text-sm md:text-base">
            Monitor and process payroll across all periods
          </p>
        </div>
        <button
          onClick={() => setIsCreateDialogOpen(true)}
          className="group px-6 py-3 bg-gradient-to-r from-[#0D9488] to-[#14B8A6] text-white rounded-xl font-medium shadow-lg hover:shadow-xl hover:scale-105 transition-all duration-200 flex items-center justify-center gap-2"
        >
          <svg className="w-5 h-5 group-hover:rotate-90 transition-transform duration-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
          </svg>
          <span>New Payroll Run</span>
        </button>
      </div>

      {/* Summary Cards */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4 md:gap-6 mb-8">
        <div className="group bg-white/60 backdrop-blur rounded-2xl p-6 shadow-lg border border-gray-100 hover:shadow-xl hover:scale-[1.02] transition-all duration-300">
          <div className="flex items-start justify-between mb-4">
            <div>
              <p className="text-sm font-medium text-[#64748B] mb-1">Total Disbursed</p>
              <MoneyDisplay amount={totalAmount} className="text-2xl md:text-3xl font-bold text-[#1E3A5F]" />
            </div>
            <div className="w-14 h-14 bg-gradient-to-br from-[#0D9488] to-[#14B8A6] rounded-2xl flex items-center justify-center shadow-lg group-hover:scale-110 transition-transform duration-300">
              <svg className="w-7 h-7 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
            </div>
          </div>
          <div className="flex items-center gap-2 text-xs text-[#64748B]">
            <span className="px-2 py-1 bg-[#0D9488]/10 text-[#0D9488] rounded-lg font-medium">All periods</span>
          </div>
        </div>

        <div className="group bg-white/60 backdrop-blur rounded-2xl p-6 shadow-lg border border-gray-100 hover:shadow-xl hover:scale-[1.02] transition-all duration-300">
          <div className="flex items-start justify-between mb-4">
            <div>
              <p className="text-sm font-medium text-[#64748B] mb-1">Total Employees</p>
              <div className="text-2xl md:text-3xl font-bold text-[#1E3A5F]">{totalEmployees}</div>
            </div>
            <div className="w-14 h-14 bg-gradient-to-br from-[#F59E0B] to-[#FCD34D] rounded-2xl flex items-center justify-center shadow-lg group-hover:scale-110 transition-transform duration-300">
              <svg className="w-7 h-7 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z" />
              </svg>
            </div>
          </div>
          <div className="flex items-center gap-2 text-xs text-[#64748B]">
            <span className="px-2 py-1 bg-[#F59E0B]/10 text-[#F59E0B] rounded-lg font-medium">In system</span>
          </div>
        </div>

        <div className="group bg-white/60 backdrop-blur rounded-2xl p-6 shadow-lg border border-gray-100 hover:shadow-xl hover:scale-[1.02] transition-all duration-300">
          <div className="flex items-start justify-between mb-4">
            <div>
              <p className="text-sm font-medium text-[#64748B] mb-1">Active Runs</p>
              <div className="text-2xl md:text-3xl font-bold text-[#1E3A5F]">{activeRuns}</div>
            </div>
            <div className="w-14 h-14 bg-gradient-to-br from-[#1E3A5F] to-[#2D5278] rounded-2xl flex items-center justify-center shadow-lg group-hover:scale-110 transition-transform duration-300">
              <svg className="w-7 h-7 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 10V3L4 14h7v7l9-11h-7z" />
              </svg>
            </div>
          </div>
          <div className="flex items-center gap-2 text-xs text-[#64748B]">
            <div className="w-2 h-2 bg-[#0D9488] rounded-full animate-pulse" />
            <span>In progress</span>
          </div>
        </div>
      </div>

      {/* Filter Pills */}
      <div className="mb-6 flex flex-wrap gap-2">
        {statusOptions.map((option) => {
          const count = option.value === 'all'
            ? payrollRuns.length
            : payrollRuns.filter(r => r.status === option.value).length;
          
          return (
            <button
              key={option.value}
              onClick={() => setStatusFilter(option.value)}
              className={`
                group px-4 py-2.5 rounded-xl text-sm font-medium transition-all duration-200 flex items-center gap-2
                ${statusFilter === option.value
                  ? 'bg-gradient-to-r from-[#1E3A5F] to-[#2D5278] text-white shadow-lg scale-105'
                  : 'bg-white/60 backdrop-blur text-[#64748B] hover:bg-white hover:shadow-md border border-gray-200'
                }
              `}
            >
              <span className="text-base">{option.icon}</span>
              <span>{option.label}</span>
              <span className={`
                px-2 py-0.5 rounded-lg text-xs font-semibold
                ${statusFilter === option.value 
                  ? 'bg-white/20 text-white' 
                  : 'bg-gray-100 text-[#64748B] group-hover:bg-gray-200'
                }
              `}>
                {count}
              </span>
            </button>
          );
        })}
      </div>

      {/* Payroll Runs Grid/List */}
      {payrollRuns.length > 0 ? (
        <div className="grid grid-cols-1 gap-4">
          {payrollRuns.map((run, index) => (
            <Link
              key={run.id}
              href={`/payroll/${run.id}`}
              className="group block bg-white/60 backdrop-blur rounded-2xl p-6 shadow-lg border border-gray-100 hover:shadow-xl hover:scale-[1.01] transition-all duration-300 animate-slide-in"
              style={{ animationDelay: `${index * 50}ms` }}
            >
              <div className="flex flex-col md:flex-row md:items-center gap-4">
                {/* Period & Status */}
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-3 mb-2">
                    <PayrollPeriodDisplay
                      month={run.month}
                      year={run.year}
                      className="text-lg font-bold text-[#1E3A5F]"
                    />
                    <StatusBadge status={run.status} />
                  </div>
                  <div className="flex items-center gap-2 text-sm text-[#64748B]">
                    <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
                    </svg>
                    <span>{run.totalEmployees} employees</span>
                    <span className="text-[#94A3B8]">•</span>
                    <span>Created by {run.createdBy}</span>
                    <span className="text-[#94A3B8]">•</span>
                    <span>{new Date(run.createdAt).toLocaleDateString('id-ID', { day: 'numeric', month: 'short', year: 'numeric' })}</span>
                  </div>
                </div>

                {/* Amount */}
                <div className="flex items-center gap-4">
                  <div className="text-right">
                    <p className="text-xs text-[#64748B] mb-1">Total Amount</p>
                    <MoneyDisplay
                      amount={run.totalAmount}
                      className="text-xl font-bold text-[#1E3A5F]"
                    />
                  </div>
                  
                  <div className="w-10 h-10 bg-gradient-to-br from-[#0D9488] to-[#14B8A6] rounded-xl flex items-center justify-center group-hover:scale-110 transition-transform duration-300">
                    <svg className="w-5 h-5 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
                    </svg>
                  </div>
                </div>
              </div>
            </Link>
          ))}
        </div>
      ) : (
        <div className="bg-white/60 backdrop-blur rounded-2xl shadow-lg border border-gray-100 p-12 text-center">
          <div className="max-w-md mx-auto">
            <div className="w-24 h-24 bg-gradient-to-br from-gray-100 to-gray-50 rounded-3xl flex items-center justify-center mx-auto mb-6 shadow-lg">
              <svg className="w-12 h-12 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
              </svg>
            </div>
            <h3 className="text-xl font-bold text-[#1E3A5F] mb-3">No Payroll Runs Yet</h3>
            <p className="text-[#64748B] mb-6">Start by creating your first payroll run to process employee salaries</p>
            <button
              onClick={() => setIsCreateDialogOpen(true)}
              className="px-6 py-3 bg-gradient-to-r from-[#0D9488] to-[#14B8A6] text-white rounded-xl font-medium shadow-lg hover:shadow-xl hover:scale-105 transition-all duration-200 inline-flex items-center gap-2"
            >
              <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
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

// Made with Bob