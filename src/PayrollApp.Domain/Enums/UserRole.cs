namespace PayrollApp.Domain.Enums;

/// <summary>
/// User roles untuk role-based access control
/// </summary>
public enum UserRole
{
    /// <summary>
    /// Administrator - Full access to all features
    /// </summary>
    Admin = 1,

    /// <summary>
    /// HR Staff - Manage employees and payroll runs
    /// </summary>
    HR = 2,

    /// <summary>
    /// Finance Manager - Approve/reject payroll runs
    /// </summary>
    Finance = 3,

    /// <summary>
    /// Employee - View own payslip only
    /// </summary>
    Employee = 4
}

// Made with Bob
