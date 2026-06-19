'use client';

import { useMemo, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { MoneyDisplay } from '@/components/MoneyDisplay';
import { PayrollPeriodDisplay } from '@/components/PayrollPeriodDisplay';
import { StatusBadge } from '@/components/StatusBadge';
import { payrollApi, type PayrollRunSummary } from '@/lib/api';

function downloadBlob(blob: Blob, fileName: string) {
  const url = window.URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = fileName;
  document.body.appendChild(link);
  link.click();
  link.remove();
  window.URL.revokeObjectURL(url);
}

export default function ReportsPage() {
  const [selectedBank, setSelectedBank] = useState('bca');
  const [downloadingKey, setDownloadingKey] = useState<string | null>(null);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  const { data, isLoading, error: queryError } = useQuery({
    queryKey: ['reports-payroll-runs'],
    queryFn: () => payrollApi.getPayrollRuns({ pageSize: 100 }),
    staleTime: 5 * 60 * 1000,
  });

  const reportRuns = useMemo(() => {
    return (data?.items ?? [])
      .filter((run) => ['Approved', 'Locked'].includes(run.status))
      .sort((a, b) => {
        if (a.year !== b.year) return b.year - a.year;
        return b.month - a.month;
      });
  }, [data]);

  const handleDownloadExcel = async (run: PayrollRunSummary) => {
    const downloadKey = `${run.id}-excel`;
    setDownloadingKey(downloadKey);
    setError('');
    setSuccess('');

    try {
      const blob = await payrollApi.exportPayrollExcel(run.id);
      downloadBlob(blob, `Payroll_${run.month.toString().padStart(2, '0')}_${run.year}.xlsx`);
      setSuccess(`Excel export for ${run.periodDisplay} downloaded successfully`);
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to download Excel report');
    } finally {
      setDownloadingKey(null);
    }
  };

  const handleDownloadBankFile = async (run: PayrollRunSummary) => {
    const downloadKey = `${run.id}-bank`;
    setDownloadingKey(downloadKey);
    setError('');
    setSuccess('');

    try {
      const blob = await payrollApi.generateBankFile(run.id, selectedBank);
      const extension = selectedBank === 'mandiri' ? 'csv' : 'txt';
      downloadBlob(blob, `${selectedBank.toUpperCase()}_Payroll_${run.month.toString().padStart(2, '0')}_${run.year}.${extension}`);
      setSuccess(`Bank file for ${run.periodDisplay} downloaded successfully`);
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to download bank file');
    } finally {
      setDownloadingKey(null);
    }
  };

  if (isLoading) {
    return (
      <div className="p-6 md:p-8 max-w-7xl mx-auto">
        <div className="mb-8">
          <div className="h-10 w-64 bg-gray-200 rounded-2xl animate-pulse mb-3" />
          <div className="h-5 w-96 bg-gray-200 rounded-xl animate-pulse" />
        </div>
        <div className="space-y-4">
          {[1, 2, 3].map((item) => (
            <div key={item} className="bg-white rounded-3xl border border-gray-200 p-6 animate-pulse">
              <div className="h-24 bg-gray-100 rounded-2xl" />
            </div>
          ))}
        </div>
      </div>
    );
  }

  if (queryError) {
    return (
      <div className="p-6 md:p-8 max-w-7xl mx-auto">
        <div className="bg-red-50 border border-red-200 rounded-2xl p-6 text-red-700">
          Failed to load reports data.
        </div>
      </div>
    );
  }

  return (
    <div className="p-6 md:p-8 max-w-7xl mx-auto space-y-8">
      <div className="flex flex-col lg:flex-row lg:items-end lg:justify-between gap-6">
        <div>
          <h1 className="text-4xl font-bold tracking-tight bg-gradient-to-r from-teal-600 to-emerald-600 bg-clip-text text-transparent">
            Reports & Exports
          </h1>
          <p className="text-[#64748B] text-base md:text-lg mt-2">
            Download payroll exports for approved and locked payroll runs.
          </p>
        </div>

        <div className="bg-white rounded-2xl border border-gray-200 p-4 flex items-center gap-3">
          <label htmlFor="bank" className="text-sm font-semibold text-[#1E3A5F]">
            Default Bank File
          </label>
          <select
            id="bank"
            value={selectedBank}
            onChange={(e) => setSelectedBank(e.target.value)}
            className="px-4 py-2 rounded-xl border border-gray-200 text-sm font-medium text-[#1E3A5F] focus:outline-none focus:ring-2 focus:ring-teal-500"
          >
            <option value="bca">BCA</option>
            <option value="mandiri">Mandiri</option>
            <option value="bni">BNI</option>
            <option value="permata">Permata</option>
          </select>
        </div>
      </div>

      {error && (
        <div className="rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-red-700">
          {error}
        </div>
      )}

      {success && (
        <div className="rounded-2xl border border-green-200 bg-green-50 px-4 py-3 text-green-700">
          {success}
        </div>
      )}

      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        <div className="bg-white rounded-3xl border border-gray-200 p-6">
          <p className="text-sm font-semibold text-[#64748B] mb-2">Available Report Runs</p>
          <p className="text-3xl font-bold text-[#1E3A5F]">{reportRuns.length}</p>
        </div>
        <div className="bg-white rounded-3xl border border-gray-200 p-6">
          <p className="text-sm font-semibold text-[#64748B] mb-2">Locked Payrolls</p>
          <p className="text-3xl font-bold text-[#1E3A5F]">
            {reportRuns.filter((run) => run.status === 'Locked').length}
          </p>
        </div>
        <div className="bg-white rounded-3xl border border-gray-200 p-6">
          <p className="text-sm font-semibold text-[#64748B] mb-2">Total Export Amount</p>
          <MoneyDisplay
            amount={reportRuns.reduce((total, run) => total + run.totalAmount, 0)}
            className="text-3xl font-bold text-[#1E3A5F]"
          />
        </div>
      </div>

      <div className="bg-white rounded-3xl border border-gray-200 overflow-hidden">
        <div className="px-6 py-5 border-b border-gray-200">
          <h2 className="text-xl font-semibold text-[#1E3A5F]">Payroll Export Center</h2>
          <p className="text-sm text-[#64748B] mt-1">
            Excel export is available for approved and locked payrolls. Bank files require payroll status Locked.
          </p>
        </div>

        {reportRuns.length === 0 ? (
          <div className="p-6 text-[#64748B]">
            No approved or locked payroll runs are available for export yet.
          </div>
        ) : (
          <div className="divide-y divide-gray-200">
            {reportRuns.map((run) => {
              const excelKey = `${run.id}-excel`;
              const bankKey = `${run.id}-bank`;
              const canDownloadBankFile = run.status === 'Locked';

              return (
                <div key={run.id} className="p-6 flex flex-col xl:flex-row xl:items-center xl:justify-between gap-6">
                  <div className="space-y-3">
                    <div className="flex flex-wrap items-center gap-3">
                      <PayrollPeriodDisplay month={run.month} year={run.year} />
                      <StatusBadge status={run.status} />
                    </div>
                    <div className="flex flex-wrap gap-6 text-sm text-[#64748B]">
                      <span>Employees: <strong className="text-[#1E3A5F]">{run.totalEmployees}</strong></span>
                      <span>Total: <MoneyDisplay amount={run.totalAmount} /></span>
                      <span>Created by: <strong className="text-[#1E3A5F]">{run.createdBy}</strong></span>
                    </div>
                  </div>

                  <div className="flex flex-wrap gap-3">
                    <button
                      onClick={() => handleDownloadExcel(run)}
                      disabled={downloadingKey !== null}
                      className="px-4 py-2.5 rounded-xl bg-teal-600 text-white font-medium hover:bg-teal-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                    >
                      {downloadingKey === excelKey ? 'Downloading Excel...' : 'Download Excel'}
                    </button>
                    <button
                      onClick={() => handleDownloadBankFile(run)}
                      disabled={!canDownloadBankFile || downloadingKey !== null}
                      className="px-4 py-2.5 rounded-xl border border-gray-200 bg-white text-[#1E3A5F] font-medium hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                    >
                      {downloadingKey === bankKey ? 'Downloading Bank File...' : `Download ${selectedBank.toUpperCase()} File`}
                    </button>
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </div>
    </div>
  );
}
