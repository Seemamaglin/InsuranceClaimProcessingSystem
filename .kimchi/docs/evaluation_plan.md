# Backend Evaluation Preparation Plan

## Mentor Evaluation Criteria
1. **Logging** — comprehensive entry/success/failure logs with proper levels
2. **Unit Testing** — 100% service coverage, happy path + edge cases + failure paths
3. **Coding Standards** — PascalCase, properties not fields, no bad comments
4. **Method Length** — max ~15 lines per method; break into helpers
5. **Exception Handling** — proper, no swallowing, specific catches
6. **SOLID Principles** — dependency inversion, single responsibility
7. **Design Patterns** — repository, unit of work, result, state machine

---

## Chunk 1 — AuthService Refactoring
- Break all public methods into private helpers ≤15 lines
- Add entry/success/failure logging to every public method

## Chunk 2 — ClaimService + ClaimValidationService Refactoring
- Break long methods into private helpers
- Add comprehensive logging
- Extract: GenerateClaimNumber, BuildClaimEntity, BuildWorkflowEntry, HandleApprovedStatus, HandleClosedStatus, FindBestReviewer
- Extract from ClaimValidationService: ValidatePolicyAsync, ValidateClaimTypeAsync, ValidateClaimantAsync, ValidateCoverageAsync

## Chunk 3 — PaymentService, DocumentService, AccountService, NotificationService, PremiumPaymentService Refactoring
- Break long methods into private helpers
- Add comprehensive logging
- Fix AccountService.MapToAccountDto to use strongly-typed User instead of dynamic

## Chunk 4 — Add Missing Unit Tests (Part 1)
- AccountServiceTests: get, update profile, update password, deactivate, delete, reactivate
- PaymentServiceTests: create intent, confirm payment, get payment
- PremiumPaymentServiceTests: record first premium, record premium, get last payment

## Chunk 5 — Add Missing Unit Tests (Part 2)
- DocumentServiceTests: upload, download, verify, delete
- NotificationServiceTests: create, get, mark read, mark all read
- ClaimStateMachineTests: all valid transitions, all invalid transitions, terminal state

## Chunk 6 — Controller Logging + Coding Standards + Build Verification
- Add ILogger and logging to all controllers
- Remove bad comments from User.cs
- Ensure `dotnet build` succeeds
- Ensure `dotnet test` passes
