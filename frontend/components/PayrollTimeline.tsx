"use client";

import { useQuery } from "@tanstack/react-query";
import { payrollApi, PayrollEvent } from "@/lib/api";
import { Card } from "@/components/ui/card";
import { 
  Clock, 
  CheckCircle2, 
  XCircle, 
  PlayCircle, 
  Lock, 
  FileText,
  Calculator,
  Eye,
  AlertCircle
} from "lucide-react";

interface PayrollTimelineProps {
  payrollRunId: string;
}

const eventConfig: Record<string, { 
  icon: React.ComponentType<{ className?: string }>;
  color: string;
  bgColor: string;
  title: string;
}> = {
  PayrollRunCreated: {
    icon: PlayCircle,
    color: "text-blue-600",
    bgColor: "bg-blue-50",
    title: "Payroll Run Created"
  },
  PayrollCalculationStarted: {
    icon: Calculator,
    color: "text-purple-600",
    bgColor: "bg-purple-50",
    title: "Calculation Started"
  },
  PayrollCalculated: {
    icon: CheckCircle2,
    color: "text-green-600",
    bgColor: "bg-green-50",
    title: "Calculation Completed"
  },
  PayrollReviewStarted: {
    icon: Eye,
    color: "text-amber-600",
    bgColor: "bg-amber-50",
    title: "Review Started"
  },
  PayrollApproved: {
    icon: CheckCircle2,
    color: "text-emerald-600",
    bgColor: "bg-emerald-50",
    title: "Approved"
  },
  PayrollRejected: {
    icon: XCircle,
    color: "text-red-600",
    bgColor: "bg-red-50",
    title: "Rejected"
  },
  PayrollLocked: {
    icon: Lock,
    color: "text-indigo-600",
    bgColor: "bg-indigo-50",
    title: "Locked"
  },
  PayslipGenerated: {
    icon: FileText,
    color: "text-teal-600",
    bgColor: "bg-teal-50",
    title: "Payslip Generated"
  },
  DisbursementInitiated: {
    icon: AlertCircle,
    color: "text-orange-600",
    bgColor: "bg-orange-50",
    title: "Disbursement Initiated"
  },
  DisbursementConfirmed: {
    icon: CheckCircle2,
    color: "text-green-600",
    bgColor: "bg-green-50",
    title: "Disbursement Confirmed"
  }
};

function formatEventData(eventType: string, data: any): string[] {
  const details: string[] = [];

  switch (eventType) {
    case "PayrollRunCreated":
      if (data.createdBy) details.push(`Created by: ${data.createdBy}`);
      if (data.month && data.year) details.push(`Period: ${data.month}/${data.year}`);
      break;
    case "PayrollCalculated":
      if (data.totalEmployees) details.push(`Employees: ${data.totalEmployees}`);
      if (data.totalAmount) details.push(`Total: Rp ${data.totalAmount.toLocaleString()}`);
      break;
    case "PayrollReviewStarted":
      if (data.reviewedBy) details.push(`Reviewed by: ${data.reviewedBy}`);
      break;
    case "PayrollApproved":
      if (data.approvedBy) details.push(`Approved by: ${data.approvedBy}`);
      if (data.notes) details.push(`Notes: ${data.notes}`);
      break;
    case "PayrollRejected":
      if (data.rejectedBy) details.push(`Rejected by: ${data.rejectedBy}`);
      if (data.reason) details.push(`Reason: ${data.reason}`);
      break;
    case "PayrollLocked":
      if (data.lockedBy) details.push(`Locked by: ${data.lockedBy}`);
      break;
    case "PayslipGenerated":
      if (data.employeeId) details.push(`Employee: ${data.employeeId}`);
      break;
  }

  return details;
}

export function PayrollTimeline({ payrollRunId }: PayrollTimelineProps) {
  const { data: events, isLoading, error } = useQuery({
    queryKey: ["payroll-events", payrollRunId],
    queryFn: () => payrollApi.getPayrollEvents(payrollRunId),
    staleTime: 30 * 1000, // 30 seconds
  });

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-12">
        <div className="flex items-center gap-2 text-gray-500">
          <Clock className="h-5 w-5 animate-spin" />
          <span>Loading timeline...</span>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <Card className="p-6">
        <div className="flex items-center gap-2 text-red-600">
          <XCircle className="h-5 w-5" />
          <span>Failed to load timeline</span>
        </div>
      </Card>
    );
  }

  if (!events || events.length === 0) {
    return (
      <Card className="p-6">
        <div className="flex items-center gap-2 text-gray-500">
          <AlertCircle className="h-5 w-5" />
          <span>No events found</span>
        </div>
      </Card>
    );
  }

  return (
    <div className="space-y-4">
      {events.map((event, index) => {
        const config = eventConfig[event.eventType] || {
          icon: Clock,
          color: "text-gray-600",
          bgColor: "bg-gray-50",
          title: event.eventType
        };
        const Icon = config.icon;
        const details = formatEventData(event.eventType, event.data);
        const isLast = index === events.length - 1;

        return (
          <div key={event.id} className="relative">
            {/* Timeline line */}
            {!isLast && (
              <div className="absolute left-6 top-12 bottom-0 w-0.5 bg-gray-200" />
            )}

            <Card className="p-4 hover:shadow-md transition-shadow">
              <div className="flex gap-4">
                {/* Icon */}
                <div className={`flex-shrink-0 w-12 h-12 rounded-full ${config.bgColor} flex items-center justify-center relative z-10`}>
                  <Icon className={`h-6 w-6 ${config.color}`} />
                </div>

                {/* Content */}
                <div className="flex-1 min-w-0">
                  <div className="flex items-start justify-between gap-4">
                    <div className="flex-1">
                      <h3 className="font-semibold text-gray-900">
                        {config.title}
                      </h3>
                      {details.length > 0 && (
                        <ul className="mt-1 space-y-0.5">
                          {details.map((detail, i) => (
                            <li key={i} className="text-sm text-gray-600">
                              {detail}
                            </li>
                          ))}
                        </ul>
                      )}
                    </div>
                    <div className="flex-shrink-0 text-right">
                      <time className="text-sm text-gray-500">
                        {new Date(event.timestamp).toLocaleString("id-ID", {
                          day: "2-digit",
                          month: "short",
                          year: "numeric",
                          hour: "2-digit",
                          minute: "2-digit"
                        })}
                      </time>
                      <div className="text-xs text-gray-400 mt-0.5">
                        v{event.version}
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </Card>
          </div>
        );
      })}
    </div>
  );
}

// Made with Bob
