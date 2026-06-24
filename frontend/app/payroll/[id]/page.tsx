'use client';

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useParams, useRouter } from 'next/navigation';
import { useState, useMemo, useCallback } from 'react';
import { StatusBadge } from '@/components/StatusBadge';
import { MoneyDisplay } from '@/components/MoneyDisplay';
import { PayrollPeriodDisplay } from '@/components/PayrollPeriodDisplay';
import { PayrollActionDialog } from '@/components/PayrollActionDialog';
import { PayrollTimeline } from '@/components/PayrollTimeline';
import { PayrollSummaryCharts } from '@/components/PayrollSummaryCharts';
import { api, payrollApi } from '@/lib/api';

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
  employeeCode: string;
  employeeName: string;
  basicSalary: number;
  allowances: number;
  overtime: number;
  grossSalary: number;
  bpjsKesehatan: number;
  bpjsKetenagakerjaan: number;
  pph21: number;
  deductions: number;
  takeHomePay: number;
}

export default function PayrollDetailPage() {
  const params = useParams();
  const router = useRouter();
  const id = params.id as string;
  const queryClient = useQueryClient();
  const [activeTab, setActiveTab] = useState<'line-items' | 'summary' | 'timeline'>('line-items');
  const [searchQuery, setSearchQuery] = useState('');
  const [actionDialog, setActionDialog] = useState<{
    isOpen: boolean;
    action: 'start-review' | 'approve' | 'reject' | 'lock' | 'initiate-disbursement' | 'confirm-disbursement' | null;
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
    // Always fetch line items because summary tab needs the data for charts
  });

  // Memoize status checks (rerender-derived-state)
  const canStartReview = useMemo(() => payrollRun?.status === 'Calculated', [payrollRun?.status]);
  const canApproveOrReject = useMemo(() => payrollRun?.status === 'UnderReview', [payrollRun?.status]);
  const canLock = useMemo(() => payrollRun?.status === 'Approved', [payrollRun?.status]);
  const isLocked = useMemo(() => payrollRun?.status === 'Locked', [payrollRun?.status]);

  // Memoize filtered line items with search
  const filteredLineItems = useMemo(() => {
    if (!lineItems) return [];
    if (!searchQuery) return lineItems;
    
    const query = searchQuery.toLowerCase();
    return lineItems.filter(item =>
      item.employeeName.toLowerCase().includes(query) ||
      item.employeeCode.toLowerCase().includes(query) ||
      item.employeeId.toLowerCase().includes(query)
    );
  }, [lineItems, searchQuery]);

  // Memoize calculations (rerender-memo)
  const totals = useMemo(() => ({
    gross: filteredLineItems.reduce((sum, item) => sum + item.grossSalary, 0),
    deductions: filteredLineItems.reduce((sum, item) =>
      sum + item.bpjsKesehatan + item.bpjsKetenagakerjaan + item.pph21, 0),
    net: filteredLineItems.reduce((sum, item) => sum + item.takeHomePay, 0),
    basicSalary: filteredLineItems.reduce((sum, item) => sum + item.basicSalary, 0),
    allowances: filteredLineItems.reduce((sum, item) => sum + item.allowances, 0),
    overtime: filteredLineItems.reduce((sum, item) => sum + item.overtime, 0),
    bpjs: filteredLineItems.reduce((sum, item) => sum + item.bpjsKesehatan + item.bpjsKetenagakerjaan, 0),
    pph21: filteredLineItems.reduce((sum, item) => sum + item.pph21, 0),
  }), [filteredLineItems]);

  // Stable event handlers (rerender-functional-setstate)
  const handleStartReview = useCallback(() => {
    setActionDialog({ isOpen: true, action: 'start-review' });
  }, []);

  const handleApprove = useCallback(() => {
    setActionDialog({ isOpen: true, action: 'approve' });
  }, []);

  const handleReject = useCallback(() => {
    setActionDialog({ isOpen: true, action: 'reject' });
  }, []);

  const handleLock = useCallback(() => {
    setActionDialog({ isOpen: true, action: 'lock' });
  }, []);

  const handleInitiateDisbursement = useCallback(() => {
    setActionDialog({ isOpen: true, action: 'initiate-disbursement' });
  }, []);

  const handleConfirmDisbursement = useCallback(() => {
    setActionDialog({ isOpen: true, action: 'confirm-disbursement' });
  }, []);

  // Download handlers (rerender-functional-setstate)
  const handleExportExcel = useCallback(async () => {
    try {
      const blob = await payrollApi.exportPayrollExcel(id);
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `Payroll_${payrollRun?.month}_${payrollRun?.year}.xlsx`;
      document.body.appendChild(a);
      a.click();
      window.URL.revokeObjectURL(url);
      document.body.removeChild(a);
    } catch (error) {
      console.error('Failed to export Excel:', error);
      alert('Gagal export Excel. Silakan coba lagi.');
    }
  }, [id, payrollRun?.month, payrollRun?.year]);

  const handleGenerateBankFile = useCallback(async (bank: string) => {
    try {
      const blob = await payrollApi.generateBankFile(id, bank);
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `${bank.toUpperCase()}_Payroll_${payrollRun?.month}_${payrollRun?.year}.${bank === 'mandiri' ? 'csv' : 'txt'}`;
      document.body.appendChild(a);
      a.click();
      window.URL.revokeObjectURL(url);
      document.body.removeChild(a);
    } catch (error) {
      console.error('Failed to generate bank file:', error);
      alert('Gagal generate bank file. Silakan coba lagi.');
    }
  }, [id, payrollRun?.month, payrollRun?.year]);

  const handleDownloadPayslip = useCallback(async (employeeId: string, employeeName: string) => {
    try {
      const blob = await payrollApi.downloadPayslipPdf(id, employeeId);
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `Payslip_${employeeName.replace(/\s+/g, '_')}_${payrollRun?.month}_${payrollRun?.year}.pdf`;
      document.body.appendChild(a);
      a.click();
      window.URL.revokeObjectURL(url);
      document.body.removeChild(a);
    } catch (error) {
      console.error('Failed to download payslip:', error);
      alert('Gagal download payslip. Silakan coba lagi.');
    }
  }, [id, payrollRun?.month, payrollRun?.year]);

  if (isLoading) {
    return (
      <div className="p-6 md:p-8 animate-fade-in">
        <div className="mb-10">
          <div className="h-12 w-80 bg-gradient-to-r from-gray-200 via-gray-100 to-gray-200 rounded-2xl skeleton mb-4" />
          <div className="h-6 w-96 bg-gradient-to-r from-gray-200 via-gray-100 to-gray-200 rounded-xl skeleton" />
        </div>
        <div className="grid grid-cols-1 md:grid-cols-4 gap-6 mb-10">
          {[1, 2, 3, 4].map((i) => (
            <div key={i} className="card-premium rounded-3xl p-8 animate-pulse">
              <div className="h-6 w-24 bg-gradient-to-r from-gray-200 to-gray-100 rounded-xl skeleton mb-4" />
              <div className="h-12 w-32 bg-gradient-to-r from-gray-200 to-gray-100 rounded-2xl skeleton" />
            </div>
          ))}
        </div>
      </div>
    );
  }

  if (!payrollRun) {
    return (
      <div className="flex items-center justify-center h-[calc(100vh-4rem)] animate-fade-in">
        <div className="text-center animate-scale-in">
          <div className="w-24 h-24 bg-gradient-to-br from-red-100 to-red-50 rounded-3xl flex items-center justify-center mx-auto mb-6 shadow-2xl animate-float">
            <svg className="w-12 h-12 text-red-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
          </div>
          <h3 className="text-2xl font-bold gradient-text-navy mb-3">Payroll Run Not Found</h3>
          <p className="text-[#64748B] mb-8">The requested payroll run does not exist</p>
          <button
            onClick={() => router.push('/payroll')}
            className="px-8 py-4 text-white bg-gradient-to-r from-teal-600 to-emerald-600 rounded-2xl font-semibold shadow-xl hover:shadow-2xl hover:scale-105 transition-all duration-300"
          >
            Back to Payroll Runs
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="p-6 md:p-8 animate-fade-in max-w-[1800px] mx-auto">
      {/* Back Button */}
      <button
        onClick={() => router.push('/payroll')}
        className="mb-8 flex items-center gap-3 text-[#64748B] hover:text-[#0F172A] transition-all duration-300 group"
      >
        <div className="w-10 h-10 rounded-xl bg-white/60 backdrop-blur flex items-center justify-center group-hover:bg-gradient-to-r group-hover:from-teal-500 group-hover:to-emerald-500 transition-all duration-300 shadow-md">
          <svg className="w-5 h-5 group-hover:text-white transition-colors" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2.5} d="M15 19l-7-7 7-7" />
          </svg>
        </div>
        <span className="text-sm font-semibold">Back to Payroll Runs</span>
      </button>

      {/* Header */}
      <div className="mb-10">
        <div className="flex flex-col lg:flex-row lg:justify-between lg:items-start gap-6 mb-8">
          <div className="animate-slide-in">
            <h1 className="text-4xl md:text-5xl font-bold mb-3 tracking-tight bg-gradient-to-r from-teal-600 to-emerald-600 bg-clip-text text-transparent">
              <PayrollPeriodDisplay month={payrollRun.month} year={payrollRun.year} />
            </h1>
            <p className="text-[#64748B] text-lg flex items-center gap-2">
              <span className="w-2 h-2 bg-gradient-to-r from-teal-500 to-emerald-500 rounded-full animate-pulse-glow" />
              Created by <span className="font-semibold text-[#0F172A]">{payrollRun.createdBy}</span> on{' '}
              {new Date(payrollRun.createdAt).toLocaleDateString('id-ID', {
                day: 'numeric',
                month: 'long',
                year: 'numeric'
              })}
            </p>
          </div>
          <div className="flex gap-3 animate-slide-in" style={{ animationDelay: '100ms' }}>
            {canStartReview && (
              <button
                onClick={handleStartReview}
                className="px-6 py-3 bg-gradient-to-r from-amber-500 to-amber-600 text-white rounded-xl font-semibold shadow-lg hover:shadow-xl hover:scale-105 transition-all duration-300 flex items-center gap-2"
              >
                <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-6 9l2 2 4-4" />
                </svg>
                Mulai Review
              </button>
            )}
            {canApproveOrReject && (
              <>
                <button
                  onClick={handleApprove}
                  className="px-6 py-3 bg-gradient-to-r from-emerald-500 to-emerald-600 text-white rounded-xl font-semibold shadow-lg hover:shadow-xl hover:scale-105 transition-all duration-300 flex items-center gap-2"
                >
                  <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
                  </svg>
                  Approve
                </button>
                <button
                  onClick={handleReject}
                  className="px-6 py-3 bg-gradient-to-r from-red-500 to-red-600 text-white rounded-xl font-semibold shadow-lg hover:shadow-xl hover:scale-105 transition-all duration-300 flex items-center gap-2"
                >
                  <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M10 14l2-2m0 0l2-2m-2 2l-2-2m2 2l2 2m7-2a9 9 0 11-18 0 9 9 0 0118 0z" />
                  </svg>
                  Reject
                </button>
              </>
            )}
            {canLock && (
              <button
                onClick={handleLock}
                className="px-6 py-3 bg-gradient-to-r from-purple-500 to-purple-600 text-white rounded-xl font-semibold shadow-lg hover:shadow-xl hover:scale-105 transition-all duration-300 flex items-center gap-2"
              >
                <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />
                </svg>
                Lock & Generate Payslip
              </button>
            )}
            
            {/* Export Buttons - Always visible for calculated and beyond */}
            {payrollRun.status !== 'Draft' && payrollRun.status !== 'Calculating' && (
              <>
                <button
                  onClick={handleExportExcel}
                  className="px-6 py-3 bg-white border-2 border-teal-500 text-teal-600 rounded-xl font-semibold hover:bg-teal-50 transition-all duration-300 flex items-center gap-2"
                >
                  <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 10v6m0 0l-3-3m3 3l3-3m2 8H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                  </svg>
                  Export Excel
                </button>

                {isLocked && (
                  <>
                    <div className="relative group">
                      <button className="px-6 py-3 bg-white border-2 border-blue-500 text-blue-600 rounded-xl font-semibold hover:bg-blue-50 transition-all duration-300 flex items-center gap-2">
                        <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 7h12m0 0l-4-4m4 4l-4 4m0 6H4m0 0l4 4m-4-4l4-4" />
                        </svg>
                        Bank File
                        <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
                        </svg>
                      </button>
                      
                      {/* Dropdown */}
                      <div className="absolute right-0 mt-2 w-48 bg-white rounded-xl shadow-2xl border-2 border-gray-100 opacity-0 invisible group-hover:opacity-100 group-hover:visible transition-all duration-300 z-10">
                        {['BCA', 'Mandiri', 'BNI', 'Permata'].map((bank) => (
                          <button
                            key={bank}
                            onClick={() => handleGenerateBankFile(bank.toLowerCase())}
                            className="w-full px-4 py-3 text-left hover:bg-gray-50 first:rounded-t-xl last:rounded-b-xl transition-colors flex items-center gap-2"
                          >
                            <svg className="w-4 h-4 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
                            </svg>
                            <span className="font-medium text-gray-700">{bank}</span>
                          </button>
                        ))}
                      </div>
                    </div>
                    <button
                      onClick={handleInitiateDisbursement}
                      className="px-6 py-3 bg-gradient-to-r from-blue-500 to-blue-600 text-white rounded-xl font-semibold shadow-lg hover:shadow-xl hover:scale-105 transition-all duration-300 flex items-center gap-2"
                    >
                      <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 10h11M9 21V3m4 18h8m-4-4l4 4-4 4" />
                      </svg>
                      Initiate Disbursement
                    </button>
                    <button
                      onClick={handleConfirmDisbursement}
                      className="px-6 py-3 bg-gradient-to-r from-emerald-500 to-teal-600 text-white rounded-xl font-semibold shadow-lg hover:shadow-xl hover:scale-105 transition-all duration-300 flex items-center gap-2"
                    >
                      <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                      </svg>
                      Confirm Disbursement
                    </button>
                  </>
                )}
              </>
            )}
          </div>
        </div>

        {/* Summary Cards */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
          <div className="group card-premium rounded-2xl p-4 hover-lift text-center">
            {/* Header with Icon and Title - Highlighted */}
            <div className="flex items-center justify-center gap-2 mb-3 pb-2 bg-gradient-to-r from-teal-50 to-emerald-50 -mx-4 -mt-4 pt-4 px-4 rounded-t-2xl">
              <div className="w-8 h-8 bg-gradient-to-br from-teal-500 to-teal-600 rounded-lg flex items-center justify-center shadow group-hover:scale-110 transition-all duration-300 flex-shrink-0">
                <svg className="w-4 h-4 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
              </div>
              <p className="text-xs font-bold text-teal-700 uppercase tracking-wide">Status</p>
            </div>
            {/* Value */}
            <div className="flex justify-center mt-2">
              <StatusBadge status={payrollRun.status} />
            </div>
          </div>

          <div className="group card-premium rounded-2xl p-4 hover-lift text-center">
            {/* Header with Icon and Title - Highlighted */}
            <div className="flex items-center justify-center gap-2 mb-3 pb-2 bg-gradient-to-r from-amber-50 to-orange-50 -mx-4 -mt-4 pt-4 px-4 rounded-t-2xl">
              <div className="w-8 h-8 bg-gradient-to-br from-amber-500 to-amber-600 rounded-lg flex items-center justify-center shadow group-hover:scale-110 transition-all duration-300 flex-shrink-0">
                <svg className="w-4 h-4 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z" />
                </svg>
              </div>
              <p className="text-xs font-bold text-amber-700 uppercase tracking-wide">Employees</p>
            </div>
            {/* Value */}
            <div className="text-2xl font-bold gradient-text-navy mt-2">{lineItems?.length || 0}</div>
          </div>

          <div className="group card-premium rounded-2xl p-4 hover-lift text-center">
            {/* Header with Icon and Title - Highlighted */}
            <div className="flex items-center justify-center gap-2 mb-3 pb-2 bg-gradient-to-r from-blue-50 to-indigo-50 -mx-4 -mt-4 pt-4 px-4 rounded-t-2xl">
              <div className="w-8 h-8 bg-gradient-to-br from-blue-500 to-blue-600 rounded-lg flex items-center justify-center shadow group-hover:scale-110 transition-all duration-300 flex-shrink-0">
                <svg className="w-4 h-4 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
              </div>
              <p className="text-xs font-bold text-blue-700 uppercase tracking-wide">Gross Amount</p>
            </div>
            {/* Value */}
            <MoneyDisplay amount={totals.gross} className="text-lg font-bold mt-2" />
          </div>

          <div className="group card-premium rounded-2xl p-4 hover-lift text-center">
            {/* Header with Icon and Title - Highlighted */}
            <div className="flex items-center justify-center gap-2 mb-3 pb-2 bg-gradient-to-r from-emerald-50 to-teal-50 -mx-4 -mt-4 pt-4 px-4 rounded-t-2xl">
              <div className="w-8 h-8 bg-gradient-to-br from-emerald-500 to-emerald-600 rounded-lg flex items-center justify-center shadow group-hover:scale-110 transition-all duration-300 flex-shrink-0">
                <svg className="w-4 h-4 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17 9V7a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2m2 4h10a2 2 0 002-2v-6a2 2 0 00-2-2H9a2 2 0 00-2 2v6a2 2 0 002 2zm7-5a2 2 0 11-4 0 2 2 0 014 0z" />
                </svg>
              </div>
              <p className="text-xs font-bold text-emerald-700 uppercase tracking-wide">Net Amount</p>
            </div>
            {/* Value */}
            <MoneyDisplay amount={payrollRun.totalAmount} className="text-lg font-bold mt-2" />
          </div>
        </div>
      </div>

      {/* Tabs */}
      <div className="mb-8">
        <div className="flex flex-wrap gap-3">
          {[
            { id: 'line-items', label: 'Line Items', icon: '📋', color: 'from-teal-500 to-teal-600' },
            { id: 'summary', label: 'Summary', icon: '📊', color: 'from-blue-500 to-blue-600' },
            { id: 'timeline', label: 'Timeline', icon: '⏱️', color: 'from-purple-500 to-purple-600' },
          ].map((tab) => (
            <button
              key={tab.id}
              onClick={() => setActiveTab(tab.id as any)}
              className={`
                group px-6 py-3.5 rounded-2xl font-semibold text-sm transition-all duration-300 flex items-center gap-3 shadow-md
                ${activeTab === tab.id
                  ? `bg-gradient-to-r ${tab.color} text-white shadow-xl scale-105`
                  : 'bg-white text-[#64748B] hover:scale-105 hover:shadow-lg border-2 border-gray-200 hover:border-teal-500'
                }
              `}
            >
              <span className="text-lg">{tab.icon}</span>
              <span>{tab.label}</span>
            </button>
          ))}
        </div>
      </div>

      {/* Tab Content */}
      {activeTab === 'line-items' && (
        <div className="space-y-6">
          {/* Search Bar */}
          <div className="relative">
            <input
              type="text"
              placeholder="Search by employee name, code, or ID..."
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

          {/* Table */}
          <div className="card-premium rounded-3xl overflow-hidden">
            <div className="overflow-x-auto">
              <table className="min-w-full">
                <thead>
                  <tr className="bg-gradient-to-r from-teal-600 to-emerald-600 text-white">
                    <th className="px-6 py-4 text-left text-xs font-semibold uppercase tracking-wider sticky left-0 bg-teal-600">
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
                    {isLocked && (
                      <th className="px-6 py-4 text-center text-xs font-semibold uppercase tracking-wider">
                        Payslip
                      </th>
                    )}
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100">
                  {filteredLineItems && filteredLineItems.length > 0 ? (
                    filteredLineItems.map((item) => (
                      <tr 
                        key={item.employeeId} 
                        className="hover:bg-gradient-to-r hover:from-teal-50/50 hover:to-emerald-50/50 transition-colors duration-150"
                      >
                        <td className="px-6 py-4 whitespace-nowrap sticky left-0 bg-white hover:bg-gradient-to-r hover:from-teal-50/50 hover:to-emerald-50/50">
                          <div className="text-sm font-semibold text-[#1E3A5F]">{item.employeeName}</div>
                          <div className="text-xs text-[#64748B] font-mono">{item.employeeCode}</div>
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
                          <MoneyDisplay amount={item.bpjsKesehatan + item.bpjsKetenagakerjaan} className="text-sm text-red-600" />
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-right">
                          <MoneyDisplay amount={item.pph21} className="text-sm text-red-600" />
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-right">
                          <MoneyDisplay amount={item.takeHomePay} className="text-sm font-bold text-emerald-600" />
                        </td>
                        {isLocked && (
                          <td className="px-6 py-4 whitespace-nowrap text-center">
                            <button
                              onClick={() => handleDownloadPayslip(item.employeeCode, item.employeeName)}
                              className="inline-flex items-center gap-1 px-3 py-1.5 bg-gradient-to-r from-red-500 to-red-600 text-white rounded-lg text-xs font-semibold hover:shadow-lg hover:scale-105 transition-all duration-200"
                              title="Download Payslip PDF"
                            >
                              <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 10v6m0 0l-3-3m3 3l3-3m2 8H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                              </svg>
                              PDF
                            </button>
                          </td>
                        )}
                      </tr>
                    ))
                  ) : (
                    <tr>
                      <td colSpan={isLocked ? 9 : 8} className="px-6 py-12 text-center text-[#64748B]">
                        {searchQuery ? 'No employees found matching your search' : 'No line items available'}
                      </td>
                    </tr>
                  )}
                </tbody>
                {filteredLineItems && filteredLineItems.length > 0 && (
                  <tfoot>
                    <tr className="bg-gradient-to-r from-gray-50 to-gray-100 font-semibold">
                      <td className="px-6 py-4 text-sm text-[#1E3A5F]">
                        Total {searchQuery && `(${filteredLineItems.length} of ${lineItems?.length})`}
                      </td>
                      <td className="px-6 py-4 text-right">
                        <MoneyDisplay amount={totals.basicSalary} className="text-sm text-[#1E3A5F]" />
                      </td>
                      <td className="px-6 py-4 text-right">
                        <MoneyDisplay amount={totals.allowances} className="text-sm text-[#1E3A5F]" />
                      </td>
                      <td className="px-6 py-4 text-right">
                        <MoneyDisplay amount={totals.overtime} className="text-sm text-[#1E3A5F]" />
                      </td>
                      <td className="px-6 py-4 text-right">
                        <MoneyDisplay amount={totals.gross} className="text-sm font-bold text-[#1E3A5F]" />
                      </td>
                      <td className="px-6 py-4 text-right">
                        <MoneyDisplay amount={totals.bpjs} className="text-sm text-red-600" />
                      </td>
                      <td className="px-6 py-4 text-right">
                        <MoneyDisplay amount={totals.pph21} className="text-sm text-red-600" />
                      </td>
                      <td className="px-6 py-4 text-right">
                        <MoneyDisplay amount={totals.net} className="text-sm font-bold text-emerald-600" />
                      </td>
                    </tr>
                  </tfoot>
                )}
              </table>
            </div>
          </div>
        </div>
      )}

      {activeTab === 'summary' && (
        <div className="space-y-6">
          {/* Pie Charts */}
          <PayrollSummaryCharts totals={totals} />

          {/* Financial Summary & Approval Info */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div className="card-premium rounded-3xl p-6">
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
                  <MoneyDisplay amount={totals.gross} className="font-semibold text-[#1E3A5F]" />
                </div>
                <div className="flex justify-between items-center py-3 border-b border-gray-100">
                  <span className="text-sm text-[#64748B]">Total Deductions</span>
                  <MoneyDisplay amount={totals.deductions} className="font-semibold text-red-600" />
                </div>
                <div className="flex justify-between items-center py-3 bg-gradient-to-r from-teal-50 to-emerald-50 -mx-6 px-6 rounded-lg">
                  <span className="text-sm font-semibold text-[#1E3A5F]">Net Amount</span>
                  <MoneyDisplay amount={payrollRun.totalAmount} className="font-bold text-lg text-emerald-600" />
                </div>
              </div>
            </div>

            <div className="card-premium rounded-3xl p-6">
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
        </div>
      )}

      {activeTab === 'timeline' && (
        <PayrollTimeline payrollRunId={id} />
      )}

      {/* Action Dialog */}
      {actionDialog.action && (
        <PayrollActionDialog
          isOpen={actionDialog.isOpen}
          onClose={() => setActionDialog({ isOpen: false, action: null })}
          payrollRunId={id}
          action={actionDialog.action}
        />
      )}
    </div>
  );
}

// Made with Bob
