'use client';

import { useState } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { payrollApi } from '@/lib/api';

interface PayrollActionDialogProps {
  isOpen: boolean;
  onClose: () => void;
  payrollRunId: string;
  action: 'start-review' | 'approve' | 'reject' | 'lock';
  onSuccess?: () => void;
}

export function PayrollActionDialog({
  isOpen,
  onClose,
  payrollRunId,
  action,
  onSuccess,
}: PayrollActionDialogProps) {
  const [notes, setNotes] = useState('');
  const [reason, setReason] = useState('');
  const queryClient = useQueryClient();

  const mutation = useMutation({
    mutationFn: async () => {
      const user = 'HR Admin'; // TODO: Get from auth context
      
      switch (action) {
        case 'start-review':
          await payrollApi.startReviewPayrollRun(payrollRunId, { reviewedBy: user });
          break;
        case 'approve':
          await payrollApi.approvePayrollRun(payrollRunId, { approvedBy: user, notes: notes || undefined });
          break;
        case 'reject':
          if (!reason.trim()) throw new Error('Reason is required');
          await payrollApi.rejectPayrollRun(payrollRunId, { rejectedBy: user, reason });
          break;
        case 'lock':
          await payrollApi.lockPayrollRun(payrollRunId, { lockedBy: user });
          break;
      }
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['payroll-run', payrollRunId] });
      queryClient.invalidateQueries({ queryKey: ['payroll-runs'] });
      onSuccess?.();
      handleClose();
    },
  });

  const handleClose = () => {
    setNotes('');
    setReason('');
    onClose();
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    mutation.mutate();
  };

  if (!isOpen) return null;

  const getDialogConfig = () => {
    switch (action) {
      case 'start-review':
        return {
          title: 'Mulai Review',
          description: 'Apakah Anda yakin ingin memulai proses review untuk payroll run ini?',
          confirmText: 'Mulai Review',
          confirmColor: 'from-amber-500 to-amber-600',
          showInput: false,
        };
      case 'approve':
        return {
          title: 'Approve Payroll',
          description: 'Setelah di-approve, payroll run dapat di-lock dan payslip akan digenerate.',
          confirmText: 'Approve',
          confirmColor: 'from-emerald-500 to-emerald-600',
          showInput: true,
          inputLabel: 'Catatan (opsional)',
          inputPlaceholder: 'Tambahkan catatan approval...',
        };
      case 'reject':
        return {
          title: 'Reject Payroll',
          description: 'Payroll run akan dikembalikan ke status Draft. Alasan penolakan wajib diisi.',
          confirmText: 'Reject',
          confirmColor: 'from-red-500 to-red-600',
          showInput: true,
          inputLabel: 'Alasan Penolakan *',
          inputPlaceholder: 'Jelaskan alasan penolakan...',
          required: true,
        };
      case 'lock':
        return {
          title: 'Lock Payroll',
          description: 'Setelah di-lock, payroll tidak dapat diubah lagi dan payslip akan digenerate otomatis.',
          confirmText: 'Lock & Generate Payslip',
          confirmColor: 'from-purple-500 to-purple-600',
          showInput: false,
        };
    }
  };

  const config = getDialogConfig();

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 animate-fade-in">
      {/* Backdrop */}
      <div 
        className="absolute inset-0 bg-black/50 backdrop-blur-sm"
        onClick={handleClose}
      />
      
      {/* Dialog */}
      <div className="relative bg-white rounded-3xl shadow-2xl max-w-md w-full p-6 animate-scale-in">
        {/* Header */}
        <div className="mb-6">
          <h3 className="text-2xl font-bold gradient-text-navy mb-2">
            {config.title}
          </h3>
          <p className="text-[#64748B]">
            {config.description}
          </p>
        </div>

        {/* Form */}
        <form onSubmit={handleSubmit}>
          {config.showInput && (
            <div className="mb-6">
              <label className="block text-sm font-semibold text-[#1E3A5F] mb-2">
                {config.inputLabel}
              </label>
              <textarea
                value={action === 'reject' ? reason : notes}
                onChange={(e) => action === 'reject' ? setReason(e.target.value) : setNotes(e.target.value)}
                placeholder={config.inputPlaceholder}
                required={config.required}
                rows={4}
                className="w-full px-4 py-3 border-2 border-gray-200 rounded-xl focus:outline-none focus:border-teal-500 focus:ring-4 focus:ring-teal-500/10 transition-all resize-none"
              />
            </div>
          )}

          {/* Error Message */}
          {mutation.isError && (
            <div className="mb-4 p-4 bg-red-50 border-2 border-red-200 rounded-xl">
              <p className="text-sm text-red-600 font-medium">
                {mutation.error instanceof Error ? mutation.error.message : 'Terjadi kesalahan'}
              </p>
            </div>
          )}

          {/* Actions */}
          <div className="flex gap-3">
            <button
              type="button"
              onClick={handleClose}
              disabled={mutation.isPending}
              className="flex-1 px-6 py-3 border-2 border-gray-200 text-[#64748B] rounded-xl font-semibold hover:border-gray-300 hover:bg-gray-50 transition-all disabled:opacity-50"
            >
              Batal
            </button>
            <button
              type="submit"
              disabled={mutation.isPending || (config.required && !reason.trim())}
              className={`flex-1 px-6 py-3 bg-gradient-to-r ${config.confirmColor} text-white rounded-xl font-semibold shadow-lg hover:shadow-xl hover:scale-105 transition-all disabled:opacity-50 disabled:hover:scale-100`}
            >
              {mutation.isPending ? (
                <span className="flex items-center justify-center gap-2">
                  <svg className="animate-spin h-5 w-5" fill="none" viewBox="0 0 24 24">
                    <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                    <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
                  </svg>
                  Processing...
                </span>
              ) : (
                config.confirmText
              )}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

// Made with Bob