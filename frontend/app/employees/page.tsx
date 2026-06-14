'use client';

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useState, useMemo } from 'react';
import { employeeApi, type Employee } from '@/lib/api';
import { MoneyDisplay } from '@/components/MoneyDisplay';
import { EmployeeDialog } from '@/components/EmployeeDialog';

export default function EmployeesPage() {
  const [activeFilter, setActiveFilter] = useState<boolean | undefined>(true);
  const [searchQuery, setSearchQuery] = useState('');
  const [isDialogOpen, setIsDialogOpen] = useState(false);
  const [selectedEmployee, setSelectedEmployee] = useState<Employee | undefined>();
  const [dialogMode, setDialogMode] = useState<'create' | 'edit'>('create');
  const queryClient = useQueryClient();

  const { data: employeesData, isLoading } = useQuery({
    queryKey: ['employees', activeFilter],
    queryFn: () => employeeApi.getEmployees({ isActive: activeFilter, pageSize: 100 }),
  });

  // Filter employees by search query
  const filteredEmployees = useMemo(() => {
    if (!employeesData?.items) return [];
    if (!searchQuery) return employeesData.items;
    
    const query = searchQuery.toLowerCase();
    return employeesData.items.filter(emp => 
      emp.fullName.toLowerCase().includes(query) ||
      emp.employeeCode.toLowerCase().includes(query) ||
      emp.email.toLowerCase().includes(query)
    );
  }, [employeesData?.items, searchQuery]);

  const getBasicSalary = (employee: Employee) => {
    const basicComp = employee.salaryComponents.find(c => c.type === 'BasicSalary');
    return basicComp?.amount || 0;
  };

  const getTotalAllowances = (employee: Employee) => {
    return employee.salaryComponents
      .filter(c => c.type === 'FixedAllowance' || c.type === 'VariableAllowance')
      .reduce((sum, c) => sum + c.amount, 0);
  };

  if (isLoading) {
    return (
      <div className="min-h-screen bg-slate-50 p-8">
        <div className="max-w-7xl mx-auto">
          <div className="animate-pulse space-y-4">
            <div className="h-8 bg-slate-200 rounded w-1/4"></div>
            <div className="h-64 bg-slate-200 rounded"></div>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-slate-50">
      {/* Header */}
      <div className="bg-white border-b border-slate-200">
        <div className="max-w-7xl mx-auto px-8 py-6">
          <div className="flex items-center justify-between">
            <div>
              <h1 className="text-3xl font-bold text-slate-900 tracking-tight">
                Team Directory
              </h1>
              <p className="mt-1 text-sm text-slate-600">
                {filteredEmployees.length} {activeFilter ? 'active' : activeFilter === false ? 'inactive' : 'total'} employees
              </p>
            </div>
            <button
              onClick={() => {
                setDialogMode('create');
                setSelectedEmployee(undefined);
                setIsDialogOpen(true);
              }}
              className="px-6 py-3 bg-gradient-to-r from-teal-600 to-emerald-600 text-white rounded-lg font-medium hover:from-teal-700 hover:to-emerald-700 transition-all shadow-lg shadow-teal-500/30 hover:shadow-xl hover:shadow-teal-500/40"
            >
              <div className="flex items-center gap-2">
                <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
                </svg>
                Add Employee
              </div>
            </button>
          </div>

          {/* Filters */}
          <div className="mt-6 flex items-center gap-4">
            <div className="flex-1 relative">
              <input
                type="text"
                placeholder="Search by name, code, or email..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="w-full px-4 py-2.5 pl-11 bg-slate-50 border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-teal-500 focus:border-transparent transition-all"
              />
              <svg className="w-5 h-5 text-slate-400 absolute left-3.5 top-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
              </svg>
            </div>

            <div className="flex gap-2 bg-slate-100 p-1 rounded-lg">
              <button
                onClick={() => setActiveFilter(undefined)}
                className={`px-4 py-2 rounded-md text-sm font-medium transition-all ${
                  activeFilter === undefined
                    ? 'bg-white text-slate-900 shadow-sm'
                    : 'text-slate-600 hover:text-slate-900'
                }`}
              >
                All
              </button>
              <button
                onClick={() => setActiveFilter(true)}
                className={`px-4 py-2 rounded-md text-sm font-medium transition-all ${
                  activeFilter === true
                    ? 'bg-white text-slate-900 shadow-sm'
                    : 'text-slate-600 hover:text-slate-900'
                }`}
              >
                Active
              </button>
              <button
                onClick={() => setActiveFilter(false)}
                className={`px-4 py-2 rounded-md text-sm font-medium transition-all ${
                  activeFilter === false
                    ? 'bg-white text-slate-900 shadow-sm'
                    : 'text-slate-600 hover:text-slate-900'
                }`}
              >
                Inactive
              </button>
            </div>
          </div>
        </div>
      </div>

      {/* Employee Grid */}
      <div className="max-w-7xl mx-auto px-8 py-8">
        {filteredEmployees.length === 0 ? (
          <div className="text-center py-16">
            <div className="inline-flex items-center justify-center w-16 h-16 bg-slate-100 rounded-full mb-4">
              <svg className="w-8 h-8 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z" />
              </svg>
            </div>
            <h3 className="text-lg font-semibold text-slate-900 mb-1">No employees found</h3>
            <p className="text-slate-600">
              {searchQuery ? 'Try adjusting your search' : 'Get started by adding your first employee'}
            </p>
          </div>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {filteredEmployees.map((employee) => {
              const basicSalary = getBasicSalary(employee);
              const allowances = getTotalAllowances(employee);
              const totalComp = basicSalary + allowances;

              return (
                <div
                  key={employee.id}
                  className="group bg-white rounded-xl border border-slate-200 hover:border-teal-300 hover:shadow-xl hover:shadow-teal-500/10 transition-all duration-300 overflow-hidden"
                >
                  {/* Card Header */}
                  <div className="p-6 border-b border-slate-100">
                    <div className="flex items-start justify-between mb-3">
                      <div className="flex-1">
                        <div className="flex items-center gap-2 mb-1">
                          <h3 className="text-lg font-semibold text-slate-900 group-hover:text-teal-700 transition-colors">
                            {employee.fullName}
                          </h3>
                          {!employee.isActive && (
                            <span className="px-2 py-0.5 bg-slate-100 text-slate-600 text-xs font-medium rounded">
                              Inactive
                            </span>
                          )}
                        </div>
                        <p className="text-sm font-mono text-slate-500">{employee.employeeCode}</p>
                      </div>
                      <div className="w-10 h-10 bg-gradient-to-br from-teal-100 to-emerald-100 rounded-lg flex items-center justify-center flex-shrink-0">
                        <span className="text-lg font-bold text-teal-700">
                          {employee.fullName.charAt(0)}
                        </span>
                      </div>
                    </div>

                    <div className="space-y-1.5 text-sm">
                      <div className="flex items-center gap-2 text-slate-600">
                        <svg className="w-4 h-4 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
                        </svg>
                        <span className="truncate">{employee.email}</span>
                      </div>
                      <div className="flex items-center gap-2 text-slate-600">
                        <svg className="w-4 h-4 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
                        </svg>
                        <span>Joined {new Date(employee.joinDate).toLocaleDateString('id-ID', { year: 'numeric', month: 'short' })}</span>
                      </div>
                    </div>
                  </div>

                  {/* Salary Breakdown */}
                  <div className="p-6 bg-slate-50">
                    <div className="space-y-3">
                      <div className="flex items-center justify-between">
                        <span className="text-sm text-slate-600">Basic Salary</span>
                        <span className="text-sm font-semibold text-slate-900">
                          <MoneyDisplay amount={basicSalary} />
                        </span>
                      </div>
                      {allowances > 0 && (
                        <div className="flex items-center justify-between">
                          <span className="text-sm text-slate-600">Allowances</span>
                          <span className="text-sm font-semibold text-slate-900">
                            <MoneyDisplay amount={allowances} />
                          </span>
                        </div>
                      )}
                      <div className="pt-3 border-t border-slate-200 flex items-center justify-between">
                        <span className="text-sm font-medium text-slate-700">Total Package</span>
                        <span className="text-lg font-bold text-teal-700">
                          <MoneyDisplay amount={totalComp} />
                        </span>
                      </div>
                    </div>

                    {/* PTKP Status */}
                    <div className="mt-4 pt-4 border-t border-slate-200">
                      <div className="flex items-center justify-between text-xs">
                        <span className="text-slate-500">Tax Status</span>
                        <span className="px-2 py-1 bg-white border border-slate-200 rounded font-mono font-medium text-slate-700">
                          {employee.ptkpStatus}
                        </span>
                      </div>
                      {employee.npwp && (
                        <div className="mt-2 flex items-center gap-1.5 text-xs text-slate-500">
                          <svg className="w-3.5 h-3.5 text-teal-600" fill="currentColor" viewBox="0 0 20 20">
                            <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clipRule="evenodd" />
                          </svg>
                          <span>NPWP Registered</span>
                        </div>
                      )}
                    </div>
                  </div>

                  {/* Actions */}
                  <div className="px-6 py-4 bg-white border-t border-slate-100 flex gap-2">
                    <button
                      onClick={() => {
                        setSelectedEmployee(employee);
                        setDialogMode('edit');
                        setIsDialogOpen(true);
                      }}
                      className="flex-1 px-4 py-2 text-sm font-medium text-slate-700 hover:text-slate-900 hover:bg-slate-50 rounded-lg transition-colors"
                    >
                      View Details
                    </button>
                    <button
                      onClick={() => {
                        setSelectedEmployee(employee);
                        setDialogMode('edit');
                        setIsDialogOpen(true);
                      }}
                      className="px-4 py-2 text-sm font-medium text-teal-700 hover:text-teal-800 hover:bg-teal-50 rounded-lg transition-colors"
                    >
                      Edit
                    </button>
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </div>

      {/* Employee Dialog */}
      <EmployeeDialog
        isOpen={isDialogOpen}
        onClose={() => {
          setIsDialogOpen(false);
          setSelectedEmployee(undefined);
        }}
        employee={selectedEmployee}
        mode={dialogMode}
      />
    </div>
  );
}

// Made with Bob