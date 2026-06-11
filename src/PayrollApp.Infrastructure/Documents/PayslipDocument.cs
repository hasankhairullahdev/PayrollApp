using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using PayrollApp.Domain.ValueObjects;

namespace PayrollApp.Infrastructure.Documents;

public class PayslipDocument : IDocument
{
    private readonly PayslipData _data;

    public PayslipDocument(PayslipData data)
    {
        _data = data;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container
            .Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);
                page.Footer().Element(ComposeFooter);
            });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text("PT. PAYROLL INDONESIA").FontSize(16).Bold();
                column.Item().Text("Jl. Sudirman No. 123, Jakarta").FontSize(9);
                column.Item().Text("Tel: (021) 1234-5678").FontSize(9);
            });

            row.RelativeItem().AlignRight().Column(column =>
            {
                column.Item().Text("SLIP GAJI").FontSize(16).Bold();
                column.Item().Text($"Periode: {_data.Period}").FontSize(10);
                column.Item().Text($"Tanggal Cetak: {DateTime.Now:dd/MM/yyyy}").FontSize(9);
            });
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingVertical(20).Column(column =>
        {
            // Employee Info
            column.Item().Element(ComposeEmployeeInfo);

            column.Item().PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

            // Salary Breakdown
            column.Item().Element(ComposeSalaryBreakdown);

            column.Item().PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

            // Take Home Pay
            column.Item().Element(ComposeTakeHomePay);
        });
    }

    private void ComposeEmployeeInfo(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().Text("INFORMASI KARYAWAN").FontSize(12).Bold();
            column.Item().PaddingTop(5).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text($"Nama: {_data.EmployeeName}").FontSize(10);
                    col.Item().Text($"NIK: {_data.EmployeeId}").FontSize(10);
                });
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text($"Jabatan: {_data.Position}").FontSize(10);
                    col.Item().Text($"Departemen: {_data.Department}").FontSize(10);
                });
            });
        });
    }

    private void ComposeSalaryBreakdown(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().Text("RINCIAN GAJI").FontSize(12).Bold();
            column.Item().PaddingTop(10).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(2);
                });

                // Header
                table.Header(header =>
                {
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Komponen").Bold();
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Jumlah").Bold();
                });

                // Pendapatan
                table.Cell().Padding(5).Text("PENDAPATAN").Bold();
                table.Cell().Padding(5).Text("");

                table.Cell().PaddingLeft(10).Padding(5).Text("Gaji Pokok");
                table.Cell().Padding(5).AlignRight().Text(FormatMoney(_data.BasicSalary));

                if (_data.Allowances > 0)
                {
                    table.Cell().PaddingLeft(10).Padding(5).Text("Tunjangan");
                    table.Cell().Padding(5).AlignRight().Text(FormatMoney(_data.Allowances));
                }

                if (_data.Overtime > 0)
                {
                    table.Cell().PaddingLeft(10).Padding(5).Text("Lembur");
                    table.Cell().Padding(5).AlignRight().Text(FormatMoney(_data.Overtime));
                }

                table.Cell().Padding(5).Text("Total Pendapatan").Bold();
                table.Cell().Padding(5).AlignRight().Text(FormatMoney(_data.GrossSalary)).Bold();

                // Potongan
                table.Cell().PaddingTop(10).Padding(5).Text("POTONGAN").Bold();
                table.Cell().PaddingTop(10).Padding(5).Text("");

                table.Cell().PaddingLeft(10).Padding(5).Text("BPJS Kesehatan (1%)");
                table.Cell().Padding(5).AlignRight().Text(FormatMoney(_data.BpjsKesehatan));

                table.Cell().PaddingLeft(10).Padding(5).Text("BPJS Ketenagakerjaan");
                table.Cell().Padding(5).AlignRight().Text(FormatMoney(_data.BpjsTk));

                table.Cell().PaddingLeft(10).Padding(5).Text("PPh 21");
                table.Cell().Padding(5).AlignRight().Text(FormatMoney(_data.Pph21));

                table.Cell().Padding(5).Text("Total Potongan").Bold();
                table.Cell().Padding(5).AlignRight().Text(FormatMoney(_data.TotalDeductions)).Bold();
            });
        });
    }

    private void ComposeTakeHomePay(IContainer container)
    {
        container.Background(Colors.Blue.Lighten4).Padding(15).Row(row =>
        {
            row.RelativeItem().Text("GAJI BERSIH (TAKE HOME PAY)").FontSize(14).Bold();
            row.RelativeItem().AlignRight().Text(FormatMoney(_data.NetSalary)).FontSize(16).Bold();
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.AlignBottom().Column(column =>
        {
            column.Item().PaddingTop(20).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("Diterima Oleh,").FontSize(9);
                    col.Item().PaddingTop(40).LineHorizontal(1);
                    col.Item().Text(_data.EmployeeName).FontSize(9);
                });

                row.RelativeItem().Column(col =>
                {
                    col.Item().AlignRight().Text("Disetujui Oleh,").FontSize(9);
                    col.Item().PaddingTop(40).AlignRight().LineHorizontal(1);
                    col.Item().AlignRight().Text("HR Manager").FontSize(9);
                });
            });

            column.Item().PaddingTop(20).AlignCenter().Text("Dokumen ini dibuat secara otomatis dan sah tanpa tanda tangan basah").FontSize(8).Italic();
        });
    }

    private static string FormatMoney(decimal amount)
    {
        return $"Rp {amount:N0}";
    }
}

public record PayslipData(
    string EmployeeId,
    string EmployeeName,
    string Position,
    string Department,
    string Period,
    decimal BasicSalary,
    decimal Allowances,
    decimal Overtime,
    decimal GrossSalary,
    decimal BpjsKesehatan,
    decimal BpjsTk,
    decimal Pph21,
    decimal TotalDeductions,
    decimal NetSalary
);

// Made with Bob