import axios from 'axios';

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5044';

export const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Add request interceptor to include JWT token
api.interceptors.request.use(
  (config) => {
    // Get token from localStorage
    const token = typeof window !== 'undefined' ? localStorage.getItem('payroll_token') : null;
    
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Add response interceptor to handle 401 errors
api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      // Clear auth data and redirect to login
      if (typeof window !== 'undefined') {
        localStorage.removeItem('payroll_token');
        localStorage.removeItem('payroll_user');
        window.location.href = '/login';
      }
    }
    return Promise.reject(error);
  }
);

// Types
export interface PayrollRunSummary {
  id: string;
  month: number;
  year: number;
  status: string;
  totalEmployees: number;
  totalAmount: number;
  createdBy: string;
  createdAt: string;
  approvedBy?: string;
  approvedAt?: string;
  lockedBy?: string;
  lockedAt?: string;
  periodDisplay: string;
  totalAmountDisplay: string;
  statusDisplay: string;
}

export interface PayrollLineItem {
  id: string;
  payrollRunId: string;
  employeeId: string;
  employeeName: string;
  basicSalary: number;
  totalAllowances: number;
  totalOvertime: number;
  grossSalary: number;
  totalDeductions: number;
  totalBPJS: number;
  pph21: number;
  takeHomePay: number;
  isProrated: boolean;
  workingDays?: number;
  totalWorkingDays?: number;
  proratePercentage?: number;
  grossSalaryDisplay: string;
  takeHomePayDisplay: string;
  prorateDisplay?: string;
}

export interface PayrollRunsResponse {
  items: PayrollRunSummary[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

export interface PayrollRunDetailResponse {
  summary: PayrollRunSummary;
  lineItems: PayrollLineItem[];
}

export interface CreatePayrollRunRequest {
  month: number;
  year: number;
  createdBy: string;
}

export interface ApprovePayrollRequest {
  approvedBy: string;
  notes?: string;
}

export interface LockPayrollRequest {
  lockedBy: string;
}

export interface StartReviewRequest {
  reviewedBy: string;
}

export interface RejectPayrollRequest {
  rejectedBy: string;
  reason: string;
}

export interface InitiateDisbursementRequest {
  bankName: string;
}

export interface ConfirmDisbursementRequest {
  confirmedBy: string;
}

// API Functions
export const payrollApi = {
  // Get list of payroll runs
  getPayrollRuns: async (params?: {
    year?: number;
    month?: number;
    status?: string;
    pageNumber?: number;
    pageSize?: number;
  }): Promise<PayrollRunsResponse> => {
    const response = await api.get('/api/payroll', { params });
    return response.data;
  },

  // Get payroll run detail
  getPayrollRunDetail: async (id: string): Promise<PayrollRunDetailResponse> => {
    const response = await api.get(`/api/payroll/${id}`);
    return response.data;
  },

  // Create new payroll run
  createPayrollRun: async (data: CreatePayrollRunRequest): Promise<string> => {
    const response = await api.post('/api/payroll', data);
    return response.data;
  },

  // Approve payroll run
  approvePayrollRun: async (id: string, data: ApprovePayrollRequest): Promise<void> => {
    await api.post(`/api/payroll/${id}/approve`, data);
  },

  // Lock payroll run
  lockPayrollRun: async (id: string, data: LockPayrollRequest): Promise<void> => {
    await api.post(`/api/payroll/${id}/lock`, data);
  },

  // Start review payroll run
  startReviewPayrollRun: async (id: string, data: StartReviewRequest): Promise<void> => {
    await api.post(`/api/payroll/${id}/start-review`, data);
  },

  // Reject payroll run
  rejectPayrollRun: async (id: string, data: RejectPayrollRequest): Promise<void> => {
    await api.post(`/api/payroll/${id}/reject`, data);
  },

  // Initiate disbursement
  initiateDisbursement: async (id: string, data: InitiateDisbursementRequest): Promise<void> => {
    await api.post(`/api/payroll/${id}/initiate-disbursement`, data);
  },

  // Confirm disbursement
  confirmDisbursement: async (id: string, data: ConfirmDisbursementRequest): Promise<void> => {
    await api.post(`/api/payroll/${id}/confirm-disbursement`, data);
  },

  // Download payslip PDF for specific employee
  downloadPayslipPdf: async (payrollRunId: string, employeeId: string): Promise<Blob> => {
    const response = await api.get(`/api/reports/payroll/${payrollRunId}/payslip/${employeeId}/pdf`, {
      responseType: 'blob',
    });
    return response.data;
  },

  // Export payroll to Excel
  exportPayrollExcel: async (payrollRunId: string): Promise<Blob> => {
    const response = await api.get(`/api/reports/payroll/${payrollRunId}/export/excel`, {
      responseType: 'blob',
    });
    return response.data;
  },

  // Generate bank file
  generateBankFile: async (payrollRunId: string, bank: string, companyId?: string): Promise<Blob> => {
    const params = new URLSearchParams({ bank });
    if (companyId) params.append('companyId', companyId);
    
    const response = await api.get(`/api/reports/payroll/${payrollRunId}/bank-file?${params.toString()}`, {
      responseType: 'blob',
    });
    return response.data;
  },

  // Get payroll events (timeline)
  getPayrollEvents: async (payrollRunId: string): Promise<PayrollEvent[]> => {
    const response = await api.get(`/api/events/payroll/${payrollRunId}`);
    return response.data;
  },
};

// Event Types
export interface PayrollEvent {
  id: string;
  streamId: string;
  version: number;
  sequence: number;
  eventType: string;
  timestamp: string;
  data: any;
}

// Employee Types
export interface SalaryComponent {
  componentId: string;
  name: string;
  amount: number;
  type: string;
  effectiveFrom: string;
  effectiveTo?: string;
}

export interface Employee {
  id: string;
  employeeCode: string;
  fullName: string;
  email: string;
  npwp?: string;
  ptkpStatus: string;
  joinDate: string;
  resignDate?: string;
  isActive: boolean;
  salaryComponents: SalaryComponent[];
}

export interface EmployeesResponse {
  items: Employee[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface CreateEmployeeRequest {
  employeeCode: string;
  fullName: string;
  email: string;
  npwp?: string;
  ptkpStatus: string;
  joinDate: string;
  salaryComponents: Array<{
    name: string;
    amount: number;
    type: string;
    effectiveFrom: string;
  }>;
}

export interface UpdateEmployeeRequest {
  fullName: string;
  email: string;
  npwp?: string;
  ptkpStatus: string;
  salaryComponents: Array<{
    name: string;
    amount: number;
    type: string;
    effectiveFrom: string;
  }>;
}

export interface DeactivateEmployeeRequest {
  resignDate: string;
}

// Employee API Functions
export const employeeApi = {
  // Get list of employees
  getEmployees: async (params?: {
    isActive?: boolean;
    page?: number;
    pageSize?: number;
  }): Promise<EmployeesResponse> => {
    const response = await api.get('/api/employees', { params });
    return response.data;
  },

  // Get employee by ID
  getEmployeeById: async (id: string): Promise<Employee> => {
    const response = await api.get(`/api/employees/${id}`);
    return response.data;
  },

  // Create new employee
  createEmployee: async (data: CreateEmployeeRequest): Promise<{ id: string }> => {
    const response = await api.post('/api/employees', data);
    return response.data;
  },

  // Update employee
  updateEmployee: async (id: string, data: UpdateEmployeeRequest): Promise<void> => {
    await api.put(`/api/employees/${id}`, data);
  },

  // Deactivate employee (resign)
  deactivateEmployee: async (id: string, data: DeactivateEmployeeRequest): Promise<void> => {
    await api.post(`/api/employees/${id}/deactivate`, data);
  },
};

// Made with Bob
