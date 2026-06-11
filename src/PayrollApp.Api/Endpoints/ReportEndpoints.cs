using MediatR;
using Microsoft.AspNetCore.Mvc;
using Marten;
using PayrollApp.Infrastructure.Documents;
using PayrollApp.Infrastructure.Export;
using PayrollApp.Infrastructure.ReadModels;
using QuestPDF.Fluent;

namespace PayrollApp.Api.Endpoints;

public static class ReportEndpoints
{
    public static IEndpointRouteBuilder MapReportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports");

        group.MapGet("/payroll/{id}/payslip/{employeeId}/pdf", GetPayslipPdf)
            .WithName("GetPayslipPdf")
            .Produces<FileResult>(200)
            .Produces(404);

        group.MapGet("/payroll/{id}/export/excel", ExportPayrollExcel)
            .WithName("ExportPayrollExcel")
            .Produces<FileResult>(200)
            .Produces(404);

        group.MapGet("/payroll/{id}/bank-file", GenerateBankFile)
            .WithName("GenerateBankFile")
            .Produces<FileResult>(200)
            .Produces(404);

        return app;
    }

    private static async Task<IResult> GetPayslipPdf(
        Guid id,
        string employeeId,
        IDocumentSession session,
        CancellationToken ct)
    {
        // Get payroll run summary
        var payrollRun = await session.Query<PayrollRunSummary>()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (payrollRun == null)
            return Results.NotFound(new { message = "Payroll run not found" });

        // Get line item for specific employee
        var lineItem = await session.Query<PayrollLineItem>()
            .FirstOrDefaultAsync(x => x.PayrollRunId == id && x.EmployeeCode == employeeId, ct);

        if (lineItem == null)
            return Results.NotFound(new { message = "Employee not found in this payroll run" });

        // Create payslip data
        var payslipData = new PayslipData(
            EmployeeId: lineItem.EmployeeCode,
            EmployeeName: lineItem.EmployeeName,
            Position: "Staff", // TODO: Get from employee aggregate
            Department: "General", // TODO: Get from employee aggregate
            Period: $"{GetMonthName(payrollRun.Month)} {payrollRun.Year}",
            BasicSalary: lineItem.BasicSalary,
            Allowances: lineItem.Allowances,
            Overtime: lineItem.Overtime,
            GrossSalary: lineItem.GrossSalary,
            BpjsKesehatan: lineItem.BpjsKesehatan,
            BpjsTk: lineItem.BpjsKetenagakerjaan,
            Pph21: lineItem.Pph21,
            TotalDeductions: lineItem.Deductions,
            NetSalary: lineItem.TakeHomePay
        );

        // Generate PDF
        var document = new PayslipDocument(payslipData);
        var pdfBytes = document.GeneratePdf();

        var fileName = $"Payslip_{lineItem.EmployeeName.Replace(" ", "_")}_{payrollRun.Month:D2}_{payrollRun.Year}.pdf";

        return Results.File(pdfBytes, "application/pdf", fileName);
    }

    private static async Task<IResult> ExportPayrollExcel(
        Guid id,
        IDocumentSession session,
        CancellationToken ct)
    {
        // Get payroll run summary
        var payrollRun = await session.Query<PayrollRunSummary>()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (payrollRun == null)
            return Results.NotFound(new { message = "Payroll run not found" });

        // Get all line items
        var lineItems = await session.Query<PayrollLineItem>()
            .Where(x => x.PayrollRunId == id)
            .ToListAsync(ct);

        if (lineItems.Count == 0)
            return Results.NotFound(new { message = "No line items found" });

        // Calculate totals
        var totalGross = lineItems.Sum(x => x.GrossSalary);
        var totalDeductions = lineItems.Sum(x => x.Deductions);
        var totalNet = lineItems.Sum(x => x.TakeHomePay);

        // Generate Excel
        var exporter = new PayrollExcelExporter();
        var period = $"{GetMonthName(payrollRun.Month)} {payrollRun.Year}";
        var excelBytes = exporter.ExportPayrollRun(period, lineItems.ToList(), totalGross, totalDeductions, totalNet);

        var fileName = $"Payroll_{payrollRun.Month:D2}_{payrollRun.Year}.xlsx";

        return Results.File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    private static async Task<IResult> GenerateBankFile(
        Guid id,
        [FromQuery] string bank,
        [FromQuery] string? companyId,
        IDocumentSession session,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(bank))
            return Results.BadRequest(new { message = "Bank parameter is required" });

        // Get payroll run summary
        var payrollRun = await session.Query<PayrollRunSummary>()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (payrollRun == null)
            return Results.NotFound(new { message = "Payroll run not found" });

        // Only allow locked payroll runs
        if (payrollRun.Status != PayrollApp.Domain.Enums.PayrollStatus.Locked)
            return Results.BadRequest(new { message = "Payroll run must be locked before generating bank file" });

        // Get all line items
        var lineItems = await session.Query<PayrollLineItem>()
            .Where(x => x.PayrollRunId == id)
            .ToListAsync(ct);

        if (lineItems.Count == 0)
            return Results.NotFound(new { message = "No line items found" });

        var generator = new BankFileGenerator();
        var paymentDate = new DateTime(payrollRun.Year, payrollRun.Month, 25); // Default: 25th of the month
        var defaultCompanyId = companyId ?? "COMPANY001";

        byte[] fileBytes;
        string fileName;
        string contentType;

        switch (bank.ToLower())
        {
            case "bca":
                fileBytes = generator.GenerateBcaFile(defaultCompanyId, lineItems.ToList(), paymentDate);
                fileName = $"BCA_Payroll_{payrollRun.Month:D2}_{payrollRun.Year}.txt";
                contentType = "text/plain";
                break;

            case "mandiri":
                fileBytes = generator.GenerateMandiriFile(defaultCompanyId, lineItems.ToList(), paymentDate);
                fileName = $"Mandiri_Payroll_{payrollRun.Month:D2}_{payrollRun.Year}.csv";
                contentType = "text/csv";
                break;

            case "bni":
                fileBytes = generator.GenerateBniFile(defaultCompanyId, lineItems.ToList(), paymentDate);
                fileName = $"BNI_Payroll_{payrollRun.Month:D2}_{payrollRun.Year}.txt";
                contentType = "text/plain";
                break;

            case "permata":
                fileBytes = generator.GeneratePermataFile(defaultCompanyId, lineItems.ToList(), paymentDate);
                fileName = $"Permata_Payroll_{payrollRun.Month:D2}_{payrollRun.Year}.txt";
                contentType = "text/plain";
                break;

            default:
                return Results.BadRequest(new { message = $"Unsupported bank: {bank}. Supported: bca, mandiri, bni, permata" });
        }

        return Results.File(fileBytes, contentType, fileName);
    }

    private static string GetMonthName(int month)
    {
        var monthNames = new[]
        {
            "Januari", "Februari", "Maret", "April", "Mei", "Juni",
            "Juli", "Agustus", "September", "Oktober", "November", "Desember"
        };

        return month >= 1 && month <= 12 ? monthNames[month - 1] : "Unknown";
    }
}

// Made with Bob