'use client';

import { useState, useEffect } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { employeeApi, type Employee, type CreateEmployeeRequest, type UpdateEmployeeRequest } from '@/lib/api';

interface SalaryComponentInput {
  name: string;
  amount: string;
  type: string;
  effectiveFrom: string;
}

interface EmployeeDialogProps {
  isOpen: boolean;
  onClose: () => void;
  employee?: Employee;
  mode: 'create' | 'edit';
}

const PTKP_OPTIONS = [
  { value: 'TK/0', label: 'TK/0 - Single, No Dependents' },
  { value: 'TK/1', label: 'TK/1 - Single, 1 Dependent' },
  { value: 'K/0', label: 'K/0 - Married, No Dependents' },
  { value: 'K/1', label: 'K/1 - Married, 1 Dependent' },
  { value: 'K/2', label: 'K/2 - Married, 2 Dependents' },
  { value: 'K/3', label: 'K/3 - Married, 3 Dependents' },
];

const COMPONENT_TYPES = [
  { value: 'BasicSalary', label: 'Basic Salary' },
  { value: 'FixedAllowance', label: 'Fixed Allowance' },
  { value: 'VariableAllowance', label: 'Variable Allowance' },
  { value: 'Deduction', label: 'Deduction' },
];

export function EmployeeDialog({ isOpen, onClose, employee, mode }: EmployeeDialogProps) {
  const queryClient = useQueryClient();
  const [formData, setFormData] = useState({
    employeeCode: '',
    fullName: '',
    email: '',
    npwp: '',
    ptkpStatus: 'TK/0',
    joinDate: new Date().toISOString().split('T')[0],
  });

  const [salaryComponents, setSalaryComponents] = useState<SalaryComponentInput[]>([
    {
      name: 'Basic Salary',
      amount: '',
      type: 'BasicSalary',
      effectiveFrom: new Date().toISOString().split('T')[0],
    },
  ]);

  const [errors, setErrors] = useState<Record<string, string>>({});

  useEffect(() => {
    if (employee && mode === 'edit') {
      setFormData({
        employeeCode: employee.employeeCode,
        fullName: employee.fullName,
        email: employee.email,
        npwp: employee.npwp || '',
        ptkpStatus: employee.ptkpStatus,
        joinDate: employee.joinDate.split('T')[0],
      });
      setSalaryComponents(
        employee.salaryComponents.map(c => ({
          name: c.name,
          amount: c.amount.toString(),
          type: c.type,
          effectiveFrom: c.effectiveFrom.split('T')[0],
        }))
      );
    }
  }, [employee, mode]);

  const createMutation = useMutation({
    mutationFn: (data: CreateEmployeeRequest) => employeeApi.createEmployee(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['employees'] });
      onClose();
      resetForm();
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateEmployeeRequest }) =>
      employeeApi.updateEmployee(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['employees'] });
      onClose();
    },
  });

  const validate = () => {
    const newErrors: Record<string, string> = {};

    if (!formData.employeeCode.trim()) newErrors.employeeCode = 'Employee code is required';
    if (!formData.fullName.trim()) newErrors.fullName = 'Full name is required';
    if (!formData.email.trim()) newErrors.email = 'Email is required';
    else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(formData.email))
      newErrors.email = 'Invalid email format';

    const hasBasicSalary = salaryComponents.some(c => c.type === 'BasicSalary');
    if (!hasBasicSalary) newErrors.salaryComponents = 'Basic salary component is required';

    salaryComponents.forEach((comp, idx) => {
      if (!comp.name.trim()) newErrors[`comp_${idx}_name`] = 'Component name is required';
      if (!comp.amount || parseFloat(comp.amount) <= 0)
        newErrors[`comp_${idx}_amount`] = 'Amount must be greater than 0';
    });

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!validate()) return;

    const components = salaryComponents.map(c => ({
      name: c.name,
      amount: parseFloat(c.amount),
      type: c.type,
      effectiveFrom: c.effectiveFrom,
    }));

    if (mode === 'create') {
      createMutation.mutate({
        ...formData,
        npwp: formData.npwp || undefined,
        salaryComponents: components,
      });
    } else if (employee) {
      updateMutation.mutate({
        id: employee.id,
        data: {
          fullName: formData.fullName,
          email: formData.email,
          npwp: formData.npwp || undefined,
          ptkpStatus: formData.ptkpStatus,
          salaryComponents: components,
        },
      });
    }
  };

  const resetForm = () => {
    setFormData({
      employeeCode: '',
      fullName: '',
      email: '',
      npwp: '',
      ptkpStatus: 'TK/0',
      joinDate: new Date().toISOString().split('T')[0],
    });
    setSalaryComponents([
      {
        name: 'Basic Salary',
        amount: '',
        type: 'BasicSalary',
        effectiveFrom: new Date().toISOString().split('T')[0],
      },
    ]);
    setErrors({});
  };

  const addSalaryComponent = () => {
    setSalaryComponents([
      ...salaryComponents,
      {
        name: '',
        amount: '',
        type: 'FixedAllowance',
        effectiveFrom: new Date().toISOString().split('T')[0],
      },
    ]);
  };

  const removeSalaryComponent = (index: number) => {
    setSalaryComponents(salaryComponents.filter((_, i) => i !== index));
  };

  const updateSalaryComponent = (index: number, field: keyof SalaryComponentInput, value: string) => {
    const updated = [...salaryComponents];
    updated[index] = { ...updated[index], [field]: value };
    setSalaryComponents(updated);
  };

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 overflow-y-auto">
      <div className="flex min-h-screen items-center justify-center p-4">
        {/* Backdrop */}
        <div className="fixed inset-0 bg-slate-900/50 backdrop-blur-sm" onClick={onClose} />

        {/* Dialog */}
        <div className="relative bg-white rounded-2xl shadow-2xl w-full max-w-3xl max-h-[90vh] overflow-hidden flex flex-col">
          {/* Header */}
          <div className="px-8 py-6 border-b border-slate-200 bg-gradient-to-r from-teal-50 to-emerald-50">
            <div className="flex items-center justify-between">
              <div>
                <h2 className="text-2xl font-bold text-slate-900">
                  {mode === 'create' ? 'Add New Employee' : 'Edit Employee'}
                </h2>
                <p className="mt-1 text-sm text-slate-600">
                  {mode === 'create'
                    ? 'Fill in the details to add a new team member'
                    : 'Update employee information and salary components'}
                </p>
              </div>
              <button
                onClick={onClose}
                className="w-10 h-10 flex items-center justify-center rounded-lg hover:bg-white/50 transition-colors"
              >
                <svg className="w-6 h-6 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                </svg>
              </button>
            </div>
          </div>

          {/* Form */}
          <form onSubmit={handleSubmit} className="flex-1 overflow-y-auto">
            <div className="px-8 py-6 space-y-6">
              {/* Basic Information */}
              <div>
                <h3 className="text-lg font-semibold text-slate-900 mb-4">Basic Information</h3>
                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <label className="block text-sm font-medium text-slate-700 mb-2">
                      Employee Code <span className="text-red-500">*</span>
                    </label>
                    <input
                      type="text"
                      value={formData.employeeCode}
                      onChange={(e) => setFormData({ ...formData, employeeCode: e.target.value })}
                      disabled={mode === 'edit'}
                      className={`w-full px-4 py-2.5 border rounded-lg focus:outline-none focus:ring-2 focus:ring-teal-500 transition-all ${
                        errors.employeeCode ? 'border-red-300' : 'border-slate-200'
                      } ${mode === 'edit' ? 'bg-slate-50 text-slate-500' : ''}`}
                      placeholder="EMP-001"
                    />
                    {errors.employeeCode && (
                      <p className="mt-1 text-sm text-red-600">{errors.employeeCode}</p>
                    )}
                  </div>

                  <div>
                    <label className="block text-sm font-medium text-slate-700 mb-2">
                      Full Name <span className="text-red-500">*</span>
                    </label>
                    <input
                      type="text"
                      value={formData.fullName}
                      onChange={(e) => setFormData({ ...formData, fullName: e.target.value })}
                      className={`w-full px-4 py-2.5 border rounded-lg focus:outline-none focus:ring-2 focus:ring-teal-500 transition-all ${
                        errors.fullName ? 'border-red-300' : 'border-slate-200'
                      }`}
                      placeholder="John Doe"
                    />
                    {errors.fullName && <p className="mt-1 text-sm text-red-600">{errors.fullName}</p>}
                  </div>

                  <div>
                    <label className="block text-sm font-medium text-slate-700 mb-2">
                      Email <span className="text-red-500">*</span>
                    </label>
                    <input
                      type="email"
                      value={formData.email}
                      onChange={(e) => setFormData({ ...formData, email: e.target.value })}
                      className={`w-full px-4 py-2.5 border rounded-lg focus:outline-none focus:ring-2 focus:ring-teal-500 transition-all ${
                        errors.email ? 'border-red-300' : 'border-slate-200'
                      }`}
                      placeholder="john@company.com"
                    />
                    {errors.email && <p className="mt-1 text-sm text-red-600">{errors.email}</p>}
                  </div>

                  <div>
                    <label className="block text-sm font-medium text-slate-700 mb-2">NPWP</label>
                    <input
                      type="text"
                      value={formData.npwp}
                      onChange={(e) => setFormData({ ...formData, npwp: e.target.value })}
                      className="w-full px-4 py-2.5 border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-teal-500 transition-all"
                      placeholder="123456789012345"
                    />
                  </div>

                  <div>
                    <label className="block text-sm font-medium text-slate-700 mb-2">
                      PTKP Status <span className="text-red-500">*</span>
                    </label>
                    <select
                      value={formData.ptkpStatus}
                      onChange={(e) => setFormData({ ...formData, ptkpStatus: e.target.value })}
                      className="w-full px-4 py-2.5 border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-teal-500 transition-all"
                    >
                      {PTKP_OPTIONS.map((opt) => (
                        <option key={opt.value} value={opt.value}>
                          {opt.label}
                        </option>
                      ))}
                    </select>
                  </div>

                  <div>
                    <label className="block text-sm font-medium text-slate-700 mb-2">
                      Join Date <span className="text-red-500">*</span>
                    </label>
                    <input
                      type="date"
                      value={formData.joinDate}
                      onChange={(e) => setFormData({ ...formData, joinDate: e.target.value })}
                      disabled={mode === 'edit'}
                      className={`w-full px-4 py-2.5 border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-teal-500 transition-all ${
                        mode === 'edit' ? 'bg-slate-50 text-slate-500' : ''
                      }`}
                    />
                  </div>
                </div>
              </div>

              {/* Salary Components */}
              <div>
                <div className="flex items-center justify-between mb-4">
                  <h3 className="text-lg font-semibold text-slate-900">Salary Components</h3>
                  <button
                    type="button"
                    onClick={addSalaryComponent}
                    className="px-4 py-2 text-sm font-medium text-teal-700 hover:text-teal-800 hover:bg-teal-50 rounded-lg transition-colors"
                  >
                    + Add Component
                  </button>
                </div>

                {errors.salaryComponents && (
                  <p className="mb-4 text-sm text-red-600">{errors.salaryComponents}</p>
                )}

                <div className="space-y-4">
                  {salaryComponents.map((comp, idx) => (
                    <div key={idx} className="p-4 bg-slate-50 rounded-lg border border-slate-200">
                      <div className="grid grid-cols-12 gap-4">
                        <div className="col-span-4">
                          <label className="block text-xs font-medium text-slate-600 mb-1">Name</label>
                          <input
                            type="text"
                            value={comp.name}
                            onChange={(e) => updateSalaryComponent(idx, 'name', e.target.value)}
                            className={`w-full px-3 py-2 text-sm border rounded-lg focus:outline-none focus:ring-2 focus:ring-teal-500 ${
                              errors[`comp_${idx}_name`] ? 'border-red-300' : 'border-slate-200'
                            }`}
                            placeholder="Component name"
                          />
                        </div>

                        <div className="col-span-3">
                          <label className="block text-xs font-medium text-slate-600 mb-1">Type</label>
                          <select
                            value={comp.type}
                            onChange={(e) => updateSalaryComponent(idx, 'type', e.target.value)}
                            className="w-full px-3 py-2 text-sm border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-teal-500"
                          >
                            {COMPONENT_TYPES.map((type) => (
                              <option key={type.value} value={type.value}>
                                {type.label}
                              </option>
                            ))}
                          </select>
                        </div>

                        <div className="col-span-3">
                          <label className="block text-xs font-medium text-slate-600 mb-1">Amount</label>
                          <input
                            type="number"
                            value={comp.amount}
                            onChange={(e) => updateSalaryComponent(idx, 'amount', e.target.value)}
                            className={`w-full px-3 py-2 text-sm border rounded-lg focus:outline-none focus:ring-2 focus:ring-teal-500 ${
                              errors[`comp_${idx}_amount`] ? 'border-red-300' : 'border-slate-200'
                            }`}
                            placeholder="0"
                          />
                        </div>

                        <div className="col-span-2 flex items-end">
                          {salaryComponents.length > 1 && (
                            <button
                              type="button"
                              onClick={() => removeSalaryComponent(idx)}
                              className="w-full px-3 py-2 text-sm text-red-600 hover:bg-red-50 rounded-lg transition-colors"
                            >
                              Remove
                            </button>
                          )}
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            </div>

            {/* Footer */}
            <div className="px-8 py-6 border-t border-slate-200 bg-slate-50 flex items-center justify-end gap-3">
              <button
                type="button"
                onClick={onClose}
                className="px-6 py-2.5 text-sm font-medium text-slate-700 hover:text-slate-900 hover:bg-white border border-slate-200 rounded-lg transition-colors"
              >
                Cancel
              </button>
              <button
                type="submit"
                disabled={createMutation.isPending || updateMutation.isPending}
                className="px-6 py-2.5 text-sm font-medium text-white bg-gradient-to-r from-teal-600 to-emerald-600 hover:from-teal-700 hover:to-emerald-700 rounded-lg transition-all shadow-lg shadow-teal-500/30 disabled:opacity-50 disabled:cursor-not-allowed"
              >
                {createMutation.isPending || updateMutation.isPending
                  ? 'Saving...'
                  : mode === 'create'
                  ? 'Create Employee'
                  : 'Save Changes'}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
}

// Made with Bob