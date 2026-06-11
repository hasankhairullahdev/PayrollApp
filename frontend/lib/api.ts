import axios from 'axios';

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5044';

export const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

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
};

// Made with Bob
