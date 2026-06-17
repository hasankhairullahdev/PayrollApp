# Authentication & Authorization Design

**Created**: 2026-06-17
**Status**: Design Phase
**Purpose**: Design document untuk implementasi JWT authentication dan RBAC

---

## 🎯 Requirements

### Functional Requirements
1. User dapat login dengan email + password
2. System generate JWT token setelah login berhasil
3. Token digunakan untuk authenticate setiap API request
4. User memiliki role: Admin, HR, Finance, Employee
5. Setiap endpoint memiliki role-based access control
6. User dapat logout (invalidate token)
7. Token auto-refresh sebelum expire

### Non-Functional Requirements
1. Password hashed dengan BCrypt (cost factor 12)
2. JWT token expire dalam 8 jam
3. Refresh token expire dalam 7 hari
4. Token stored di httpOnly cookie (XSS protection)
5. CORS configured untuk frontend domain

---

## 🏗️ Architecture Design

### User Roles & Permissions

```
┌─────────────┬──────────────────────────────────────────────────┐
│ Role        │ Permissions                                      │
├─────────────┼──────────────────────────────────────────────────┤
│ Admin       │ - Full access to all features                    │
│             │ - User management (create, update, delete users) │
│             │ - System configuration                           │
├─────────────┼──────────────────────────────────────────────────┤
│ HR          │ - Create payroll runs                            │
│             │ - Start review                                   │
│             │ - View all payroll data                          │
│             │ - Employee CRUD                                  │
│             │ - Generate reports                               │
│             │ - Lock payroll (after Finance approval)          │
│             │ - Initiate disbursement                          │
├─────────────┼──────────────────────────────────────────────────┤
│ Finance     │ - View payroll runs (read-only)                  │
│             │ - Approve/Reject payroll (UnderReview status)    │
│             │ - View reports                                   │
│             │ - Cannot create or modify payroll                │
├─────────────┼──────────────────────────────────────────────────┤
│ Employee    │ - View own payslip only                          │
│             │ - View own salary history                        │
│             │ - Download own payslip PDF                       │
│             │ - Cannot access other employees' data            │
└─────────────┴──────────────────────────────────────────────────┘
```

### Authentication Flow

```
┌─────────┐                ┌─────────┐                ┌──────────┐
│ Browser │                │   API   │                │ Database │
└────┬────┘                └────┬────┘                └────┬─────┘
     │                          │                          │
     │ POST /api/auth/login     │                          │
     │ { email, password }      │                          │
     ├─────────────────────────>│                          │
     │                          │                          │
     │                          │ Query User by email      │
     │                          ├─────────────────────────>│
     │                          │                          │
     │                          │ User data                │
     │                          │<─────────────────────────┤
     │                          │                          │
     │                          │ Verify password (BCrypt) │
     │                          │                          │
     │                          │ Generate JWT token       │
     │                          │ (userId, email, role)    │
     │                          │                          │
     │ Set-Cookie: token=xxx    │                          │
     │ { user, token }          │                          │
     │<─────────────────────────┤                          │
     │                          │                          │
     │ GET /api/payroll         │                          │
     │ Cookie: token=xxx        │                          │
     ├─────────────────────────>│                          │
     │                          │                          │
     │                          │ Validate JWT             │
     │                          │ Extract userId, role     │
     │                          │                          │
     │                          │ Check authorization      │
     │                          │ (role has permission?)   │
     │                          │                          │
     │                          │ Query data               │
     │                          ├─────────────────────────>│
     │                          │                          │
     │ Response data            │                          │
     │<─────────────────────────┤                          │
     │                          │                          │
```

---

## 📦 Domain Model

### User Aggregate

```csharp
public class User : AggregateRoot
{
    public Guid Id { get; private set; }
    public string Email { get; private set; }
    public string PasswordHash { get; private set; }
    public string FullName { get; private set; }
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    
    // Domain methods
    public void Register(string email, string passwordHash, string fullName, UserRole role)
    public void Login()
    public void ChangePassword(string newPasswordHash)
    public void UpdateProfile(string fullName)
    public void Deactivate()
    public void Activate()
}
```

### UserRole Enum

```csharp
public enum UserRole
{
    Admin = 1,
    HR = 2,
    Finance = 3,
    Employee = 4
}
```

### Domain Events

```csharp
public record UserRegistered(Guid UserId, string Email, string FullName, UserRole Role, DateTime RegisteredAt);
public record UserLoggedIn(Guid UserId, DateTime LoginAt);
public record UserPasswordChanged(Guid UserId, DateTime ChangedAt);
public record UserProfileUpdated(Guid UserId, string FullName, DateTime UpdatedAt);
public record UserDeactivated(Guid UserId, DateTime DeactivatedAt);
public record UserActivated(Guid UserId, DateTime ActivatedAt);
```

---

## 🔐 Security Implementation

### Password Hashing

```csharp
public class PasswordHasher
{
    private const int WorkFactor = 12;
    
    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    }
    
    public bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}
```

### JWT Token Generation

```csharp
public class JwtTokenGenerator
{
    public string GenerateToken(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };
        
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        
        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: credentials
        );
        
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

---

## 🛡️ Authorization Policies

### Endpoint Protection Matrix

```
┌────────────────────────────────────┬───────┬────┬─────────┬──────────┐
│ Endpoint                           │ Admin │ HR │ Finance │ Employee │
├────────────────────────────────────┼───────┼────┼─────────┼──────────┤
│ POST /api/payroll                  │   ✓   │ ✓  │    ✗    │    ✗     │
│ GET /api/payroll                   │   ✓   │ ✓  │    ✓    │    ✗     │
│ GET /api/payroll/{id}              │   ✓   │ ✓  │    ✓    │    ✗     │
│ POST /api/payroll/{id}/start-review│   ✓   │ ✓  │    ✗    │    ✗     │
│ POST /api/payroll/{id}/approve    │   ✓   │ ✗  │    ✓    │    ✗     │
│ POST /api/payroll/{id}/reject     │   ✓   │ ✗  │    ✓    │    ✗     │
│ POST /api/payroll/{id}/lock       │   ✓   │ ✓  │    ✗    │    ✗     │
│ GET /api/employees                 │   ✓   │ ✓  │    ✗    │    ✗     │
│ POST /api/employees                │   ✓   │ ✓  │    ✗    │    ✗     │
│ PUT /api/employees/{id}            │   ✓   │ ✓  │    ✗    │    ✗     │
│ GET /api/payslip/me                │   ✓   │ ✓  │    ✓    │    ✓     │
│ GET /api/users                     │   ✓   │ ✗  │    ✗    │    ✗     │
│ POST /api/users                    │   ✓   │ ✗  │    ✗    │    ✗     │
└────────────────────────────────────┴───────┴────┴─────────┴──────────┘
```

### Authorization Attribute

```csharp
[AttributeUsage(AttributeTargets.Method)]
public class AuthorizeRolesAttribute : Attribute
{
    public UserRole[] Roles { get; }
    
    public AuthorizeRolesAttribute(params UserRole[] roles)
    {
        Roles = roles;
    }
}

// Usage:
[AuthorizeRoles(UserRole.Admin, UserRole.HR)]
public static async Task<IResult> CreatePayrollRun(...)
```

---

## 📱 Frontend Implementation

### Auth Context

```typescript
interface AuthContextType {
  user: User | null;
  token: string | null;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
  isAuthenticated: boolean;
  hasRole: (role: UserRole) => boolean;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);
```

### Protected Route

```typescript
function ProtectedRoute({ 
  children, 
  allowedRoles 
}: { 
  children: React.ReactNode; 
  allowedRoles?: UserRole[] 
}) {
  const { user, isAuthenticated } = useAuth();
  
  if (!isAuthenticated) {
    return <Navigate to="/login" />;
  }
  
  if (allowedRoles && !allowedRoles.includes(user.role)) {
    return <Navigate to="/unauthorized" />;
  }
  
  return <>{children}</>;
}
```

---

## 🗄️ Database Schema

### users table (Marten document)

```sql
CREATE TABLE users (
    id UUID PRIMARY KEY,
    email VARCHAR(255) UNIQUE NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    full_name VARCHAR(255) NOT NULL,
    role VARCHAR(50) NOT NULL,
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP NOT NULL,
    last_login_at TIMESTAMP,
    data JSONB NOT NULL  -- Marten document storage
);

CREATE INDEX idx_users_email ON users(email);
CREATE INDEX idx_users_role ON users(role);
```

---

## 🚀 Implementation Plan

### Phase 1: Backend Foundation
1. Create User aggregate in Domain layer
2. Create domain events (UserRegistered, UserLoggedIn, etc.)
3. Add BCrypt.Net-Next package
4. Implement PasswordHasher service
5. Implement JwtTokenGenerator service

### Phase 2: Authentication Endpoints
1. Create RegisterUserCommand + Handler
2. Create LoginCommand + Handler
3. Create LogoutCommand + Handler
4. Add /api/auth/register endpoint
5. Add /api/auth/login endpoint
6. Add /api/auth/logout endpoint
7. Add /api/auth/me endpoint (get current user)

### Phase 3: Authorization Middleware
1. Add JWT authentication middleware
2. Create AuthorizeRolesAttribute
3. Create authorization policy handler
4. Apply authorization to existing endpoints
5. Add role-based filtering in queries

### Phase 4: Frontend Auth
1. Create login page UI
2. Implement AuthContext
3. Create useAuth hook
4. Implement ProtectedRoute component
5. Add role-based UI rendering
6. Add logout functionality
7. Add token refresh logic

### Phase 5: User Management
1. Create user management page (Admin only)
2. Add user CRUD operations
3. Add password reset functionality
4. Add user activation/deactivation

---

## 🧪 Testing Strategy

### Unit Tests
- PasswordHasher: hash and verify
- JwtTokenGenerator: token generation and validation
- User aggregate: domain methods and invariants

### Integration Tests
- Login flow: valid credentials → token returned
- Login flow: invalid credentials → 401
- Protected endpoint: valid token → access granted
- Protected endpoint: invalid token → 401
- Protected endpoint: wrong role → 403

### E2E Tests
- User can register → login → access dashboard
- HR can create payroll, Finance cannot
- Finance can approve payroll, HR cannot
- Employee can only see own payslip

---

## 📝 Configuration

### appsettings.json

```json
{
  "Jwt": {
    "Secret": "your-super-secret-key-min-32-characters",
    "Issuer": "PayrollApp",
    "Audience": "PayrollApp.Client",
    "ExpirationHours": 8
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:3000"]
  }
}
```

### Environment Variables

```env
JWT_SECRET=your-super-secret-key-min-32-characters
JWT_ISSUER=PayrollApp
JWT_AUDIENCE=PayrollApp.Client
JWT_EXPIRATION_HOURS=8
FRONTEND_URL=http://localhost:3000
```

---

## 🔒 Security Considerations

1. **Password Policy**: Minimum 8 characters, must include uppercase, lowercase, number
2. **Rate Limiting**: Max 5 login attempts per 15 minutes per IP
3. **Token Storage**: httpOnly cookie, secure flag in production
4. **CORS**: Whitelist frontend domain only
5. **SQL Injection**: Use parameterized queries (Marten handles this)
6. **XSS Protection**: Sanitize all user inputs
7. **CSRF Protection**: Use anti-forgery tokens for state-changing operations

---

## 📚 Dependencies

### Backend
```xml
<PackageReference Include="BCrypt.Net-Next" Version="4.0.3" />
<PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="7.0.3" />
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.0" />
```

### Frontend
```json
{
  "axios": "^1.6.0",
  "js-cookie": "^3.0.5",
  "@types/js-cookie": "^3.0.6"
}
```

---

**END OF AUTH_DESIGN.md**