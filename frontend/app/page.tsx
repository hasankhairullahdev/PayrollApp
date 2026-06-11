'use client';

import { useQuery } from '@tanstack/react-query';
import { payrollApi } from '@/lib/api';
import Link from 'next/link';

export default function Home() {
  const { data, isLoading, error } = useQuery({
    queryKey: ['payroll-runs', { pageSize: 5 }],
    queryFn: () => payrollApi.getPayrollRuns({ pageSize: 5 }),
  });

  return (
    <div className="p-6 md:p-8 space-y-8 max-w-[1600px] mx-auto">
      {/* Header */}
      <div className="animate-fade-in">
        <h1 className="text-3xl md:text-4xl font-bold text-[#1E3A5F] tracking-tight">Dashboard</h1>
        <p className="mt-2 text-[#64748B]">
          Welcome to Payroll Management System
        </p>
      </div>

      {/* Stats Cards */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4 md:gap-6 animate-fade-in">
        <div className="group bg-white/60 backdrop-blur rounded-2xl p-6 shadow-lg border border-gray-100 hover:shadow-xl hover:scale-[1.02] transition-all duration-300">
          <div className="flex items-center gap-4">
            <div className="flex-shrink-0 w-14 h-14 bg-gradient-to-br from-[#0D9488] to-[#14B8A6] rounded-2xl p-3 shadow-lg group-hover:scale-110 transition-transform duration-300">
              <svg
                className="w-full h-full text-white"
                fill="none"
                viewBox="0 0 24 24"
                stroke="currentColor"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
                />
              </svg>
            </div>
            <div>
              <p className="text-sm font-medium text-[#64748B]">
                Total Payroll Runs
              </p>
              <p className="text-2xl md:text-3xl font-bold text-[#1E3A5F]">
                {data?.totalCount || 0}
              </p>
            </div>
          </div>
        </div>

        <div className="group bg-white/60 backdrop-blur rounded-2xl p-6 shadow-lg border border-gray-100 hover:shadow-xl hover:scale-[1.02] transition-all duration-300">
          <div className="flex items-center gap-4">
            <div className="flex-shrink-0 w-14 h-14 bg-gradient-to-br from-[#F59E0B] to-[#FCD34D] rounded-2xl p-3 shadow-lg group-hover:scale-110 transition-transform duration-300">
              <svg
                className="w-full h-full text-white"
                fill="none"
                viewBox="0 0 24 24"
                stroke="currentColor"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z"
                />
              </svg>
            </div>
            <div>
              <p className="text-sm font-medium text-[#64748B]">
                Total Employees
              </p>
              <p className="text-2xl md:text-3xl font-bold text-[#1E3A5F]">
                {data?.items[0]?.totalEmployees || 0}
              </p>
            </div>
          </div>
        </div>

        <div className="group bg-white/60 backdrop-blur rounded-2xl p-6 shadow-lg border border-gray-100 hover:shadow-xl hover:scale-[1.02] transition-all duration-300">
          <div className="flex items-center gap-4">
            <div className="flex-shrink-0 w-14 h-14 bg-gradient-to-br from-[#1E3A5F] to-[#2D5278] rounded-2xl p-3 shadow-lg group-hover:scale-110 transition-transform duration-300">
              <svg
                className="w-full h-full text-white"
                fill="none"
                viewBox="0 0 24 24"
                stroke="currentColor"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
                />
              </svg>
            </div>
            <div>
              <p className="text-sm font-medium text-[#64748B]">
                Latest Total Amount
              </p>
              <p className="text-xl md:text-2xl font-bold text-[#1E3A5F] money-display">
                {data?.items[0]?.totalAmountDisplay || 'Rp 0'}
              </p>
            </div>
          </div>
        </div>
      </div>

      {/* Recent Payroll Runs */}
      <div className="bg-white/60 backdrop-blur rounded-2xl shadow-lg border border-gray-100 overflow-hidden animate-fade-in">
        <div className="px-6 py-5 border-b border-gray-200/50">
          <div className="flex items-center justify-between">
            <h2 className="text-xl font-bold text-[#1E3A5F]">
              Recent Payroll Runs
            </h2>
            <Link
              href="/payroll"
              className="text-sm font-medium text-[#0D9488] hover:text-[#0F766E] flex items-center gap-1 transition-colors"
            >
              View all
              <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
              </svg>
            </Link>
          </div>
        </div>

        <div className="p-6">
          {isLoading && (
            <div className="text-center py-12">
              <div className="inline-block animate-spin rounded-full h-12 w-12 border-4 border-[#0D9488] border-t-transparent"></div>
              <p className="mt-4 text-sm text-[#64748B]">Loading...</p>
            </div>
          )}

          {error && (
            <div className="text-center py-12">
              <div className="w-16 h-16 bg-red-100 rounded-full flex items-center justify-center mx-auto mb-4">
                <svg className="w-8 h-8 text-red-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
              </div>
              <p className="text-sm text-red-600 font-medium">
                Error loading payroll runs. Please try again.
              </p>
            </div>
          )}

          {data && data.items.length === 0 && (
            <div className="text-center py-12">
              <div className="w-20 h-20 bg-gradient-to-br from-gray-100 to-gray-50 rounded-3xl flex items-center justify-center mx-auto mb-6 shadow-lg">
                <svg className="w-10 h-10 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                </svg>
              </div>
              <p className="text-[#64748B] mb-6">
                No payroll runs yet. Create your first one!
              </p>
              <Link
                href="/payroll"
                className="inline-flex items-center gap-2 px-6 py-3 bg-gradient-to-r from-[#0D9488] to-[#14B8A6] text-white rounded-xl font-medium shadow-lg hover:shadow-xl hover:scale-105 transition-all duration-200"
              >
                <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
                </svg>
                Create Payroll Run
              </Link>
            </div>
          )}

          {data && data.items.length > 0 && (
            <div className="overflow-x-auto -mx-6 px-6">
              <table className="min-w-full">
                <thead>
                  <tr className="border-b border-gray-200">
                    <th className="px-4 py-3 text-left text-xs font-semibold text-[#64748B] uppercase tracking-wider">
                      Period
                    </th>
                    <th className="px-4 py-3 text-left text-xs font-semibold text-[#64748B] uppercase tracking-wider">
                      Status
                    </th>
                    <th className="px-4 py-3 text-left text-xs font-semibold text-[#64748B] uppercase tracking-wider">
                      Employees
                    </th>
                    <th className="px-4 py-3 text-left text-xs font-semibold text-[#64748B] uppercase tracking-wider">
                      Total Amount
                    </th>
                    <th className="px-4 py-3 text-left text-xs font-semibold text-[#64748B] uppercase tracking-wider">
                      Actions
                    </th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100">
                  {data.items.map((payroll, index) => (
                    <tr key={payroll.id} className="hover:bg-gray-50/50 transition-colors animate-slide-in" style={{ animationDelay: `${index * 50}ms` }}>
                      <td className="px-4 py-4 whitespace-nowrap text-sm font-semibold text-[#1E3A5F]">
                        {payroll.periodDisplay}
                      </td>
                      <td className="px-4 py-4 whitespace-nowrap">
                        <span
                          className={`px-3 py-1 inline-flex text-xs leading-5 font-semibold rounded-full ${
                            payroll.status === 'Locked'
                              ? 'bg-purple-50 text-purple-700 border border-purple-200'
                              : payroll.status === 'Approved'
                              ? 'bg-emerald-50 text-emerald-700 border border-emerald-200'
                              : payroll.status === 'Calculated'
                              ? 'bg-amber-50 text-amber-700 border border-amber-200'
                              : 'bg-slate-50 text-slate-700 border border-slate-200'
                          }`}
                        >
                          {payroll.statusDisplay}
                        </span>
                      </td>
                      <td className="px-4 py-4 whitespace-nowrap text-sm text-[#64748B]">
                        {payroll.totalEmployees}
                      </td>
                      <td className="px-4 py-4 whitespace-nowrap text-sm font-semibold text-[#1E3A5F] money-display">
                        {payroll.totalAmountDisplay}
                      </td>
                      <td className="px-4 py-4 whitespace-nowrap text-sm font-medium">
                        <Link
                          href={`/payroll/${payroll.id}`}
                          className="inline-flex items-center gap-1 text-[#0D9488] hover:text-[#0F766E] transition-colors"
                        >
                          View Details
                          <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
                          </svg>
                        </Link>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

// Made with Bob
