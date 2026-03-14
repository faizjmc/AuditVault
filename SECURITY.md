# Security Policy — AuditVault API

## OWASP Top 10 Mitigations

### A01 — Broken Access Control
- **Tenant isolation** is enforced at two independent layers:
  1. EF Core global query filter in `AppDbContext` — every `AuditLog` query is automatically scoped to the current tenant. A developer cannot forget to add a `WHERE tenant_id = ?` clause.
  2. `TenantResolutionMiddleware` — TenantId is always sourced from the validated JWT, never from the request body or URL parameters.
- **RBAC** is enforced using ASP.NET Core's `[Authorize(Roles = "...")]` attribute. Auditors can only POST; Admins can only GET.

### A02 — Cryptographic Failures
- Passwords are hashed using **BCrypt** (work factor 11) — never stored in plaintext.
- All API communication is over **HTTPS** (enforced via `UseHttpsRedirection()`).
- JWTs are signed with **HMAC-SHA256**. Token expiry is short (60 minutes, configurable).

### A03 — Injection
- All database queries use **EF Core parameterized queries** exclusively.
- Raw SQL is never constructed by concatenating user input.
- Request payloads are validated with **FluentValidation** before reaching the service layer.

### A04 — Insecure Design
- The system is designed with a principle of least privilege: Admins cannot upload; Auditors can only access their own tenant's data.
- TenantId is never trusted from client input.

### A05 — Security Misconfiguration
- `GlobalExceptionMiddleware` returns generic error messages in production — no stack traces or internal details are exposed.
- `Helmet`-equivalent headers are set via ASP.NET Core defaults.
- CORS is not configured wide-open.

### A06 — Vulnerable and Outdated Components
- All dependencies are managed via NuGet with explicit version pinning.
- `dotnet list package --vulnerable` is run as part of CI.

### A07 — Identification and Authentication Failures
- Login failures return a **generic error** — the response does not reveal whether the email or password was incorrect (prevents user enumeration).
- `ClockSkew = TimeSpan.Zero` on JWT validation — no tolerance window on expired tokens.
- Rate limiting on the `/auth/login` endpoint (10 req/min per IP) prevents brute-force attacks.

### A08 — Software and Data Integrity Failures
- All audit logs are immutable once created — there is no PATCH or DELETE endpoint.
- Migrations are applied deterministically via EF Core.

### A09 — Security Logging and Monitoring Failures
- All authentication events (success and failure), tenant context resolution, and cross-tenant access attempts are logged via **Serilog** with structured fields (TenantId, UserId, LogId).
- In production, logs should be shipped to a SIEM (e.g. Azure Monitor, Datadog).

### A10 — Server-Side Request Forgery (SSRF)
- The API does not make any outbound HTTP requests based on user-supplied URLs.

---

## Secrets Management

### Development
- Connection strings and JWT keys are stored in `appsettings.Development.json` (git-ignored in production).

### Production
- Secrets must be provided via environment variables or a secrets manager:
  - **Azure**: Azure Key Vault + Managed Identity
  - **AWS**: AWS Secrets Manager
  - **Self-hosted**: HashiCorp Vault
- The `JwtSettings:SecretKey` must be a random string of at least 32 characters, generated with a CSPRNG.
- **Never commit production secrets to source control.** The `.gitignore` excludes `appsettings.Production.json`.

---

## CI/CD Security Scanning

The following pipeline (GitHub Actions) can be used to automate security checks:

```yaml
# .github/workflows/security.yml
name: Security Scan

on: [push, pull_request]

jobs:
  security:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'

      - name: Restore
        run: dotnet restore

      - name: Check for vulnerable NuGet packages
        run: dotnet list package --vulnerable --include-transitive

      - name: Run tests
        run: dotnet test --no-restore

      - name: SAST — CodeQL Analysis
        uses: github/codeql-action/analyze@v3
        with:
          languages: csharp

      - name: Container scan (Trivy)
        uses: aquasecurity/trivy-action@master
        with:
          image-ref: 'auditvault-api:latest'
          format: 'table'
          exit-code: '1'
          severity: 'CRITICAL,HIGH'
```

---

## Reporting a Vulnerability

Please do not open a public GitHub issue for security vulnerabilities.
Contact the maintainer directly with details and allow reasonable time to patch before public disclosure.
