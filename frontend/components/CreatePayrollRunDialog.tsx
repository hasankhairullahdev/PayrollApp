'use client';

import { useState } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/lib/api';

interface CreatePayrollRunDialogProps {
  isOpen: boolean;
  onClose: () => void;
}

export function CreatePayrollRunDialog({ isOpen, onClose }: CreatePayrollRunDialogProps) {
  const queryClient = useQueryClient();
  const [month, setMonth] = useState(new Date().getMonth() + 1);
  const [year, setYear] = useState(new Date().getFullYear());
  const [error, setError] = useState<string | null>(null);

  const createMutation = useMutation({
    mutationFn: async () => {
      const response = await api.post('/api/payroll', {
        month,
        year,
        createdBy: 'current-user', // TODO: Get from auth context
      });
      return response.data;
    },
    onSuccess: () => {
      // Invalidate all payroll-runs queries (with any filter)
      queryClient.invalidateQueries({
        queryKey: ['payroll-runs'],
        exact: false
      });
      onClose();
      setError(null);
    },
    onError: (err: any) => {
      setError(err.response?.data?.message || 'Failed to create payroll run');
    },
  });

  if (!isOpen) return null;

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    createMutation.mutate();
  };

  const currentYear = new Date().getFullYear();
  const years = Array.from({ length: 3 }, (_, i) => currentYear - 1 + i);

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <div className="bg-white rounded-lg shadow-xl max-w-md w-full mx-4">
        <div className="p-6">
          <h3 className="text-lg font-semibold text-gray-900 mb-4">
            Create Payroll Run
          </h3>

          <form onSubmit={handleSubmit}>
            <div className="space-y-4">
              <div>
                <label htmlFor="month" className="block text-sm font-medium text-gray-700 mb-1">
                  Month
                </label>
                <select
                  id="month"
                  value={month}
                  onChange={(e) => setMonth(Number(e.target.value))}
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                  required
                >
                  <option value={1}>Januari</option>
                  <option value={2}>Februari</option>
                  <option value={3}>Maret</option>
                  <option value={4}>April</option>
                  <option value={5}>Mei</option>
                  <option value={6}>Juni</option>
                  <option value={7}>Juli</option>
                  <option value={8}>Agustus</option>
                  <option value={9}>September</option>
                  <option value={10}>Oktober</option>
                  <option value={11}>November</option>
                  <option value={12}>Desember</option>
                </select>
              </div>

              <div>
                <label htmlFor="year" className="block text-sm font-medium text-gray-700 mb-1">
                  Year
                </label>
                <select
                  id="year"
                  value={year}
                  onChange={(e) => setYear(Number(e.target.value))}
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                  required
                >
                  {years.map((y) => (
                    <option key={y} value={y}>
                      {y}
                    </option>
                  ))}
                </select>
              </div>

              {error && (
                <div className="p-3 bg-red-50 border border-red-200 rounded-md">
                  <p className="text-sm text-red-600">{error}</p>
                </div>
              )}
            </div>

            <div className="flex gap-3 justify-end mt-6">
              <button
                type="button"
                onClick={onClose}
                disabled={createMutation.isPending}
                className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
              >
                Cancel
              </button>
              <button
                type="submit"
                disabled={createMutation.isPending}
                className="px-4 py-2 text-sm font-medium text-white bg-blue-600 rounded-md hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed"
              >
                {createMutation.isPending ? 'Creating...' : 'Create'}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
}

// Made with Bob