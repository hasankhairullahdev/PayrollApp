using System.Text;
using PayrollApp.Infrastructure.ReadModels;

namespace PayrollApp.Infrastructure.Export;

public class BankFileGenerator
{
    public byte[] GenerateBcaFile(
        string companyAccountNumber,
        List<PayrollLineItem> lineItems,
        DateTime paymentDate)
    {
        var sb = new StringBuilder();

        // Header record (fixed-width format)
        var header = $"H{companyAccountNumber.PadRight(10)}{paymentDate:yyyyMMdd}{lineItems.Count:D6}";
        sb.AppendLine(header);

        // Detail records
        foreach (var item in lineItems)
        {
            // Format: D + Account Number (10) + Amount (15, right-aligned) + Beneficiary Name (30)
            var accountNumber = item.EmployeeCode.PadRight(10);
            var amount = ((long)item.TakeHomePay).ToString().PadLeft(15, '0');
            var beneficiaryName = item.EmployeeName.Length > 30
                ? item.EmployeeName.Substring(0, 30)
                : item.EmployeeName.PadRight(30);

            var detail = $"D{accountNumber}{amount}{beneficiaryName}";
            sb.AppendLine(detail);
        }

        // Trailer record
        var totalAmount = lineItems.Sum(x => (long)x.TakeHomePay);
        var trailer = $"T{lineItems.Count:D6}{totalAmount.ToString().PadLeft(18, '0')}";
        sb.AppendLine(trailer);

        return Encoding.ASCII.GetBytes(sb.ToString());
    }

    public byte[] GenerateMandiriFile(
        string companyCode,
        List<PayrollLineItem> lineItems,
        DateTime paymentDate)
    {
        var sb = new StringBuilder();

        // Header CSV
        sb.AppendLine("Company Code,Payment Date,Account Number,Beneficiary Name,Amount,Description");

        // Detail records
        foreach (var item in lineItems)
        {
            var line = $"{companyCode}," +
                      $"{paymentDate:yyyy-MM-dd}," +
                      $"{item.EmployeeCode}," +
                      $"\"{item.EmployeeName}\"," +
                      $"{item.TakeHomePay:F2}," +
                      $"\"Salary Payment {paymentDate:MMMM yyyy}\"";
            sb.AppendLine(line);
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public byte[] GenerateBniFile(
        string companyId,
        List<PayrollLineItem> lineItems,
        DateTime paymentDate)
    {
        var sb = new StringBuilder();

        // BNI format: pipe-delimited
        sb.AppendLine("COMPANY_ID|PAYMENT_DATE|ACCOUNT_NUMBER|BENEFICIARY_NAME|AMOUNT|CURRENCY|DESCRIPTION");

        foreach (var item in lineItems)
        {
            var line = $"{companyId}|" +
                      $"{paymentDate:yyyyMMdd}|" +
                      $"{item.EmployeeCode}|" +
                      $"{item.EmployeeName}|" +
                      $"{item.TakeHomePay:F2}|" +
                      $"IDR|" +
                      $"Salary {paymentDate:MMM yyyy}";
            sb.AppendLine(line);
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public byte[] GeneratePermataFile(
        string corporateId,
        List<PayrollLineItem> lineItems,
        DateTime paymentDate)
    {
        var sb = new StringBuilder();

        // Permata format: tab-delimited
        sb.AppendLine("Corporate ID\tPayment Date\tAccount Number\tBeneficiary Name\tAmount\tRemark");

        foreach (var item in lineItems)
        {
            var line = $"{corporateId}\t" +
                      $"{paymentDate:dd/MM/yyyy}\t" +
                      $"{item.EmployeeCode}\t" +
                      $"{item.EmployeeName}\t" +
                      $"{item.TakeHomePay:N0}\t" +
                      $"Payroll {paymentDate:MMMM yyyy}";
            sb.AppendLine(line);
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }
}

// Made with Bob