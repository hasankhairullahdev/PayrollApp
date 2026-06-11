'use client';

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useParams, useRouter } from 'next/navigation';
import { useState } from 'react';
import { StatusBadge } from '@/components/StatusBadge';
import { MoneyDisplay } from '@/components/MoneyDisplay';
import { PayrollPeriodDisplay } from '@/components/PayrollPeriodDisplay';
import { ConfirmDialog } from '@/components/ConfirmDialog';
import { api } from '@/lib/api';

interface PayrollRunDetail {
  id: string;
  month: number;
  year: number;
  status: string;
  totalAmount: number;
  employeeCount: number;
  createdAt: string;
  createdBy: string;
  approvedAt?: string;
  approvedBy?: string;
  lockedAt?: string;
  lockedBy?: string;
}

interface LineItem {
  employeeId: string;
  employeeName: string;
  basicSalary: number;
  allowances: number;
  overtime: number;
  grossSalary: number;
  bpjsKesehatan: number;
  bpjsTk: number;
  pph21: number;
  totalDeductions: number;
  netSalary: number;
}

export default function PayrollDetailPage() {
  const params = useParams();
  const router = useRouter();
  const id = params.id as string;
  const queryClient = useQueryClient();
  const [activeTab, setActiveTab] = useState<'line-items' | 'summary' | 'timeline'>('line-items');
  const [confirmDialog, setConfirmDialog] = useState<{
    isOpen: boolean;
    action: 'approve' | 'lock' | null;
  }>({ isOpen: false, action: null });

  const { data: payrollRun, isLoading } = useQuery({
    queryKey: ['payroll-run', id],
    queryFn: async () => {
      const response = await api.get<PayrollRunDetail>(`/api/payroll/${id}`);
      return response.data;
    },
  });

  const { data: lineItems } = useQuery({
    queryKey: ['payroll-line-items', id],
    queryFn: async () => {
      const response = await api.get<LineItem[]>(`/api/payroll/${id}/line-items`);
      return response.data;
    },
    enabled: activeTab === 'line-items',
  });

  const approveMutation = useMutation({
    mutationFn: async () => {
      await api.post(`/api/payroll/${id}/approve`, {
        approvedBy: 'current-user',
      });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['payroll-run', id] });
      setConfirmDialog({ isOpen: false, action: null });
    },
  });

  const lockMutation = useMutation({
    mutationFn: async () => {
      await api.post(`/api/payroll/${id}/lock`, {
        lockedBy: 'current-user',
      });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['payroll-run', id] });
      setConfirmDialog({ isOpen: false, action: null });
    },
  });

  if (isLoading) {
    return (
      <div className="p-8 animate-fade-in">
        <div className="mb-8">
          <div className="h-8 w-64 bg-gray-200 rounded skeleton mb-2" />
          <div className="h-4 w-96 bg-gray-200 rounded skeleton" />
        </div>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-8">
          {[1, 2, 3].map((i) => (
            <div key={i} className="bg-white rounded-xl p-6 shadow-md">
              <div className="h-4 w-24 bg-gray-200 rounded skeleton mb-3" />
              <div className="h-10 w-32 bg-gray-200 rounded skeleton" />
            </div>
          ))}
        </div>
      </div>
    );
  }

  if (!payrollRun) {
    return (
      <div className="flex items-center justify-center h-[calc(100vh-4rem)] animate-fade-in">
        <div className="text-center">
          <div className="w-16 h-16 bg-red-100 rounded-full flex items-center justify-center mx-auto mb-4">
            <svg className="w-8 h-8 text-red-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
          </div>
          <h3 className="text-lg font-semibold text-[#1E3A5F] mb-2">Payroll Run Not Found</h3>
          <p className="text-sm text-[#64748B] mb-4">The requested payroll run does not exist</p>
          <button
            onClick={() => router.push('/payroll')}
            className="px-4 py-2 bg-[#0D9488] text-white rounded-lg text-sm font-medium hover:bg-[#0F766E] transition-colors"
          >
            Back to Payroll Runs
          </button>
        </div>
      </div>
    );
  }

  const canApprove = payrollRun.status === 'UnderReview';
  const canLock = payrollRun.status === 'Approved';

  const totalGross = lineItems?.reduce((sum, item) => sum + item.grossSalary, 0) || 0;
  const totalDeductions = lineItems?.reduce((sum, item) => sum + item.totalDeductions, 0) || 0;

  return (
    <div className="p-8 animate-fade-in">
      {/* Back Button */}
      <button
        onClick={() => router.push('/payroll')}
        className="mb-6 flex items-center gap-2 text-[#64748B] hover:text-[#1E3A5F] transition-colors"
      >
        <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 19l-7-7 7-7" />
        </svg>
        <span className="text-sm font-medium">Back to Payroll Runs</span>
      </button>

      {/* Header */}
      <div className="mb-8">
        <div className="flex justify-between items-start mb-6">
          <div>
            <h1 className="text-3xl font-bold text-[#1E3A5F] mb-2 tracking-tight">
              <PayrollPeriodDisplay month={payrollRun.month} year={payrollRun.year} />
            </h1>
            <p className="text-[#64748B]">
              Created by <span className="font-medium text-[#1E3A5F]">{payrollRun.createdBy}</span> on{' '}
              {new Date(payrollRun.createdAt).toLocaleDateString('id-ID', {
                day: 'numeric',
                month: 'long',
                year: 'numeric'
              })}
            </p>
          </div>
          <div className="flex gap-3">
            {canApprove && (
              <button
                onClick={() => setConfirmDialog({ isOpen: true, action: 'approve' })}
                className="px-6 py-3 bg-gradient-to-r from-[#059669] to-[#10B981] text-white rounded-lg font-medium shadow-md hover:shadow-lg hover:scale-105 transition-all duration-200 flex items-center gap-2"
              >
                <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
                Approve Payroll
              </button>
            )}
            {canLock && (
              <button
                onClick={() => setConfirmDialog({ isOpen: true, action: 'lock' })}
                className="px-6 py-3 bg-gradient-to-r from-[#1E3A5F] to-[#2D5278] text-white rounded-lg font-medium shadow-md hover:shadow-lg hover:scale-105 transition-all duration-200 flex items-center gap-2"
              >
                <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />
                </svg>
                Lock Payroll
              </button>
            )}
          </div>
        </div>

        {/* Summary Cards */}
        <div className="grid grid-cols-1 md:grid-cols-4 gap-6">
          <div className="bg-white rounded-xl p-6 shadow-md border border-gray-100 hover-lift">
            <div className="flex items-center justify-between mb-3">
              <span className="text-sm font-medium text-[#64748B]">Status</span>
              <div className="w-10 h-10 bg-gradient-to-br from-[#0D9488] to-[#14B8A6] rounded-lg flex items-center justify-center">
                <svg className="w-5 h-5 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
              </div>
            </div>
            <StatusBadge status={payrollRun.status} />
          </div>

          <div className="bg-white rounded-xl p-6 shadow-md border border-gray-100 hover-lift">
            <div className="flex items-center justify-between mb-3">
              <span className="text-sm font-medium text-[#64748B]">Employees</span>
              <div className="w-10 h-10 bg-gradient-to-br from-[#F59E0B] to-[#FCD34D] rounded-lg flex items-center justify-center">
                <svg className="w-5 h-5 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z" />
                </svg>
              </div>
            </div>
            <div className="text-2xl font-bold text-[#1E3A5F]">{payrollRun.employeeCount}</div>
          </div>

          <div className="bg-white rounded-xl p-6 shadow-md border border-gray-100 hover-lift">
            <div className="flex items-center justify-between mb-3">
              <span className="text-sm font-medium text-[#64748B]">Gross Amount</span>
              <div className="w-10 h-10 bg-gradient-to-br from-[#1E3A5F] to-[#2D5278] rounded-lg flex items-center justify-center">
                <svg className="w-5 h-5 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
              </div>
            </div>
            <MoneyDisplay amount={totalGross} className="text-xl font-bold text-[#1E3A5F]" />
          </div>

          <div className="bg-white rounded-xl p-6 shadow-md border border-gray-100 hover-lift">
            <div className="flex items-center justify-between mb-3">
              <span className="text-sm font-medium text-[#64748B]">Net Amount</span>
              <div className="w-10 h-10 bg-gradient-to-br from-[#059669] to-[#10B981] rounded-lg flex items-center justify-center">
                <svg className="w-5 h-5 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17 9V7a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2m2 4h10a2 2 0 002-2v-6a2 2 0 00-2-2H9a2 2 0 00-2 2v6a2 2 0 002 2zm7-5a2 2 0 11-4 0 2 2 0 014 0z" />
                </svg>
              </div>
            </div>
            <MoneyDisplay amount={payrollRun.totalAmount} className="text-xl font-bold text-[#1E3A5F]" />
          </div>
        </div>
      </div>

      {/* Tabs */}
      <div className="border-b border-gray-200 mb-6">
        <nav className="-mb-px flex space-x-8">
          {[
            { id: 'line-items', label: 'Line Items', icon: '📋' },
            { id: 'summary', label: 'Summary', icon: '📊' },
            { id: 'timeline', label: 'Timeline', icon: '⏱️' },
          ].map((tab) => (
            <button
              key={tab.id}
              onClick={() => setActiveTab(tab.id as any)}
              className={`
                group py-4 px-1 border-b-2 font-medium text-sm transition-all duration-200
                ${activeTab === tab.id
                  ? 'border-[#0D9488] text-[#0D9488]'
                  : 'border-transparent text-[#64748B] hover:text-[#1E3A5F] hover:border-gray-300'
                }
              `}
            >
              <span className="flex items-center gap-2">
                <span>{tab.icon}</span>
                <span>{tab.label}</span>
              </span>
            </button>
          ))}
        </nav>
      </div>

      {/* Tab Content */}
      {activeTab === 'line-items' && (
        <div className="bg-white rounded-xl shadow-md border border-gray-100 overflow-hidden">
          <div className="overflow-x-auto">
            <table className="min-w-full">
              <thead>
                <tr className="bg-gradient-to-r from-[#1E3A5F] to-[#2D5278] text-white">
                  <th className="px-6 py-4 text-left text-xs font-semibold uppercase tracking-wider sticky left-0 bg-[#1E3A5F]">
                    Employee
                  </th>
                  <th className="px-6 py-4 text-right text-xs font-semibold uppercase tracking-wider">
                    Basic Salary
                  </th>
                  <th className="px-6 py-4 text-right text-xs font-semibold uppercase tracking-wider">
                    Allowances
                  </th>
                  <th className="px-6 py-4 text-right text-xs font-semibold uppercase tracking-wider">
                    Overtime
                  </th>
                  <th className="px-6 py-4 text-right text-xs font-semibold uppercase tracking-wider">
                    Gross
                  </th>
                  <th className="px-6 py-4 text-right text-xs font-semibold uppercase tracking-wider">
                    BPJS
                  </th>
                  <th className="px-6 py-4 text-right text-xs font-semibold uppercase tracking-wider">
                    PPh 21
                  </th>
                  <th className="px-6 py-4 text-right text-xs font-semibold uppercase tracking-wider">
                    Net Salary
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {lineItems && lineItems.length > 0 ? (
                  lineItems.map((item, index) => (
                    <tr 
                      key={item.employeeId} 
                      className="hover:bg-[#F8FAFC] transition-colors duration-150"
                    >
                      <td className="px-6 py-4 whitespace-nowrap sticky left-0 bg-white hover:bg-[#F8FAFC]">
                        <div className="text-sm font-semibold text-[#1E3A5F]">{item.employeeName}</div>
                        <div className="text-xs text-[#64748B] font-mono">{item.employeeId}</div>
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-right">
                        <MoneyDisplay amount={item.basicSalary} className="text-sm text-[#1E3A5F]" />
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-right">
                        <MoneyDisplay amount={item.allowances} className="text-sm text-[#1E3A5F]" />
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-right">
                        <MoneyDisplay amount={item.overtime} className="text-sm text-[#1E3A5F]" />
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-right">
                        <MoneyDisplay amount={item.grossSalary} className="text-sm font-semibold text-[#1E3A5F]" />
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-right">
                        <MoneyDisplay amount={item.bpjsKesehatan + item.bpjsTk} className="text-sm text-red-600" />
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-right">
                        <MoneyDisplay amount={item.pph21} className="text-sm text-red-600" />
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-right">
                        <MoneyDisplay amount={item.netSalary} className="text-sm font-bold text-[#059669]" />
                      </td>
                    </tr>
                  ))
                ) : (
                  <tr>
                    <td colSpan={8} className="px-6 py-12 text-center text-[#64748B]">
                      No line items available
                    </td>
                  </tr>
                )}
              </tbody>
              {lineItems && lineItems.length > 0 && (
                <tfoot>
                  <tr className="bg-gray-50 font-semibold">
                    <td className="px-6 py-4 text-sm text-[#1E3A5F]">Total</td>
                    <td className="px-6 py-4 text-right">
                      <MoneyDisplay amount={lineItems.reduce((sum, item) => sum + item.basicSalary, 0)} className="text-sm text-[#1E3A5F]" />
                    </td>
                    <td className="px-6 py-4 text-right">
                      <MoneyDisplay amount={lineItems.reduce((sum, item) => sum + item.allowances, 0)} className="text-sm text-[#1E3A5F]" />
                    </td>
                    <td className="px-6 py-4 text-right">
                      <MoneyDisplay amount={lineItems.reduce((sum, item) => sum + item.overtime, 0)} className="text-sm text-[#1E3A5F]" />
                    </td>
                    <td className="px-6 py-4 text-right">
                      <MoneyDisplay amount={totalGross} className="text-sm font-bold text-[#1E3A5F]" />
                    </td>
                    <td className="px-6 py-4 text-right">
                      <MoneyDisplay amount={lineItems.reduce((sum, item) => sum + item.bpjsKesehatan + item.bpjsTk, 0)} className="text-sm text-red-600" />
                    </td>
                    <td className="px-6 py-4 text-right">
                      <MoneyDisplay amount={lineItems.reduce((sum, item) => sum + item.pph21, 0)} className="text-sm text-red-600" />
                    </td>
                    <td className="px-6 py-4 text-right">
                      <MoneyDisplay amount={payrollRun.totalAmount} className="text-sm font-bold text-[#059669]" />
                    </td>
                  </tr>
                </tfoot>
              )}
            </table>
          </div>
        </div>
      )}

      {activeTab === 'summary' && (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          <div className="bg-white rounded-xl shadow-md border border-gray-100 p-6">
            <h3 className="text-lg font-semibold text-[#1E3A5F] mb-4 flex items-center gap-2">
              <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 7h6m0 10v-3m-3 3h.01M9 17h.01M9 14h.01M12 14h.01M15 11h.01M12 11h.01M9 11h.01M7 21h10a2 2 0 002-2V5a2 2 0 00-2-2H7a2 2 0 00-2 2v14a2 2 0 002 2z" />
              </svg>
              Financial Summary
            </h3>
            <div className="space-y-4">
              <div className="flex justify-between items-center py-3 border-b border-gray-100">
                <span className="text-sm text-[#64748B]">Total Employees</span>
                <span className="font-semibold text-[#1E3A5F]">{payrollRun.employeeCount}</span>
              </div>
              <div className="flex justify-between items-center py-3 border-b border-gray-100">
                <span className="text-sm text-[#64748B]">Gross Amount</span>
                <MoneyDisplay amount={totalGross} className="font-semibold text-[#1E3A5F]" />
              </div>
              <div className="flex justify-between items-center py-3 border-b border-gray-100">
                <span className="text-sm text-[#64748B]">Total Deductions</span>
                <MoneyDisplay amount={totalDeductions} className="font-semibold text-red-600" />
              </div>
              <div className="flex justify-between items-center py-3 bg-[#F8FAFC] -mx-6 px-6 rounded-lg">
                <span className="text-sm font-semibold text-[#1E3A5F]">Net Amount</span>
                <MoneyDisplay amount={payrollRun.totalAmount} className="font-bold text-lg text-[#059669]" />
              </div>
            </div>
          </div>

          <div className="bg-white rounded-xl shadow-md border border-gray-100 p-6">
            <h3 className="text-lg font-semibold text-[#1E3A5F] mb-4 flex items-center gap-2">
              <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
              Approval Information
            </h3>
            <div className="space-y-4">
              {payrollRun.approvedBy && (
                <>
                  <div className="flex justify-between items-center py-3 border-b border-gray-100">
                    <span className="text-sm text-[#64748B]">Approved By</span>
                    <span className="font-semibold text-[#1E3A5F]">{payrollRun.approvedBy}</span>
                  </div>
                  <div className="flex justify-between items-center py-3 border-b border-gray-100">
                    <span className="text-sm text-[#64748B]">Approved At</span>
                    <span className="font-semibold text-[#1E3A5F]">
                      {payrollRun.approvedAt && new Date(payrollRun.approvedAt).toLocaleString('id-ID')}
                    </span>
                  </div>
                </>
              )}
              {payrollRun.lockedBy && (
                <>
                  <div className="flex justify-between items-center py-3 border-b border-gray-100">
                    <span className="text-sm text-[#64748B]">Locked By</span>
                    <span className="font-semibold text-[#1E3A5F]">{payrollRun.lockedBy}</span>
                  </div>
                  <div className="flex justify-between items-center py-3 border-b border-gray-100">
                    <span className="text-sm text-[#64748B]">Locked At</span>
                    <span className="font-semibold text-[#1E3A5F]">
                      {payrollRun.lockedAt && new Date(payrollRun.lockedAt).toLocaleString('id-ID')}
                    </span>
                  </div>
                </>
              )}
              {!payrollRun.approvedBy && !payrollRun.lockedBy && (
                <div className="text-center py-8 text-[#64748B]">
                  <svg className="w-12 h-12 mx-auto mb-3 opacity-50" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />
                  </svg>
                  <p className="text-sm">No approval information yet</p>
                </div>
              )}
            </div>
          </div>
        </div>
      )}

      {activeTab === 'timeline' && (
        <div className="bg-white rounded-xl shadow-md border border-gray-100 p-8">
          <h3 className="text-lg font-semibold text-[#1E3A5F] mb-6 flex items-center gap-2">
            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
            Payroll Timeline
          </h3>
          <div className="space-y-6 relative before:absolute before:left-[11px] before:top-[20px] before:bottom-[20px] before:w-0.5 before:bg-gray-200">
            <div className="flex gap-4 relative">
              <div className="flex-shrink-0 w-6 h-6 bg-[#0D9488] rounded-full flex items-center justify-center z-10 shadow-md">
                <svg className="w-3 h-3 text-white" fill="currentColor" viewBox="0 0 20 20">
                  <path fillRule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clipRule="evenodd" />
                </svg>
              </div>
              <div className="flex-1 pb-6">
                <div className="font-semibold text-[#1E3A5F] mb-1">Created</div>
                <div className="text-sm text-[#64748B]">
                  {new Date(payrollRun.createdAt).toLocaleString('id-ID', {
                    day: 'numeric',
                    month: 'long',
                    year: 'numeric',
                    hour: '2-digit',
                    minute: '2-digit'
                  })}
                </div>
                <div className="text-xs text-[#64748B] mt-1">by {payrollRun.createdBy}</div>
              </div>
            </div>

            {payrollRun.approvedAt && (
              <div className="flex gap-4 relative">
                <div className="flex-shrink-0 w-6 h-6 bg-[#059669] rounded-full flex items-center justify-center z-10 shadow-md">
                  <svg className="w-3 h-3 text-white" fill="currentColor" viewBox="0 0 20 20">
                    <path fillRule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clipRule="evenodd" />
                  </svg>
                </div>
                <div className="flex-1 pb-6">
                  <div className="font-semibold text-[#1E3A5F] mb-1">Approved</div>
                  <div className="text-sm text-[#64748B]">
                    {new Date(payrollRun.approvedAt).toLocaleString('id-ID', {
                      day: 'numeric',
                      month: 'long',
                      year: 'numeric',
                      hour: '2-digit',
                      minute: '2-digit'
                    })}
                  </div>
                  <div className="text-xs text-[#64748B] mt-1">by {payrollRun.approvedBy}</div>
                </div>
              </div>
            )}

            {payrollRun.lockedAt && (
              <div className="flex gap-4 relative">
                <div className="flex-shrink-0 w-6 h-6 bg-[#1E3A5F] rounded-full flex items-center justify-center z-10 shadow-md">
                  <svg className="w-3 h-3 text-white" fill="currentColor" viewBox="0 0 20 20">
                    <path fillRule="evenodd" d="M5 9V7a5 5 0 0110 0v2a2 2 0 012 2v5a2 2 0 01-2 2H5a2 2 0 01-2-2v-5a2 2 0 012-2zm8-2v2H7V7a3 3 0 016 0z" clipRule="evenodd" />
                  </svg>
                </div>
                <div className="flex-1">
                  <div className="font-semibold text-[#1E3A5F] mb-1">Locked</div>
                  <div className="text-sm text-[#64748B]">
                    {new Date(payrollRun.lockedAt).toLocaleString('id-ID', {
                      day: 'numeric',
                      month: 'long',
                      year: 'numeric',
                      hour: '2-digit',
                      minute: '2-digit'
                    })}
                  </div>
                  <div className="text-xs text-[#64748B] mt-1">by {payrollRun.lockedBy}</div>
                </div>
              </div>
            )}
          </div>
        </div>
      )}

      {/* Confirm Dialogs */}
      <ConfirmDialog
        isOpen={confirmDialog.isOpen && confirmDialog.action === 'approve'}
        title="Approve Payroll"
        description="Are you sure you want to approve this payroll run? This action cannot be undone."
        confirmText="Approve"
        cancelText="Cancel"
        variant="primary"
        onConfirm={() => approveMutation.mutate()}
        onCancel={() => setConfirmDialog({ isOpen: false, action: null })}
      />

      <ConfirmDialog
        isOpen={confirmDialog.isOpen && confirmDialog.action === 'lock'}
        title="Lock Payroll"
        description="Are you sure you want to lock this payroll run? After locking, no changes can be made."
        confirmText="Lock"
        cancelText="Cancel"
        variant="danger"
        onConfirm={() => lockMutation.mutate()}
        onCancel={() => setConfirmDialog({ isOpen: false, action: null })}
      />
    </div>
  );
}

// Made with Bob