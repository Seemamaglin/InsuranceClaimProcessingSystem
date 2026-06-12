# Auth Security Fixes Plan

## Mentor Criteria Gaps Found

### Registration (Criteria 6-11)
- ✅ `RegisterRequest` has `UserRole Role` property
- ❌ `AuthService.BuildUserEntity` IGNORES `request.Role` and hardcodes `UserRole.PolicyHolder`
- ❌ NO explicit server-side validation that Role is in allowed set
- ❌ NO explicit rejection of privileged roles (Admin, ClaimsManager, ClaimReviewer, FinanceOfficer)
- ❌ Role is NOT validated before persistence

### Login (Criteria 12-15)
- ✅ Login only authenticates credentials
- ✅ Role fetched from DB once (`FindUserByEmailOrUsernameAsync`)
- ✅ JWT includes role claim (`ClaimTypes.Role`)

### Authorization (Criteria 16-19)
- ✅ Uses `[Authorize(Policy = "...")]` with `.RequireRole()`
- ✅ No DB role lookups per request
- ✅ JWT signed and validated

### Token Design (Criteria 20-22)
- ✅ Includes UserId, Email, Role, Expiry
- ✅ Short-lived (15 min)
- ✅ Stateless

### Security Rules (Criteria 23-25)
- ❌ No explicit validation that client-provided roles are trusted
- ❌ No explicit rejection of role escalation attempts
- ✅ Auth logic in attributes, not controllers/services

---

## Fixes Required

### Fix 1: AuthService.RegisterAsync — Explicit Role Validation
**File**: `src/InsuranceClaimSystem.Infrastructure/Services/Auth/AuthService.cs`

1. Extract `ValidateRegistrationRole` private method that:
   - Receives `UserRole requestedRole`
   - Returns `Result<UserRole>`
   - Allowed roles for public registration: `PolicyHolder` ONLY
   - If requested role is privileged (Admin, ClaimsManager, ClaimReviewer, FinanceOfficer): return failure
   - If requested role is invalid/unknown: return failure
   - Otherwise return validated role

2. In `RegisterAsync`, BEFORE `BuildUserEntity`:
   ```csharp
   var roleValidation = ValidateRegistrationRole(request.Role);
   if (roleValidation.IsFailure) return Result<AuthResponse>.Failure(roleValidation.Error);
   ```

3. In `BuildUserEntity`, use `roleValidation.Value` instead of hardcoded `UserRole.PolicyHolder`:
   ```csharp
   Role = validatedRole,
   ```

### Fix 2: RegisterRequestValidator — Role Validation Rule
**File**: `src/InsuranceClaimSystem.Application/Validators/RegisterRequestValidator.cs`

Add rule:
```csharp
RuleFor(x => x.Role)
    .IsInEnum()
    .WithMessage("Invalid role specified.");
```

### Fix 3: AuthServiceTests — Role Validation Test Cases
**File**: `src/InsuranceClaimSystem.Tests/UnitTests/Services/AuthServiceTests.cs`

Add tests:
1. `Register_WithAllowedRole_ShouldCreateUserWithThatRole` — PolicyHolder → success with PolicyHolder role
2. `Register_WithAdminRole_ShouldReturnFailure` — Admin → rejected
3. `Register_WithClaimsManagerRole_ShouldReturnFailure` — ClaimsManager → rejected
4. `Register_WithClaimReviewerRole_ShouldReturnFailure` — ClaimReviewer → rejected
5. `Register_WithFinanceOfficerRole_ShouldReturnFailure` — FinanceOfficer → rejected
6. `Register_WithoutRole_ShouldDefaultToPolicyHolder` — if Role is default(0), validate behavior

### Fix 4: Coding Standards
- Remove any leftover `//creates one to many relationships` style comments
- Ensure `ValidateRegistrationRole` is ≤15 lines

---

## Acceptance Criteria
- [ ] `dotnet build` passes with 0 errors
- [ ] `dotnet test` passes with all tests including new role validation tests
- [ ] RegisterRequest.Role is validated server-side in AuthService
- [ ] Privileged roles are explicitly rejected during registration
- [ ] Validated role is persisted in Users table
- [ ] Role validation has dedicated unit tests
