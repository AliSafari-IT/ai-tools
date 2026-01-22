# Implementation Guide - Subscription, API Keys, Upload Flow

## COMMANDS TO RUN

### 1. Database Migration

```bash
cd d:\repos\ai-tools
dotnet ef migrations add AddSubscriptionAndProviderTracking --project src/LogCopilot.Infrastructure --startup-project src/LogCopilot.Api
dotnet ef database update --project src/LogCopilot.Infrastructure --startup-project src/LogCopilot.Api
```

### 2. Register Services in Program.cs

Add to `src/LogCopilot.Api/Program.cs` before `builder.Build()`:

```csharp
builder.Services.AddScoped<IBillingService, BillingService>();
builder.Services.AddScoped<IApiKeyService, ApiKeyService>();
```

### 3. Seed Default Billing Plans

Run SQL in PostgreSQL:

```sql
INSERT INTO "BillingPlans" ("Id", "Name", "Description", "MonthlyPrice", "MaxUsers", "MaxStorageBytes", "MaxUploadSessionsPerMonth", "EnablesAiReports", "EnablesSemanticClustering", "IsActive", "CreatedAt", "UpdatedAt")
VALUES 
  (gen_random_uuid(), 'Community', 'Free tier with basic features', 0, 5, 1073741824, 50, false, false, true, NOW(), NOW()),
  (gen_random_uuid(), 'Pro', 'Professional tier with AI reports', 49, 20, 10737418240, 500, true, true, true, NOW(), NOW());
```

### 4. Frontend Build

```bash
cd frontend
pnpm install
pnpm run dev
```

## VERIFICATION STEPS

### A. Upload Flow Completion

1. Navigate to <http://localhost:5173/upload>
2. Upload a log file (e.g., 4003 lines)
3. **Expected**: Progress bar shows 0-100%, then displays "Completed" with:
   - Summary: "4003 events processed, X errors, Y warnings"
   - "What Next?" panel with buttons:
     - View Logs
     - View Clusters
     - View Traces
     - Generate Report
     - Upload Another
4. Click "View Logs" → should navigate to /logs with session filter
5. Click "Generate Report" → should create report and show success

### B. Provider Display (OpenAI vs Community)

1. Go to <http://localhost:5173/reports>
2. Click "Generate Report" for a time range
3. **Expected**:
   - If OpenAI API key is valid: "Provider: OpenAI"
   - If OpenAI fails: "Provider: OpenAI (fallback → Community)"
   - If no Pro plan: "Provider: Community"
4. Check report modal shows correct provider status

### C. Billing Plan Enforcement

1. Login as admin
2. Go to /settings/billing (need to create this page)
3. **Expected**: Shows current plan (Community or Pro)
4. Shows usage: X/Y users, X/Y GB storage, X/Y uploads this month
5. Try adding 6th user when on Community (max 5) → should fail
6. Try uploading when quota exceeded → should fail with message

### D. API Keys

1. Login as admin
2. Go to /settings/api-keys (need to create this page)
3. Click "Create API Key"
4. Enter name: "Test Key", scopes: ["logs:read", "reports:read"]
5. **Expected**: Shows raw key ONCE (e.g., "lc_abc123...")
6. Copy key
7. Test with curl:

```bash
curl -H "X-Api-Key: lc_abc123..." http://localhost:5000/api/logs
```

8. **Expected**: Returns logs for that organization
2. Deactivate key → subsequent requests fail with 401

### E. Admin Role Management

1. Login as admin
2. Go to /settings/members (need to create this page)
3. **Expected**: Lists all organization members with roles
4. Change a member's role from Member → Admin
5. Try to remove last admin → should fail with error
6. Logout and login as new admin → should see admin menu

## REMAINING WORK

### Frontend Pages to Create

1. **UploadCompletionPanel.tsx** - "What Next?" UI after upload
2. **SettingsLayout.tsx** - Admin settings navigation
3. **BillingPage.tsx** - Plan info and usage
4. **ApiKeysPage.tsx** - API key management
5. **MembersPage.tsx** - Role management (already exists in AdminController)

### Backend Endpoints to Add

1. **GET /api/logs/status/{sessionId}** - Fix to return consistent phase/progress
2. **POST /api/ai/test** - Test AI provider connectivity
3. **PUT /api/admin/members/{id}/role** - Already exists in AdminController

### Tests to Add

1. **BillingServiceTests.cs** - Plan limits enforcement
2. **ApiKeyServiceTests.cs** - Key generation, validation, scopes
3. **UploadFlowTests.tsx** - State transitions
4. **ProviderTrackingTests.cs** - Fallback behavior

## QUICK FIX FOR IMMEDIATE ISSUES

### Issue 1: Upload stuck at "Processing logs... 0%"

**Root cause**: Frontend polling doesn't detect completion properly
**Fix applied**: Changed to check `data.isComplete` flag from backend
**Verify**: Upload a file, should complete and clear progress bar

### Issue 2: Provider shows "Community" despite Pro license

**Root cause**: OpenAI JSON parsing fails, falls back to Community
**Fix applied**:

- Added robust JSON extraction (strips markdown fences)
- Added `response_format: json_object` to OpenAI request
- Added provider tracking fields to IncidentReport
**Verify**: Generate new report, should show "Provider: OpenAI" or "Provider: OpenAI (fallback → Community)" with reason

## ARCHITECTURE NOTES

### Billing Plan Flow

1. Organization has optional BillingPlanId
2. BillingService checks limits before operations
3. Counters reset monthly automatically
4. Feature flags control AI reports and semantic clustering

### API Key Flow

1. Raw key generated: "lc_" + base64(32 bytes)
2. Hash stored in DB (SHA256)
3. Prefix stored for quick lookup
4. Scopes: comma-separated string
5. Middleware validates on each request with X-Api-Key header

### Provider Tracking

1. RequestedProvider: What user/plan wanted (OpenAI or Community)
2. ActualProvider: What actually generated the report
3. ProviderStatus: Success | Fallback | Error
4. ProviderErrorSummary: Redacted error message for UI

### Upload State Machine

```
Idle → Uploading → Queued → Processing → Completed
                                      ↓
                                   Failed/Canceled
```
