# Insurance Claim Processing System — Architecture Document

> **Internship Capstone Project**  
> Stack: ASP.NET Core 8 · Angular 17 · PostgreSQL 16 · EF Core 8 · Hangfire · SignalR · Stripe · NUnit · Serilog

---

## 1. Requirements Understanding

### 1.1 What the System Does

An enterprise-grade insurance claim processing portal where:

| Actor | Core Need |
|-------|-----------|
| **Policyholder** | Register, submit claims, upload documents, pay premiums, track status, receive notifications |
| **ClaimReviewer** | Review assigned claims, verify/reject documents, request more docs, add notes |
| **ClaimsManager** | Final approve or reject claims, set payout amount, override assignments |
| **FinanceOfficer** | Process Stripe payments for approved claims only |
| **Admin** | Create staff accounts, approve policyholder registrations, view audit logs, generate reports, monitor system health |

### 1.2 Key Business Rules

1. **Policy must be Active or in GracePeriod** before a claim can be submitted — Lapsed policies block all new claims
2. **Only one open claim per policy at a time** — duplicate open claims rejected
3. **IRDAI Grace Period** — Monthly premium: 15-day grace; all others: 30-day grace; no penalty during grace
4. **Payout formula** — `FinalPayableAmount = MIN(ClaimedAmount, SubLimit) × (1 - CoPayPercent/100) - DeductibleAmount`
5. **Life insurance** — Fixed benefit (ignores actual bills); nominee can file; AES-256 Aadhaar encryption mandatory
6. **SLA enforcement** — Each ClaimType has configurable `SlaDays`; breaches flagged daily by Hangfire job
7. **CoverageExhausted** is a terminal status — no cron job should ever overwrite it
8. **Maturity/Survival claims** — Only one ever per policy, even across all statuses including Closed
9. **Reviewer auto-assignment** — Specialization match → lowest workload → round-robin tie-break
10. **Policy revival** — Must be within 2 years of `LapsedAt`; penalty applies on revival, not during grace

---

## 2. System Architecture Overview

```
┌──────────────────────────────────────────────────────────────────┐
│                        CLIENT LAYER                              │
│   Angular SPA  ·  5 role-specific dashboards  ·  Stripe.js      │
│   JWT Interceptor  ·  RoleGuard  ·  SignalR Client               │
└──────────────────────────┬───────────────────────────────────────┘
                           │ HTTPS + JWT Bearer
┌──────────────────────────▼───────────────────────────────────────┐
│              MIDDLEWARE PIPELINE (ASP.NET Core)                  │
│  CORS → JWT Auth → Rate Limiter → Exception Handler              │
│  → Request Logger (Serilog + CorrelationID) → FluentValidation   │
└──────────────────────────┬───────────────────────────────────────┘
                           │
┌──────────────────────────▼───────────────────────────────────────┐
│                   PRESENTATION LAYER                             │
│  Controllers: Auth · Claims · Policies · Documents · Payments    │
│               Admin · Reports · Notifications · Nominees         │
│  All routes prefixed: /api/v1/                                   │
└──────────────────────────┬───────────────────────────────────────┘
                           │ Depends on interfaces only (DIP)
┌──────────────────────────▼───────────────────────────────────────┐
│                  APPLICATION / SERVICE LAYER                     │
│  IAuthService · IClaimService · IPolicyService                   │
│  IPremiumPaymentService · IDocumentService · IPaymentService     │
│  IClaimValidationService · INotificationService · IReportService │
│  ISlaService · IUserRegistrationService · ClaimStateMachine      │
│  Money (value object) · ClaimExtensions (ext methods)            │
│  PagedResult<T> · IRepository<T> (generic classes)               │
└──────────────────────────┬───────────────────────────────────────┘
                           │ Depends on repository interfaces
┌──────────────────────────▼───────────────────────────────────────┐
│                    REPOSITORY LAYER                              │
│  IRepository<T> (generic) · IUnitOfWork                          │
│  IClaimRepository · IPolicyRepository · IUserRepository          │
│  IDocumentRepository · IPaymentRepository · IPremiumRepository   │
│  INomineeRepository · IRefreshTokenRepository · IAuditLogRepo    │
└──────────────────────────┬───────────────────────────────────────┘
                           │ EF Core + Fluent API
┌──────────────────────────▼───────────────────────────────────────┐
│                   DATA LAYER — PostgreSQL 16                     │
│  18 Tables  ·  Code-First Migrations  ·  Fluent API Config       │
│  Global soft-delete query filter via BaseEntity                  │
│  DB indexes on all FK columns + Status + Specialization          │
└──────────────────────────────────────────────────────────────────┘
```

---

## 3. Solution Structure

```
ClaimProcessing.sln
├── ClaimProcessing.Domain/
│   ├── Entities/
│   │   ├── BaseEntity.cs              ← abstract; Id, CreatedAt, UpdatedAt, IsDeleted
│   │   ├── User.cs
│   │   ├── Policy.cs
│   │   ├── Claim.cs
│   │   ├── Document.cs
│   │   ├── Payment.cs
│   │   ├── Nominee.cs
│   │   ├── PremiumPaymentSchedule.cs
│   │   ├── ClaimWorkflowHistory.cs
│   │   ├── Notification.cs
│   │   ├── AuditLog.cs
│   │   ├── Comment.cs
│   │   ├── PolicyType.cs
│   │   ├── ClaimType.cs
│   │   ├── PolicyBenefitRules.cs      ← seed-only, no CRUD
│   │   ├── ThirdPartyClaimant.cs      ← handled inside ClaimService
│   │   ├── RefreshToken.cs
│   │   ├── PasswordResetToken.cs
│   │   └── SystemHealthLog.cs         ← no BaseEntity; cron job only
│   ├── Enums/
│   │   ├── UserRole.cs
│   │   ├── ClaimStatus.cs
│   │   ├── PolicyStatus.cs
│   │   ├── PremiumStatus.cs
│   │   └── ...all other enums
│   ├── ValueObjects/
│   │   └── Money.cs                   ← operator overloading: +, -, ==, >
│   └── Exceptions/
│       ├── NotFoundException.cs
│       ├── BusinessRuleException.cs
│       └── UnauthorizedAccessException.cs
│
├── ClaimProcessing.Application/
│   ├── Interfaces/
│   │   ├── Repositories/
│   │   │   ├── IRepository.cs         ← generic: GetById, GetAll, Find, Add, Update, Delete
│   │   │   ├── IClaimRepository.cs
│   │   │   ├── IPolicyRepository.cs
│   │   │   └── ...all specific repos
│   │   ├── Services/
│   │   │   ├── IAuthService.cs
│   │   │   ├── IClaimService.cs
│   │   │   └── ...all service interfaces
│   │   └── External/
│   │       ├── IEmailService.cs
│   │       ├── IFileStorageService.cs
│   │       ├── IStripeService.cs
│   │       ├── IPiiEncryptionService.cs
│   │       └── IAadhaarMaskingService.cs
│   ├── Services/
│   │   ├── AuthService.cs
│   │   ├── ClaimService.cs
│   │   ├── PolicyService.cs
│   │   ├── PremiumPaymentService.cs
│   │   ├── DocumentService.cs
│   │   ├── PaymentService.cs
│   │   ├── ClaimValidationService.cs
│   │   ├── ClaimStateMachine.cs
│   │   ├── NotificationService.cs
│   │   ├── ReportService.cs
│   │   ├── SlaService.cs
│   │   └── UserRegistrationService.cs
│   ├── DTOs/
│   │   ├── Auth/
│   │   ├── Claims/
│   │   ├── Policies/
│   │   └── ...per module
│   ├── Validators/                    ← FluentValidation
│   ├── Mappings/                      ← AutoMapper profiles
│   │   ├── ClaimProfile.cs
│   │   ├── PolicyProfile.cs
│   │   ├── UserProfile.cs
│   │   └── NomineeProfile.cs          ← IdentityProofNumberEncrypted NEVER mapped
│   └── Extensions/
│       └── ClaimExtensions.cs         ← IsOverSlaDeadline, IsEligibleForClaim, ToMaskedPhone…
│
├── ClaimProcessing.Infrastructure/
│   ├── Data/
│   │   ├── AppDbContext.cs
│   │   ├── Configurations/            ← one IEntityTypeConfiguration<T> per entity
│   │   ├── Migrations/
│   │   └── Seeder/
│   │       └── DbSeeder.cs            ← Admin account, PolicyTypes, ClaimTypes, BenefitRules
│   ├── Repositories/
│   │   ├── GenericRepository.cs       ← implements IRepository<T>
│   │   ├── UnitOfWork.cs
│   │   ├── ClaimRepository.cs
│   │   └── ...all specific repos
│   ├── Services/
│   │   ├── SmtpEmailService.cs
│   │   ├── LocalFileStorageService.cs
│   │   ├── StripePaymentService.cs
│   │   ├── AesEncryptionService.cs    ← Singleton, key from env var
│   │   └── AadhaarMaskingService.cs   ← Singleton
│   └── Jobs/
│       ├── PremiumReminderJob.cs      ← daily 6 AM
│       ├── GracePeriodLapseJob.cs     ← daily 7 AM
│       ├── PolicyExpiryJob.cs         ← daily midnight
│       ├── SlaCheckerJob.cs           ← daily 1 AM
│       └── SystemHealthCheckJob.cs    ← every 5 min
│
├── ClaimProcessing.API/
│   ├── Controllers/
│   ├── Middleware/
│   │   ├── ExceptionMiddleware.cs
│   │   └── RequestLoggingMiddleware.cs
│   ├── Hubs/
│   │   └── NotificationHub.cs         ← SignalR
│   └── Program.cs
│
└── ClaimProcessing.Tests/
    ├── Unit/
    │   ├── Services/                  ← 100% coverage target
    │   ├── Controllers/               ← ≥80% coverage
    │   ├── Validators/                ← ≥80% coverage
    │   └── StateMachine/              ← ≥80% coverage
    └── Integration/
        └── ClaimLifecycleTests.cs     ← TestContainers + real PostgreSQL
```

---

## 4. Database — 18 Entities

| # | Table | Purpose |
|---|-------|---------|
| 1 | `Users` | All 5 roles; lockout fields; registration status |
| 2 | `Policies` | Policy master with status lifecycle |
| 3 | `PolicyTypes` | Health / Auto / Life / Property — seed data |
| 4 | `Claims` | Central entity; payout fields; SLA deadline |
| 5 | `ClaimTypes` | Per-type config: SlaDays, IsMaturityClaim, RequiredDocs |
| 6 | `Documents` | Uploaded files with MIME + soft-delete |
| 7 | `ClaimWorkflowHistory` | Immutable audit of every status change |
| 8 | `Notifications` | In-app + email + SMS log |
| 9 | `AuditLogs` | Compliance-grade immutable event log |
| 10 | `Comments` | Internal (reviewer-only) and public notes |
| 11 | `Payments` | Stripe payments with idempotency key |
| 12 | `Nominees` | Life insurance; Aadhaar AES-256 encrypted |
| 13 | `ThirdPartyClaimants` | Auto third-party; handled inside ClaimService |
| 14 | `PolicyBenefitRules` | Rules lookup — seed only, no CRUD |
| 15 | `SystemHealthLogs` | Cron job health snapshots |
| 16 | `PremiumPaymentSchedule` | Full premium lifecycle per policy |
| 17 | `RefreshTokens` | JWT refresh with rotation + family revocation |
| 18 | `PasswordResetTokens` | 30-min SHA-256 hashed reset links |

### BaseEntity (abstract — inherited by all 17 except SystemHealthLog)

```csharp
public abstract class BaseEntity
{
    public Guid     Id        { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool     IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }

    protected BaseEntity() => CreatedAt = UpdatedAt = DateTime.UtcNow;  // constructor chaining
    protected BaseEntity(Guid id) : this() => Id = id;                   // chained overload
}
```

---

## 5. Service Layer — Interface Contracts

```csharp
// Generic repository — showcases generic classes
public interface IRepository<T> where T : BaseEntity
{
    Task<T?>              GetByIdAsync(Guid id);
    Task<IEnumerable<T>>  GetAllAsync();
    Task<IEnumerable<T>>  FindAsync(Expression<Func<T, bool>> predicate);
    Task<T>               AddAsync(T entity);
    Task                  UpdateAsync(T entity);
    Task                  DeleteAsync(Guid id);
    Task<bool>            ExistsAsync(Guid id);
}

// Claim service
public interface IClaimService
{
    Task<ClaimResponseDto>        SubmitAsync(SubmitClaimDto dto, Guid userId);
    Task<ClaimResponseDto>        GetByIdAsync(Guid claimId);
    Task<PagedResult<ClaimResponseDto>> GetPagedAsync(ClaimFilterDto filter);
    Task                          UpdateStatusAsync(Guid claimId, ClaimStatus status);
    Task                          AutoAssignReviewerAsync(Guid claimId);
    Task                          AssignReviewerAsync(Guid claimId, Guid reviewerId);
    Task<List<WorkflowHistoryDto>>GetWorkflowHistoryAsync(Guid claimId);
    Task                          AddCommentAsync(Guid claimId, CommentDto dto);
}

// Claim validation — all 9 business rule checks
public interface IClaimValidationService
{
    Task<ValidationResult> ValidateSubmissionAsync(SubmitClaimDto dto, Guid policyHolderId);
    Task                   CheckPolicyActiveAsync(Guid policyId);
    Task                   CheckDuplicateClaimAsync(Guid policyId);
    Task                   CheckWaitingPeriodAsync(Guid policyId, DateTime incidentDate);
    Task                   CheckCoverageAsync(Guid policyId, decimal claimedAmount);
    Task                   CheckMaturityDuplicateAsync(Guid policyId);
    Task<decimal>          CalculatePayoutAsync(Guid policyId, decimal claimedAmount);
}
```

---

## 6. Claim State Machine

```
Valid transitions:
  Submitted        → UnderReview        [System: on auto-assign]
  UnderReview      → DocumentsPending   [ClaimReviewer only]
  DocumentsPending → UnderReview        [System: on document upload]
  UnderReview      → Approved           [ClaimsManager only]
  UnderReview      → Rejected           [ClaimsManager only]
  Approved         → Closed             [System: on Stripe payment success]
  Rejected         → Closed             [System: automatic]

Invalid transitions blocked:
  Draft → Approved  (skip review)
  Closed → any      (terminal state)
  Rejected → Approved (without re-submission)

Every transition writes to ClaimWorkflowHistory (immutable audit trail).
```

---

## 7. Security Architecture

| Concern | Implementation |
|---------|---------------|
| **Authentication** | Custom JWT (HS256) · 15 min access token · 7 day refresh token |
| **Token storage** | Access token in memory (Angular) · Refresh in httpOnly cookie |
| **Token rotation** | Every refresh revokes old token and issues new one |
| **Theft detection** | Revoked token reuse → entire family revoked |
| **Password hashing** | BCrypt cost factor 12 |
| **First login** | Forced password change (`IsFirstLogin = true`) |
| **Rate limiting** | 5 policies — auth-strict / upload / reports / global / webhook |
| **Account lockout** | 5 failed logins → 15 min lock (per account, application layer) |
| **Authorization** | Policy-based `[Authorize(Policy="ClaimsManagerOnly")]` |
| **PII protection** | AES-256 Aadhaar/PAN · masked in all API responses and logs |
| **Audit trail** | Every data access/change logged to `AuditLogs` (immutable) |
| **CORS** | Whitelist Angular origin only |
| **Document access** | Authenticated API endpoint only — never public static files |

---

## 8. API Design

### Response Envelope (all endpoints)
```json
{
  "success": true,
  "data": { },
  "message": "Claim submitted successfully",
  "errors": [],
  "pagination": {
    "page": 1,
    "pageSize": 10,
    "totalCount": 250,
    "totalPages": 25,
    "hasNext": true,
    "hasPrevious": false
  }
}
```

### Key Endpoints

| Method | Route | Role | Description |
|--------|-------|------|-------------|
| POST | `/api/v1/auth/register` | Public | Policyholder self-registration |
| POST | `/api/v1/auth/login` | Public | Returns JWT + sets refresh cookie |
| POST | `/api/v1/auth/refresh` | Public | Rotates refresh token |
| POST | `/api/v1/auth/forgot-password` | Public | Sends reset email |
| POST | `/api/v1/claims` | Policyholder | Submit new claim |
| GET | `/api/v1/claims` | All | Paginated + filtered list |
| PATCH | `/api/v1/claims/{id}/status` | Reviewer/Manager | Status transition |
| POST | `/api/v1/documents/upload` | Policyholder/Reviewer | Upload with MIME check |
| GET | `/api/v1/documents/{id}/download` | Authorized | Secure file download |
| POST | `/api/v1/payments/{claimId}/process` | FinanceOfficer | Stripe PaymentIntent |
| POST | `/api/v1/stripe/webhook` | System | Stripe callback (no rate limit) |
| GET | `/api/v1/reports/claims` | Admin/Manager | Claims report + CSV |
| GET | `/api/health` | Public | System health check |

---

## 9. Hangfire Background Jobs

| Job | Schedule | Purpose |
|-----|----------|---------|
| `PremiumReminderJob` | Daily 6 AM | Send reminder emails 30/15/7/1 days before due; idempotent |
| `GracePeriodLapseJob` | Daily 7 AM | Move Due→GracePeriod→Lapsed; skip CoverageExhausted/Cancelled |
| `PolicyExpiryJob` | Daily midnight | Mark Active policies Expired when EndDate passed |
| `SlaCheckerJob` | Daily 1 AM | Flag AtRisk (≤3 days) and Breached claims; notify manager |
| `SystemHealthCheckJob` | Every 5 min | Check API + DB; write to SystemHealthLogs; email admin if Unhealthy |

> **Critical rule for all jobs:** `WHERE Status NOT IN ('CoverageExhausted', 'Cancelled')` — these are terminal statuses and must never be overwritten by any cron job.

---

## 10. Notification System (18 Events)

| Trigger | Recipients |
|---------|-----------|
| Registration submitted | Admin |
| Registration approved/rejected | Policyholder |
| Staff account created | Staff member |
| Premium reminder (30/15/7/1 day) | Policyholder |
| Overdue notice | Policyholder |
| Lapse notice | Policyholder |
| Revival confirmed / penalty waived | Policyholder |
| Claim submitted | ClaimReviewer (assigned), Admin |
| Reviewer assigned | Reviewer |
| Documents requested | Policyholder |
| Documents uploaded | ClaimReviewer |
| Claim approved | Policyholder, FinanceOfficer |
| Claim rejected | Policyholder |
| Payment processed | Policyholder |
| SLA at risk / SLA breached | Admin, ClaimsManager |

**Delivery:** SignalR push (real-time in-app) + SMTP email (async via Hangfire) + DB record (all channels)

---

## 11. Testing Strategy

### Coverage Targets

| Layer | Target | Framework |
|-------|--------|-----------|
| Service Layer | **100%** | NUnit + Moq + FluentAssertions |
| Controllers | ≥ 80% | NUnit + WebApplicationFactory |
| Repositories | ≥ 80% | NUnit + In-Memory EF / TestContainers |
| FluentValidation | ≥ 80% | NUnit |
| ClaimStateMachine | ≥ 80% | NUnit |
| Hangfire Jobs | ≥ 80% | NUnit + Moq |
| Angular Services | **100%** | Jasmine + Karma |
| Angular Components | ≥ 80% | Jasmine + Karma |
| Angular Guards | ≥ 80% | Jasmine |
| E2E Flows | 10 flows must pass | Cypress |

### NUnit Test Pattern

```csharp
[TestFixture]
public class ClaimServiceTests
{
    private IClaimService _sut;
    private Mock<IClaimRepository> _repoMock;
    private Mock<IClaimValidationService> _validationMock;

    [SetUp]
    public void SetUp()
    {
        _repoMock       = new Mock<IClaimRepository>();
        _validationMock = new Mock<IClaimValidationService>();
        _sut = new ClaimService(_repoMock.Object, _validationMock.Object, ...);
    }

    [Test]
    public async Task SubmitClaim_WithValidPolicy_ShouldReturnSubmittedStatus()
    {
        // Arrange + Act + Assert (FluentAssertions)
        result.Status.Should().Be(ClaimStatus.Submitted);
    }

    [TestCase(PolicyStatus.Lapsed)]
    [TestCase(PolicyStatus.Expired)]
    [TestCase(PolicyStatus.CoverageExhausted)]
    public async Task SubmitClaim_WithInvalidPolicyStatus_ShouldThrow(PolicyStatus status)
    {
        await FluentActions.Invoking(() => _sut.SubmitAsync(dto, userId))
            .Should().ThrowAsync<BusinessRuleException>();
    }
}
```

---

## 12. Curriculum Coverage Map

| Topic | Where in project |
|-------|-----------------|
| **OOP** | All entity classes; inheritance from BaseEntity; encapsulation in services |
| **Generic classes** | `IRepository<T>`, `PagedResult<T>`, `ClaimFilterDto<T>` |
| **Generic collections** | `List<T>`, `Dictionary<K,V>` in ClaimStateMachine transitions map |
| **Exception handling** | Global Exception Middleware; custom domain exceptions; try-catch in services |
| **Constructor chaining** | `BaseEntity()` → `BaseEntity(Guid id)` · `Claim()` → `Claim(policyId, claimantId)` |
| **Operator overloading** | `Money` value object: `+`, `-`, `==`, `>` |
| **Extension methods** | `ClaimExtensions`: `IsOverSlaDeadline(this Claim)`, `IsEligibleForClaim(this Policy)` |
| **LINQ** | Dynamic EF Core filter queries in all Repository `GetPagedAsync()` methods |
| **EF Core / Fluent API** | `IEntityTypeConfiguration<T>` per entity; relationships, indexes, constraints |
| **Code-First** | Migrations + `AppDbContext`; `dotnet ef migrations add` |
| **Web API** | 8 controllers; RESTful conventions; versioning (`/api/v1/`) |
| **DI / DIP** | All services/repos injected via constructor; interfaces in Application layer |
| **JWT** | Custom HS256 tokens; 15 min access + 7 day refresh; rotation; family revocation |
| **Password encryption** | BCrypt cost 12 + AES-256 for PII |
| **Policy-based authorization** | `[Authorize(Policy="ClaimsManagerOnly")]` in controllers |
| **Async/Await** | Every service and repository method is async |
| **Unit testing** | NUnit `[TestFixture]`, `[Test]`, `[TestCase]`, `[SetUp]`, `[TearDown]` |
| **Moq** | All external dependencies mocked; `Mock<IClaimRepository>` etc. |
| **Serilog** | Structured logging; Console + File sinks; Correlation ID; never log PII |
| **nUnit** | Primary test framework; `[OneTimeSetUp]`, parameterised `[TestCase]` |
| **In-memory database** | `UseInMemoryDatabase()` for unit tests; TestContainers for integration |
| **Filters** | `HangfireAdminAuthFilter`; `ActionFilter` for audit logging |
| **API versioning** | All routes `/api/v1/`; `IApiVersioningBuilder` in `Program.cs` |
| **Throttling** | `Microsoft.AspNetCore.RateLimiting` — 5 named policies + global fallback |
| **Auth0** | _Design decision: Custom JWT in Phase 1; Auth0 upgrade path documented — swap only `AuthService` + `Program.cs` config_ |

---

## 13. Implementation Phases

| Phase | Name | Key Output | Depends On |
|-------|------|-----------|-----------|
| **1** | Foundation | 18 DB tables, Hangfire, Serilog, Swagger | — |
| **2** | Auth & Registration | JWT, BCrypt, refresh tokens, lockout, 2 registration flows | 1 |
| **3** | Policy Management | CRUD, premium schedule auto-generation, expiry job | 2 |
| **4** | Premium Payments | IRDAI lifecycle, grace/lapse/revival, Stripe premium | 3 |
| **5** | Nominee Management | AES-256 Aadhaar, share % validation | 3 |
| **6** | Claim Submission | 9 validations, auto-assign, SLA, state machine | 3, 4, 5 |
| **7** | Review & Approval | Document upload/verify, ClaimsManager approve/reject | 6 |
| **8** | Stripe Payments | PaymentIntent, webhook, recipient routing, idempotency | 7 |
| **9** | Notifications | SignalR hub, 18 email templates, 18 events | 6, 7, 8 |
| **10** | Reports & Health | 5 reports, CSV export, SystemHealthLogs, Hangfire secured | 6, 7, 8 |
| **11** | Angular Frontend | Full SPA, all routes, guards, interceptors, Stripe.js | 1–10 |
| **12** | Testing & Hardening | 100% service coverage, ≥80% all layers, integration test | 1–11 |

---

## 14. IRDAI Compliance Checklist

| Requirement | Implementation |
|-------------|---------------|
| Claim settlement within 30 days | `SlaDeadline = SubmittedAt + SlaDays`; daily breach check |
| Settlement ratio reporting | IRDAI Compliance Report endpoint |
| Claim register | `Claims` + `ClaimWorkflowHistory` + `AuditLogs` |
| Rejection reason documentation | Mandatory `RejectionReason` field |
| Status change timestamps | `ClaimWorkflowHistory` append-only |
| Policyholder data protection | Aadhaar AES-256 + masked `XXXX-XXXX-1234` in all responses |
| Monthly grace = 15 days | `GracePeriodDays = 15` when `PremiumFrequency = Monthly` |
| Other grace = 30 days | `GracePeriodDays = 30` for Quarterly/HalfYearly/Annually |
| Claims valid during grace | `ClaimValidationService` allows `GracePeriod` policy status |
| Revival within 2 years | `PolicyService.RevivePolicy()` checks `LapsedAt` |

---
