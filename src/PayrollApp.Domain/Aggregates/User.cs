using PayrollApp.Domain.Enums;
using PayrollApp.Domain.Events;

namespace PayrollApp.Domain.Aggregates;

/// <summary>
/// User aggregate root - manages user authentication and authorization
/// Uses event sourcing pattern for state management
/// </summary>
public partial class User
{
    private readonly List<object> _uncommittedEvents = new();

    public Guid Id { get; private set; }
    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public string FullName { get; private set; } = null!;
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }

    // For event sourcing reconstruction
    public int Version { get; private set; }

    // Private constructor for event sourcing
    private User() { }

    /// <summary>
    /// Factory method for registering a new user
    /// </summary>
    public static User Register(string email, string passwordHash, string fullName, UserRole role)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty", nameof(email));

        if (!IsValidEmail(email))
            throw new ArgumentException("Invalid email format", nameof(email));

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash cannot be empty", nameof(passwordHash));

        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Full name cannot be empty", nameof(fullName));

        var user = new User();
        var @event = new UserRegistered(
            Guid.NewGuid(),
            email.ToLowerInvariant(),
            fullName,
            role,
            DateTime.UtcNow
        );

        user.RaiseEvent(@event);
        return user;
    }

    /// <summary>
    /// Record user login
    /// </summary>
    public void Login()
    {
        if (!IsActive)
            throw new InvalidOperationException("Cannot login. User account is deactivated.");

        var @event = new UserLoggedIn(Id, DateTime.UtcNow);
        RaiseEvent(@event);
    }

    /// <summary>
    /// Change user password
    /// </summary>
    public void ChangePassword(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new ArgumentException("Password hash cannot be empty", nameof(newPasswordHash));

        if (!IsActive)
            throw new InvalidOperationException("Cannot change password. User account is deactivated.");

        var @event = new UserPasswordChanged(Id, DateTime.UtcNow);
        RaiseEvent(@event);
    }

    /// <summary>
    /// Update user profile information
    /// </summary>
    public void UpdateProfile(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Full name cannot be empty", nameof(fullName));

        if (!IsActive)
            throw new InvalidOperationException("Cannot update profile. User account is deactivated.");

        var @event = new UserProfileUpdated(Id, fullName, DateTime.UtcNow);
        RaiseEvent(@event);
    }

    /// <summary>
    /// Deactivate user account
    /// </summary>
    public void Deactivate()
    {
        if (!IsActive)
            throw new InvalidOperationException("User account is already deactivated.");

        var @event = new UserDeactivated(Id, DateTime.UtcNow);
        RaiseEvent(@event);
    }

    /// <summary>
    /// Activate user account
    /// </summary>
    public void Activate()
    {
        if (IsActive)
            throw new InvalidOperationException("User account is already active.");

        var @event = new UserActivated(Id, DateTime.UtcNow);
        RaiseEvent(@event);
    }

    // Event sourcing infrastructure
    private void RaiseEvent(object @event)
    {
        Apply(@event);
        _uncommittedEvents.Add(@event);
        Version++;
    }

    private void Apply(object @event)
    {
        switch (@event)
        {
            case UserRegistered e:
                Apply(e);
                break;
            case UserLoggedIn e:
                Apply(e);
                break;
            case UserPasswordChanged e:
                Apply(e);
                break;
            case UserProfileUpdated e:
                Apply(e);
                break;
            case UserDeactivated e:
                Apply(e);
                break;
            case UserActivated e:
                Apply(e);
                break;
        }
    }

    private void Apply(UserRegistered @event)
    {
        Id = @event.UserId;
        Email = @event.Email;
        FullName = @event.FullName;
        Role = @event.Role;
        IsActive = true;
        CreatedAt = @event.RegisteredAt;
    }

    private void Apply(UserLoggedIn @event)
    {
        LastLoginAt = @event.LoginAt;
    }

    private void Apply(UserPasswordChanged @event)
    {
        // Password hash is updated separately in the handler
        // This event is for audit trail only
    }

    private void Apply(UserProfileUpdated @event)
    {
        FullName = @event.FullName;
    }

    private void Apply(UserDeactivated @event)
    {
        IsActive = false;
    }

    private void Apply(UserActivated @event)
    {
        IsActive = true;
    }

    public IEnumerable<object> GetUncommittedEvents() => _uncommittedEvents;

    public void ClearUncommittedEvents() => _uncommittedEvents.Clear();

    // Helper method for email validation
    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}

// Made with Bob
