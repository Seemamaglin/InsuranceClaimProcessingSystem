# Insurance Claim Processing System — Live Demo Guide

> **Target Audience:** Mentor evaluation
> **Environment:** Localhost (`http://localhost:5050`)
> **Tools:** Postman + Swagger UI (`http://localhost:5050/swagger`)

---

## 1. DEMO STRATEGY

### The Story

> *"We will walk through a complete real-world insurance lifecycle: a new customer registers, purchases a policy, submits a claim after an incident, and the claim is reviewed, approved, paid, and closed — all with full audit trails and role-based security."*

### Roles You Will Impersonate

| Role | Postman Variable | Why They Matter |
|------|------------------|-----------------|
| **System Admin** | `accessToken_Admin` | Approves users and policies |
| **Policyholder** | `accessToken_Policyholder` | Buys policy, submits claim, uploads docs |
| **Claim Reviewer** | `accessToken_Reviewer` | Verifies documents, underwrites claim |
| **Claims Manager** | `accessToken_Manager` | Final approval/rejection |
| **Finance Officer** | `accessToken_Finance` | Handles Stripe payment |

### Why This Flow Proves Production-Readiness

1. ** RBAC & JWT** — Every endpoint is gated by policies (`AdminOnly`, `PolicyHolderOnly`, `ReviewerOrManager`, `FinanceOfficerOnly`)
2. **State Machine** — Claims move through `Submitted → DocumentsPending → UnderReview → Approved → Closed`
3. **Business Rules Engine** — Waiting periods, co-pay, deductible, sub-limit validations
4. **Audit Trail** — `ClaimWorkflowHistory` captures every state change with `PreviousStatus`, `NewStatus`, `ChangedByUserId`
5. **Payment Integration** — Stripe PaymentIntent with idempotency keys
6. **Background Jobs** — Hangfire handles policy expiry and grace-period lapse
7. **Notifications** — In-app notifications triggered on key events
8. **Soft Delete / CQRS-free N-Tier** — Clean separation of Domain, Application, Infrastructure, API

---

## 2. POSTMAN ENVIRONMENT SETUP

Create a Postman Environment named `InsuranceClaimSystem_Local` with these variables:

| Variable | Initial Value | Description |
|----------|---------------|-------------|
| `baseUrl` | `http://localhost:5050` | API base |
| `accessToken_Admin` | *(empty)* | Admin JWT |
| `accessToken_Policyholder` | *(empty)* | Policyholder JWT |
| `accessToken_Reviewer` | *(empty)* | Reviewer JWT |
| `accessToken_Manager` | *(empty)* | Manager JWT |
| `accessToken_Finance` | *(empty)* | Finance JWT |
| `userId_Policyholder` | *(empty)* | Created policyholder GUID |
| `userId_Reviewer` | *(empty)* | Created reviewer GUID |
| `policyId` | *(empty)* | Created policy GUID |
| `claimId` | *(empty)* | Created claim GUID |
| `claimNumber` | *(empty)* | Created claim number |
| `policyNumber` | *(empty)* | Created policy number |
| `documentId` | *(empty)* | Uploaded document GUID |

### Storing JWT Tokens After Login

In Postman **Tests** tab of every Login request, add:

```javascript
var jsonData = pm.response.json();
pm.environment.set("accessToken_Policyholder", jsonData.accessToken);
// Change variable name per role
```

Then set the **Authorization** tab of subsequent requests to:
- Type: `Bearer Token`
- Token: `{{accessToken_Policyholder}}` (or the relevant variable)

---

## 3. STEP-BY-STEP DEMO FLOW

### PRE-DEMO CHECKLIST

Run these in a terminal before starting:

```bash
cd /Users/seemamaglin/Documents/Genspark-assignments/Capstone/InsuranceClaimSystem/src/InsuranceClaimSystem.API
dotnet run
```

Open Swagger at `http://localhost:5050/swagger` and verify:
- ✅ All controllers listed
- ✅ JWT login icon (lock icon) visible on protected endpoints

---

### STEP 1: Admin Login (Seeded Admin)

> **What to say:** *"Our system ships with a seeded admin so we can start managing users immediately. Let me log in."*

| Field | Value |
|-------|-------|
| **Endpoint** | `POST {{baseUrl}}/api/v1/auth/login` |
| **Method** | POST |
| **Headers** | `Content-Type: application/json` |
| **Body** | See below |
| **Role** | None (public endpoint) |

**Request Body:**
```json
{
  "emailOrUsername": "admin@insuranceclaimsystem.com",
  "password": "Admin@123!"
}
```

**Expected Response:**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2g...",
  "expiresIn": 900,
  "tokenType": "Bearer",
  "user": {
    "id": "...",
    "email": "admin@insuranceclaimsystem.com",
    "role": "Admin"
  }
}
```

**Postman Test Script:**
```javascript
var jsonData = pm.response.json();
pm.environment.set("accessToken_Admin", jsonData.accessToken);
```

**Common Mistake to Avoid:**
- Do NOT use the seeded admin for policyholder actions later — that breaks the role-based demo story.

---

### STEP 2: Register a New Policyholder

> **What to say:** *"Now let a real customer register. The system enforces email verification and sends the account to admin approval before activation."*

| Field | Value |
|-------|-------|
| **Endpoint** | `POST {{baseUrl}}/api/v1/auth/register` |
| **Method** | POST |
| **Headers** | `Content-Type: application/json` |
| **Role** | None (public endpoint) |

**Request Body:**
```json
{
  "firstName": "Rahul",
  "lastName": "Sharma",
  "dateOfBirth": "1985-03-15T00:00:00Z",
  "email": "rahul.sharma@example.com",
  "userName": "rahulsharma",
  "password": "Rahul@2025!",
  "confirmPassword": "Rahul@2025!",
  "phoneNumber": "+91-9876543210"
}
```

**Expected Response:**
```json
{
  "id": "<user-guid>",
  "email": "rahul.sharma@example.com",
  "username": "rahulsharma",
  "registrationStatus": "PendingApproval",
  "isActive": false
}
```

**Note:** Save the returned `id` to `{{userId_Policyholder}}`.

---

### STEP 3: Admin Approves the Policyholder

> **What to say:** *"Admin reviews pending registrations and activates the user. This is our first RBAC gate."*

| Field | Value |
|-------|-------|
| **Endpoint** | `POST {{baseUrl}}/api/v1/admin/registrations/{{userId_Policyholder}}/approve` |
| **Method** | POST |
| **Headers** | `Authorization: Bearer {{accessToken_Admin}}` |
| **Role** | Admin |

**Expected Response:**
```json
{
  "success": true,
  "message": "Registration approved successfully."
}
```

**Verification:** Call `GET {{baseUrl}}/api/v1/admin/registrations/pending` → should return empty list.

---

### STEP 4: Policyholder Logs In

| Field | Value |
|-------|-------|
| **Endpoint** | `POST {{baseUrl}}/api/v1/auth/login` |
| **Method** | POST |
| **Body** | Same as Step 1 but with policyholder credentials |
| **Role** | None (public endpoint) |

**Request Body:**
```json
{
  "emailOrUsername": "rahul.sharma@example.com",
  "password": "Rahul@2025!"
}
```

**Postman Test Script:**
```javascript
var jsonData = pm.response.json();
pm.environment.set("accessToken_Policyholder", jsonData.accessToken);
```

---

### STEP 5: Admin Creates a Policy (Health)

> **What to say:** *"Admin creates a premium health policy for the newly approved customer. Coverage is ₹5,00,000 with a 30-day waiting period."*

| Field | Value |
|-------|-------|
| **Endpoint** | `POST {{baseUrl}}/api/v1/policy` |
| **Method** | POST |
| **Headers** | `Authorization: Bearer {{accessToken_Admin}}`, `Content-Type: application/json` |
| **Role** | Admin |

**Request Body:**
```json
{
  "policyHolderId": "{{userId_Policyholder}}",
  "policyTypeId": "<health-policy-type-id>",
  "startDate": "2025-06-01T00:00:00Z",
  "endDate": "2026-06-01T00:00:00Z",
  "coverageAmount": 500000,
  "premiumAmount": 12000,
  "premiumFrequency": "Monthly"
}
```

> **Tip:** Get `policyTypeId` from `GET {{baseUrl}}/api/v1/policy-types` (Health type).

**Expected Response:**
```json
{
  "id": "<policy-guid>",
  "policyNumber": "POL-2025-0001",
  "status": "PendingApproval",
  "coverageAmount": 500000,
  "remainingCoverageAmount": 500000
}
```

**Postman Test Script:**
```javascript
var jsonData = pm.response.json();
pm.environment.set("policyId", jsonData.id);
pm.environment.set("policyNumber", jsonData.policyNumber);
```

---

### STEP 6: Admin Approves the Policy

> **What to say:** *"Policy approval activates coverage. Notice the status changes to Active and the remaining coverage is initialized."*

| Field | Value |
|-------|-------|
| **Endpoint** | `POST {{baseUrl}}/api/v1/policy/{{policyId}}/approve` |
| **Method** | POST |
| **Headers** | `Authorization: Bearer {{accessToken_Admin}}` |
| **Role** | Admin |

**Expected Response:**
```json
{
  "id": "<policy-guid>",
  "status": "Active",
  "remainingCoverageAmount": 500000
}
```

---

### STEP 7: Policyholder Submits a Claim

> **What to say:** *"45 days later, the customer is hospitalized. He submits a claim with all incident details. Our validation engine checks policy status, waiting period, duplicate open claims, and benefit rules in one go."*

| Field | Value |
|-------|-------|
| **Endpoint** | `POST {{baseUrl}}/api/v1/claims` |
| **Method** | POST |
| **Headers** | `Authorization: Bearer {{accessToken_Policyholder}}`, `Content-Type: application/json` |
| **Role** | PolicyHolder |

**Request Body:**
```json
{
  "policyId": "{{policyId}}",
  "claimTypeId": "<hospitalization-claim-type-id>",
  "incidentDate": "2025-07-15T00:00:00Z",
  "incidentDescription": "Emergency appendix surgery at City Hospital",
  "incidentLocation": "City Hospital, Delhi",
  "claimedAmount": 75000,
  "claimantType": "Policyholder"
}
```

> **Tip:** Get `claimTypeId` from `GET {{baseUrl}}/api/v1/policy-types` → find Hospitalization under Health.

**Expected Response:**
```json
{
  "id": "<claim-guid>",
  "claimNumber": "CLM-2025-0001",
  "status": "Submitted",
  "policyId": "<policy-guid>",
  "claimedAmount": 75000,
  "finalPayableAmount": 0
}
```

**Postman Test Script:**
```javascript
var jsonData = pm.response.json();
pm.environment.set("claimId", jsonData.id);
pm.environment.set("claimNumber", jsonData.claimNumber);
```

---

### STEP 8: Register & Login a Claim Reviewer

> **What to say:** *"We need a reviewer to handle this claim. Let me create one."*

Register a reviewer (same as Step 2 but different role):

**Request Body (Register Reviewer):**
```json
{
  "firstName": "Priya",
  "lastName": "Patel",
  "dateOfBirth": "1988-07-20T00:00:00Z",
  "email": "priya.patel@reviewer.com",
  "userName": "priyapatel",
  "password": "Priya@2025!",
  "confirmPassword": "Priya@2025!",
  "phoneNumber": "+91-9123456789"
}
```

Admin approves reviewer (same as Step 3). Save reviewer ID as `{{userId_Reviewer}}`.

Login as reviewer:

```javascript
var jsonData = pm.response.json();
pm.environment.set("accessToken_Reviewer", jsonData.accessToken);
```

---

### STEP 9: Manager Auto-Assigns Reviewer to Claim

> **What to say:** *"The Claims Manager assigns the reviewer to this claim."*

| Field | Value |
|-------|-------|
| **Endpoint** | `POST {{baseUrl}}/api/v1/claims/{{claimId}}/auto-assign` |
| **Method** | POST |
| **Headers** | `Authorization: Bearer {{accessToken_Manager}}` |
| **Role** | Manager / Admin |

**Expected Response:**
```json
{
  "success": true,
  "message": "Reviewer assigned successfully."
}
```

> **Note:** If you don't have a Manager token yet, create one similar to the reviewer (role = ClaimsManager) or use the Admin token (Admin has ManagerOrAdmin policy).

---

### STEP 10: Reviewer Requests Documents

> **What to say:** *"The reviewer finds some documents missing and requests them. The system updates the claim status to DocumentsPending and notifies the policyholder."*

| Field | Value |
|-------|-------|
| **Endpoint** | `POST {{baseUrl}}/api/v1/reviewers/claims/{{claimId}}/request-documents` |
| **Method** | POST |
| **Headers** | `Authorization: Bearer {{accessToken_Reviewer}}`, `Content-Type: application/json` |
| **Role** | ClaimReviewer |

**Request Body:**
```json
{
  "reviewerId": "{{userId_Reviewer}}",
  "message": "Please upload hospital discharge summary and original bills."
}
```

**Expected Response:**
```json
{
  "message": "Document request sent successfully"
}
```

**Verify Status:** `GET {{baseUrl}}/api/v1/claims/{{claimId}}` → Status should be `DocumentsPending`.

---

### STEP 11: Policyholder Uploads Documents

> **What to say:** *"The policyholder uploads the requested documents via multipart/form-data."*

| Field | Value |
|-------|-------|
| **Endpoint** | `POST {{baseUrl}}/api/v1/documents/upload?claimId={{claimId}}&uploadedByUserId={{userId_Policyholder}}&documentType=MedicalReport` |
| **Method** | POST |
| **Headers** | `Authorization: Bearer {{accessToken_Policyholder}}`, `Content-Type: multipart/form-data` |
| **Role** | PolicyHolder |
| **Body** | Form-Data Key: `file`, Value: select a sample PDF/image |

**Expected Response:**
```json
{
  "id": "<doc-guid>",
  "fileName": "discharge_summary.pdf",
  "documentType": "MedicalReport",
  "verificationStatus": "Pending"
}
```

**Postman Test Script:**
```javascript
var jsonData = pm.response.json();
pm.environment.set("documentId", jsonData.id);
```

---

### STEP 12: Reviewer Verifies Documents

> **What to say:** *"Reviewer verifies the uploaded documents. Once verified, the claim moves to UnderReview automatically."*

| Field | Value |
|-------|-------|
| **Endpoint** | `POST {{baseUrl}}/api/v1/reviewers/claims/{{claimId}}/verify-documents` |
| **Method** | POST |
| **Headers** | `Authorization: Bearer {{accessToken_Reviewer}}`, `Content-Type: application/json` |
| **Role** | ClaimReviewer |

**Request Body:**
```json
{
  "reviewerId": "{{userId_Reviewer}}",
  "documentVerifications": [
    {
      "documentId": "{{documentId}}",
      "status": "Approved",
      "rejectionReason": null
    }
  ]
}
```

**Expected Response:**
```json
{
  "message": "Documents verified and claim updated"
}
```

**Verify Status:** `GET {{baseUrl}}/api/v1/claims/{{claimId}}` → Status should be `UnderReview`.

---

### STEP 13: Claims Manager Approves the Claim

> **What to say:** *"The Claims Manager gives final approval. The system calculates the final payable amount considering co-pay, deductible, and sub-limit from the benefit rule."*

| Field | Value |
|-------|-------|
| **Endpoint** | `PATCH {{baseUrl}}/api/v1/claims/{{claimId}}/status` |
| **Method** | PATCH |
| **Headers** | `Authorization: Bearer {{accessToken_Manager}}`, `Content-Type: application/json` |
| **Role** | ClaimsManager (or ReviewerOrManager) |

**Request Body:**
```json
{
  "newStatus": "Approved",
  "changedByUserId": "{{userId_Manager}}"
}
```

**Expected Response:**
```json
{
  "success": true,
  "message": "Status updated to Approved"
}
```

**Verify:** `GET {{baseUrl}}/api/v1/claims/{{claimId}}` → Status: `Approved`, `finalPayableAmount`: calculated value (e.g., 67500 after 10% co-pay + ₹1000 deductible).

---

### STEP 14: Finance Officer Creates Stripe Payment Intent

> **What to say:** *"Finance initiates payment via Stripe. The system creates a PaymentIntent with idempotency to prevent duplicate charges."*

| Field | Value |
|-------|-------|
| **Endpoint** | `POST {{baseUrl}}/api/v1/payments/{{claimId}}/create-intent` |
| **Method** | POST |
| **Headers** | `Authorization: Bearer {{accessToken_Finance}}` |
| **Role** | FinanceOfficer |

**Expected Response:**
```json
{
  "paymentIntentId": "pi_3O..."
}
```

---

### STEP 15: Finance Officer Confirms Payment

> **What to say:** *"Payment is confirmed. The claim closes automatically, coverage decrements, and a notification is sent."*

| Field | Value |
|-------|-------|
| **Endpoint** | `POST {{baseUrl}}/api/v1/payments/{{claimId}}/confirm` |
| **Method** | POST |
| **Headers** | `Authorization: Bearer {{accessToken_Finance}}`, `Content-Type: application/json` |
| **Role** | FinanceOfficer |

**Request Body:**
```json
{
  "paymentIntentId": "pi_3O..."
}
```

**Expected Response:**
```json
{
  "success": true
}
```

---

### STEP 16: Verify Claim is Closed & Coverage Decremented

> **What to say:** *"Let's verify the full lifecycle impact."*

| Field | Value |
|-------|-------|
| **Endpoint** | `GET {{baseUrl}}/api/v1/claims/{{claimId}}` |
| **Method** | GET |
| **Headers** | `Authorization: Bearer {{accessToken_Policyholder}}` |

**Expected:** Status = `Closed`, `resolvedAt` not null.

| Field | Value |
|-------|-------|
| **Endpoint** | `GET {{baseUrl}}/api/v1/policy/{{policyId}}` |
| **Method** | GET |
| **Headers** | `Authorization: Bearer {{accessToken_Policyholder}}` |

**Expected:** `remainingCoverageAmount` < `coverageAmount`

---

### STEP 17: Show Claim Workflow History + Notifications

> **What to say:** *"Every action is auditable and every stakeholder is notified."*

#### Workflow History

| Field | Value |
|-------|-------|
| **Endpoint** | `GET {{baseUrl}}/api/v1/claims/{{claimId}}` |
| **Method** | GET |
| **Headers** | `Authorization: Bearer {{accessToken_Policyholder}}` |

Look for `workflowHistory` array in the response showing all transitions.

#### Notifications

Login as the policyholder and check notifications:

| Field | Value |
|-------|-------|
| **Endpoint** | `GET {{baseUrl}}/api/v1/notifications` |
| **Method** | GET |
| **Headers** | `Authorization: Bearer {{accessToken_Policyholder}}` |

**Expected:** Notifications for:
- Claim submitted
- Document request
- Claim approved
- Payment processed

---

## 4. ERROR HANDLING DEMO (Optional but Impressive)

### Demo A: Invalid Claim Submission (Policy Not Active)

Try submitting a claim against a `PendingApproval` policy → **400 ValidationFailed** via `ClaimValidationService`.

```json
{
  "error": {
    "code": "ValidationFailed",
    "description": "Policy is not active. Current status: PendingApproval"
  }
}
```

**Handled by:** `ClaimValidationService.ValidateSubmissionAsync` → throws `BusinessRuleException` → caught by `ClaimService.SubmitClaimAsync` → returned as `Result<T>.Failure` → `ClaimsController` returns `BadRequest(result.Error)`.

### Demo B: Unauthorized Access

Call `POST /api/v1/policy` with a **Policyholder** token:

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.3",
  "title": "Forbidden",
  "status": 403,
  "detail": "Policy AdminOnly failed."
}
```

**Handled by:** ASP.NET Core Authorization middleware + `[Authorize(Policy = "AdminOnly")]`.

### Demo C: Validation Failure (Missing Rejection Reason)

Call `POST /api/v1/policy/{id}/reject` with empty body:

```json
{
  "error": {
    "code": "ValidationFailed",
    "description": "Rejection reason is required."
  }
}
```

**Handled by:** FluentValidation + `GlobalExceptionMiddleware` producing consistent error envelope.

---

## 5. FINAL DEMO CHECKLIST

### Before Demo

- [ ] Run `dotnet run` on `InsuranceClaimSystem.API`
- [ ] Verify Swagger loads at `http://localhost:5050/swagger`
- [ ] Reset database: `dotnet ef database drop -f && dotnet ef database update` (if you want clean seed)
- [ ] Confirm Postman Environment variables are empty (fresh start)
- [ ] Open Postman Collection with all endpoints pre-loaded
- [ ] Have a sample PDF or image ready for document upload

### Data That Must Exist (Seeded)

| Entity | How to Verify |
|--------|---------------|
| Admin user | Login with `admin@insuranceclaimsystem.com` / `Admin@123!` |
| PolicyTypes (Health, Auto, Life, Property) | `GET /api/v1/policy-types` |
| ClaimTypes (Hospitalization, Accident, Death, Fire…) | Included in above |
| BenefitRules | Look up success responses in claim validation |

### During Demo

#### If Time is Short — Skip These:
1. Forgot/Reset password flow
2. Email verification flow
3. Multiple document uploads
4. Policy rejection demo
5. Deactivation/reactivation of accounts

#### What NEVER to Skip:
1. Admin login
2. Policyholder registration + approval
3. Policy creation + approval
4. Claim submission + validation
5. Status transitions (Submitted → DocumentsPending → UnderReview → Approved → Closed)
6. Payment creation + confirmation
7. Coverage decrement verification

#### What NOT to Touch:
- **Do NOT** delete the seeded admin
- **Do NOT** delete policy types or claim types
- **Do NOT** run database migrations during demo
- **Do NOT** call `DELETE /api/v1/policy/{id}` on an active policy (it may fail due to existing claims)

---

## Appendix: Quick Reference — All Key Endpoints

| # | Action | Endpoint | Method | Auth Policy |
|---|--------|----------|--------|-------------|
| 1 | Register | `/api/v1/auth/register` | POST | Public |
| 2 | Login | `/api/v1/auth/login` | POST | Public |
| 3 | Approve Registration | `/api/v1/admin/registrations/{id}/approve` | POST | AdminOnly |
| 4 | List Policy Types | `/api/v1/policy-types` | GET | Authenticated |
| 5 | Create Policy | `/api/v1/policy` | POST | AdminOnly |
| 6 | Approve Policy | `/api/v1/policy/{id}/approve` | POST | AdminOnly |
| 7 | Submit Claim | `/api/v1/claims` | POST | PolicyHolderOnly |
| 8 | Get Claim by ID | `/api/v1/claims/{id}` | GET | Authenticated |
| 9 | Auto-Assign Reviewer | `/api/v1/claims/{id}/auto-assign` | POST | ManagerOrAdmin |
| 10 | Request Documents | `/api/v1/reviewers/claims/{id}/request-documents` | POST | ClaimReviewerOnly |
| 11 | Upload Document | `/api/v1/documents/upload` | POST | PolicyHolderOnly |
| 12 | Verify Documents | `/api/v1/reviewers/claims/{id}/verify-documents` | POST | ClaimReviewerOnly |
| 13 | Update Claim Status | `/api/v1/claims/{id}/status` | PATCH | ReviewerOrManager |
| 14 | Create Payment Intent | `/api/v1/payments/{claimId}/create-intent` | POST | FinanceOfficerOnly |
| 15 | Confirm Payment | `/api/v1/payments/{claimId}/confirm` | POST | FinanceOfficerOnly |
| 16 | Get Notifications | `/api/v1/notifications` | GET | Authenticated |

---

*Good luck with your demo! If your mentor asks about how background jobs or notifications work, point them to the Hangfire Dashboard at `http://localhost:5050/hangfire`.*
