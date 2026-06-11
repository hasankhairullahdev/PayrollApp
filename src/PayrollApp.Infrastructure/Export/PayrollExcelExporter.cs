using ClosedXML.Excel;
using PayrollApp.Infrastructure.ReadModels;

namespace PayrollApp.Infrastructure.Export;

public class PayrollExcelExporter
{
    public byte[] ExportPayrollRun(
        string period,
        List<PayrollLineItem> lineItems,
        decimal totalGross,
        decimal totalDeductions,
        decimal totalNet)
    {
        using var workbook = new XLWorkbook();

        // Sheet 1: Summary per karyawan
        CreateSummarySheet(workbook, period, lineItems, totalGross, totalDeductions, totalNet);

        // Sheet 2: Rekap BPJS
        CreateBpjsSheet(workbook, period, lineItems);

        // Sheet 3: Rekap PPh 21
        CreatePph21Sheet(workbook, period, lineItems);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private void CreateSummarySheet(
        XLWorkbook workbook,
        string period,
        List<PayrollLineItem> lineItems,
        decimal totalGross,
        decimal totalDeductions,
        decimal totalNet)
    {
        var worksheet = workbook.Worksheets.Add("Summary");

        // Title
        worksheet.Cell(1, 1).Value = "REKAP GAJI KARYAWAN";
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Font.FontSize = 14;

        worksheet.Cell(2, 1).Value = $"Periode: {period}";
        worksheet.Cell(2, 1).Style.Font.Bold = true;

        // Headers
        var headerRow = 4;
        var headers = new[]
        {
            "No", "NIK", "Nama", "Gaji Pokok", "Tunjangan", "Lembur",
            "Total Pendapatan", "BPJS Kesehatan", "BPJS TK", "PPh 21",
            "Total Potongan", "Gaji Bersih"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = worksheet.Cell(headerRow, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        }

        // Data
        var row = headerRow + 1;
        var no = 1;
        foreach (var item in lineItems)
        {
            worksheet.Cell(row, 1).Value = no++;
            worksheet.Cell(row, 2).Value = item.EmployeeCode;
            worksheet.Cell(row, 3).Value = item.EmployeeName;
            worksheet.Cell(row, 4).Value = item.BasicSalary;
            worksheet.Cell(row, 5).Value = item.Allowances;
            worksheet.Cell(row, 6).Value = item.Overtime;
            worksheet.Cell(row, 7).Value = item.GrossSalary;
            worksheet.Cell(row, 8).Value = item.BpjsKesehatan;
            worksheet.Cell(row, 9).Value = item.BpjsKetenagakerjaan;
            worksheet.Cell(row, 10).Value = item.Pph21;
            worksheet.Cell(row, 11).Value = item.Deductions;
            worksheet.Cell(row, 12).Value = item.TakeHomePay;

            // Format currency
            for (int col = 4; col <= 12; col++)
            {
                worksheet.Cell(row, col).Style.NumberFormat.Format = "#,##0";
            }

            row++;
        }

        // Total row
        worksheet.Cell(row, 1).Value = "TOTAL";
        worksheet.Cell(row, 1).Style.Font.Bold = true;
        worksheet.Cell(row, 7).Value = totalGross;
        worksheet.Cell(row, 11).Value = totalDeductions;
        worksheet.Cell(row, 12).Value = totalNet;

        for (int col = 7; col <= 12; col += 4)
        {
            worksheet.Cell(row, col).Style.Font.Bold = true;
            worksheet.Cell(row, col).Style.NumberFormat.Format = "#,##0";
            worksheet.Cell(row, col).Style.Fill.BackgroundColor = XLColor.LightYellow;
        }

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();
    }

    private void CreateBpjsSheet(XLWorkbook workbook, string period, List<PayrollLineItem> lineItems)
    {
        var worksheet = workbook.Worksheets.Add("BPJS");

        // Title
        worksheet.Cell(1, 1).Value = "REKAP BPJS KESEHATAN & KETENAGAKERJAAN";
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Font.FontSize = 14;

        worksheet.Cell(2, 1).Value = $"Periode: {period}";
        worksheet.Cell(2, 1).Style.Font.Bold = true;

        // Headers
        var headerRow = 4;
        var headers = new[]
        {
            "No", "NIK", "Nama", "Gaji Pokok",
            "BPJS Kesehatan (1%)", "BPJS TK (JHT+JP)",
            "Total BPJS Karyawan"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = worksheet.Cell(headerRow, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightBlue;
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        }

        // Data
        var row = headerRow + 1;
        var no = 1;
        decimal totalKesehatan = 0;
        decimal totalTk = 0;

        foreach (var item in lineItems)
        {
            worksheet.Cell(row, 1).Value = no++;
            worksheet.Cell(row, 2).Value = item.EmployeeCode;
            worksheet.Cell(row, 3).Value = item.EmployeeName;
            worksheet.Cell(row, 4).Value = item.BasicSalary;
            worksheet.Cell(row, 5).Value = item.BpjsKesehatan;
            worksheet.Cell(row, 6).Value = item.BpjsKetenagakerjaan;
            worksheet.Cell(row, 7).Value = item.BpjsKesehatan + item.BpjsKetenagakerjaan;

            totalKesehatan += item.BpjsKesehatan;
            totalTk += item.BpjsKetenagakerjaan;

            // Format currency
            for (int col = 4; col <= 7; col++)
            {
                worksheet.Cell(row, col).Style.NumberFormat.Format = "#,##0";
            }

            row++;
        }

        // Total row
        worksheet.Cell(row, 1).Value = "TOTAL";
        worksheet.Cell(row, 1).Style.Font.Bold = true;
        worksheet.Cell(row, 5).Value = totalKesehatan;
        worksheet.Cell(row, 6).Value = totalTk;
        worksheet.Cell(row, 7).Value = totalKesehatan + totalTk;

        for (int col = 5; col <= 7; col++)
        {
            worksheet.Cell(row, col).Style.Font.Bold = true;
            worksheet.Cell(row, col).Style.NumberFormat.Format = "#,##0";
            worksheet.Cell(row, col).Style.Fill.BackgroundColor = XLColor.LightYellow;
        }

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();
    }

    private void CreatePph21Sheet(XLWorkbook workbook, string period, List<PayrollLineItem> lineItems)
    {
        var worksheet = workbook.Worksheets.Add("PPh 21");

        // Title
        worksheet.Cell(1, 1).Value = "REKAP PPh 21";
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Font.FontSize = 14;

        worksheet.Cell(2, 1).Value = $"Periode: {period}";
        worksheet.Cell(2, 1).Style.Font.Bold = true;

        // Headers
        var headerRow = 4;
        var headers = new[]
        {
            "No", "NIK", "Nama", "Gaji Bruto", "PPh 21"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = worksheet.Cell(headerRow, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGreen;
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        }

        // Data
        var row = headerRow + 1;
        var no = 1;
        decimal totalGross = 0;
        decimal totalPph21 = 0;

        foreach (var item in lineItems)
        {
            worksheet.Cell(row, 1).Value = no++;
            worksheet.Cell(row, 2).Value = item.EmployeeCode;
            worksheet.Cell(row, 3).Value = item.EmployeeName;
            worksheet.Cell(row, 4).Value = item.GrossSalary;
            worksheet.Cell(row, 5).Value = item.Pph21;

            totalGross += item.GrossSalary;
            totalPph21 += item.Pph21;

            // Format currency
            worksheet.Cell(row, 4).Style.NumberFormat.Format = "#,##0";
            worksheet.Cell(row, 5).Style.NumberFormat.Format = "#,##0";

            row++;
        }

        // Total row
        worksheet.Cell(row, 1).Value = "TOTAL";
        worksheet.Cell(row, 1).Style.Font.Bold = true;
        worksheet.Cell(row, 4).Value = totalGross;
        worksheet.Cell(row, 5).Value = totalPph21;

        worksheet.Cell(row, 4).Style.Font.Bold = true;
        worksheet.Cell(row, 4).Style.NumberFormat.Format = "#,##0";
        worksheet.Cell(row, 4).Style.Fill.BackgroundColor = XLColor.LightYellow;

        worksheet.Cell(row, 5).Style.Font.Bold = true;
        worksheet.Cell(row, 5).Style.NumberFormat.Format = "#,##0";
        worksheet.Cell(row, 5).Style.Fill.BackgroundColor = XLColor.LightYellow;

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();
    }
}

// Made with Bob