# Log Copilot

Production-ready log analysis platform with AI-powered incident reporting and Open-Core / Pro-Plugin architecture. Upload Serilog JSON logs and Nginx logs, automatically cluster errors, reconstruct request traces, generate AI-assisted incident reports, and export in multiple formats with sensitive data redaction.

## Tech Stack

**Backend:**

- ASP.NET Core 8 Web API
- PostgreSQL with EF Core
- JWT Authentication
- Serilog for observability
- BCrypt for password hashing
- Plugin architecture for extensibility

**Frontend:**

- React 18 + TypeScript
- Vite
- React Router
- TanStack Query
- CSS Modules

**Infrastructure:**

- Docker Compose
- Nginx (production frontend)

## Features

**Community Edition (Always Free):**

- Multi-tenant user system with role-based access (Admin/Member)
- Upload and parse Serilog JSONL, Nginx access/error logs
- Automatic error clustering by exception signature
- Request trace reconstruction via TraceId/CorrelationId
- Heuristic-based incident report generation with metrics and analysis
- Tenant isolation at database level
- Secure defaults with JWT + refresh tokens
- Export reports in Markdown, JSON, and HTML formats
- Server-side sensitive data redaction (JWT tokens, API keys, emails, passwords)
- Agent prompt generation for AI assistants (instruction-only, no code blocks)

**Pro Edition (Plugin-Based):**

- OpenAI-powered narrative generation for incident reports
- Semantic clustering strategy (pluggable)
- Webhook integration sinks for report distribution
- Advanced feature flags and license gating
- Extensible plugin architecture for custom implementations

## Quick Start

### Prerequisites

- .NET 8 SDK
- Node.js 20+
- PostgreSQL 16 (or use Docker Compose)
- Docker (optional, for containerized deployment)

### Development Setup

**1. Clone and navigate:**

```bash
cd D:\repos\ai-tools
```

**2. Start PostgreSQL (via Docker):**

```bash
docker run -d --name logcopilot-postgres -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=logcopilot -p 5432:5432 postgres:16
```

**3. Run migrations:**

```bash
cd src/LogCopilot.Api
dotnet ef migrations add InitialCreate --project ../LogCopilot.Infrastructure/LogCopilot.Infrastructure.csproj --output-dir Data/Migrations
dotnet ef database update --project ../LogCopilot.Infrastructure/LogCopilot.Infrastructure.csproj
```

**4. Start backend:**

```bash
cd src/LogCopilot.Api
dotnet run
```

Backend runs on <http://localhost:5000>

**5. Install frontend dependencies and start:**

```bash
cd frontend
npm install
npm run dev
```

Frontend runs on <http://localhost:5173>

**6. Register first user:**
Navigate to <http://localhost:5173/register> and create an account. First user becomes admin of their organization.

### Docker Compose Deployment

```bash
docker-compose up --build
```

Services:

- Frontend: <http://localhost:3000>
- Backend API: <http://localhost:5000>
- PostgreSQL: localhost:5432

## Configuration

### Backend (appsettings.json)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=logcopilot;Username=postgres;Password=postgres"
  },
  "Jwt": {
    "Key": "YourSuperSecretKeyThatIsAtLeast32CharactersLong!",
    "Issuer": "LogCopilot",
    "Audience": "LogCopilotClient"
  },
  "Storage": {
    "LocalPath": "uploads"
  },
  "AI": {
    "Provider": "Mock",
    "ApiKey": "",
    "Endpoint": "https://api.openai.com/v1/chat/completions",
    "Model": "gpt-4"
  },
  "Features": {
    "ProEnabled": false,
    "SemanticClustering": false,
    "Integrations": false
  },
  "License": {
    "Key": "",
    "IsTrial": true,
    "ExpirationDate": ""
  }
}
```

**Configuration Notes:**

- Change `AI.Provider` to `"OpenAI"` and set `AI.ApiKey` to use real AI provider
- Set `Features.ProEnabled` to `true` to enable Pro plugin features
- Set `LICENSE_KEY` environment variable or `License.Key` in config to enable Pro edition (format: `LC-PRO-*`)
- Feature flags control plugin availability: `ProEnabled`, `SemanticClustering`, `Integrations`

### Frontend (.env)

```
VITE_API_URL=http://localhost:5000
```

## API Endpoints

### Authentication

- `POST /api/auth/register` - Register new user + organization
- `POST /api/auth/login` - Login
- `POST /api/auth/refresh` - Refresh access token
- `POST /api/auth/logout` - Revoke refresh token

### Uploads

- `POST /api/uploads` - Create upload session
- `POST /api/uploads/{id}/files` - Upload log file
- `POST /api/uploads/{id}/start` - Start ingestion
- `GET /api/uploads/{id}` - Get session status

### Logs

- `GET /api/logs` - Query logs (paginated, filtered)
- `GET /api/logs/{id}` - Get log event details

### Traces

- `GET /api/traces` - List request traces
- `GET /api/traces/{id}` - Get trace timeline

### Clusters

- `GET /api/clusters` - List issue clusters
- `GET /api/clusters/{id}` - Get cluster details

### Reports

- `POST /api/reports/generate` - Generate incident report (uses plugin architecture)
- `GET /api/reports` - List reports (paginated)
- `GET /api/reports/{id}` - Get report details with full output
- `GET /api/reports/{id}/export?format=md|json|html|prompt&target=claude` - Export report with redaction
  - `format=md` - Download Markdown file
  - `format=json` - Download JSON file
  - `format=html` - Download standalone HTML file
  - `format=prompt` - Download AI agent prompt (instruction-only, no code blocks)

## Architecture

### Plugin Architecture

Log Copilot uses a plugin-based architecture to enable Open-Core / Pro separation:

**Core Plugins:**

- `IIncidentNarrativeGenerator` - Report generation strategy
  - `CommunityHeuristicNarrativeGenerator` - Heuristic-based (Community)
  - `OpenAiNarrativeGenerator` - AI-powered (Pro)
- `IClusterStrategy` - Clustering algorithm (extensible for semantic clustering)
- `IIntegrationSink` - Report distribution (webhook, email, etc.)
- `IFeatureFlagService` - Feature flag evaluation
- `ILicenseVerifier` - License validation and edition detection

**Dependency Injection:**

All plugins are registered in `Program.cs` and can be swapped at runtime based on:

- Feature flags (`Features:ProEnabled`, `Features:SemanticClustering`, `Features:Integrations`)
- License verification (`LICENSE_KEY` environment variable)

### Project Structure

```
src/
├── LogCopilot.Api/          # Web API controllers, auth, DI setup
├── LogCopilot.Application/  # DTOs, interfaces, validation
├── LogCopilot.Domain/       # Entities, enums
└── LogCopilot.Infrastructure/
    ├── Data/                # DbContext, EF configurations
    ├── Parsers/             # Serilog/Nginx log parsers
    ├── AI/                  # AI provider abstraction
    ├── Clustering/          # Error clustering logic, IClusterStrategy
    ├── Tracing/             # Trace reconstruction
    ├── Storage/             # File storage abstraction
    ├── Plugins/             # IIncidentNarrativeGenerator implementations
    ├── Licensing/           # ILicenseVerifier implementations
    ├── Features/            # IFeatureFlagService
    ├── Integrations/        # IIntegrationSink implementations
    ├── Utils/               # RedactionUtility for sensitive data
    └── Services/            # Auth, ingestion, report services

frontend/
├── src/
│   ├── components/         # Reusable UI components
│   │   └── reports/        # EditionBadge, ExportDropdown
│   ├── pages/              # Route pages
│   ├── contexts/           # React contexts (Auth)
│   └── lib/                # API client, utilities
```

## Database Schema

**Core entities:**

- Organization, User, OrganizationMember, RefreshToken
- UploadSession, UploadedFile, IngestionJob
- LogSource, LogEvent, HttpEventDetails, ExceptionDetails
- RequestTrace, TraceEvent
- IssueCluster, ClusterEvent
- IncidentReport

All entities include audit fields (CreatedAt, UpdatedAt, CreatedBy, UpdatedBy) and soft delete where appropriate.

## Security Features

- BCrypt password hashing
- JWT access tokens (1 hour) + refresh tokens (7 days)
- Refresh token rotation on use
- Tenant isolation enforced in all queries
- CORS configuration for local dev
- Prompt injection defense for AI calls
- Server-side sensitive data redaction on all exports:
  - JWT tokens → `[REDACTED_JWT]`
  - API keys → `[REDACTED_OPENAI_KEY]`, `[REDACTED_API_KEY]`
  - Email addresses → `[REDACTED_EMAIL]`
  - Passwords → `[REDACTED_PASSWORD]`
  - Database connection strings → `[REDACTED_CONNECTION_STRING]`
- Export formats support download and clipboard operations
- Agent prompt generation strips code blocks and diffs for safe AI consumption

## Testing

```bash
cd tests/LogCopilot.Tests
dotnet test
```

Includes:

- Clustering signature hash tests
- Serilog JSON parser tests
- Redaction utility tests (JWT, API keys, emails, passwords)
- Feature flag service tests
- License verifier tests (Community and Pro)
- Agent prompt generation tests (no code blocks validation)

## Export and Report Generation

### Report Export Formats

All exports include server-side redaction of sensitive data (JWT tokens, API keys, emails, passwords, connection strings).

**Markdown Export:**

```bash
curl -H "Authorization: Bearer $TOKEN" \
  "http://localhost:5000/api/reports/{id}/export?format=md" \
  -o report.md
```

Downloads a formatted Markdown file with sections for executive summary, metrics, top issues, and recommendations.

**JSON Export:**

```bash
curl -H "Authorization: Bearer $TOKEN" \
  "http://localhost:5000/api/reports/{id}/export?format=json" \
  -o report.json
```

Downloads the full report structure as JSON for programmatic processing.

**HTML Export:**

```bash
curl -H "Authorization: Bearer $TOKEN" \
  "http://localhost:5000/api/reports/{id}/export?format=html" \
  -o report.html
```

Downloads a standalone, print-friendly HTML file with inline CSS and no external dependencies.

**Agent Prompt Export:**

```bash
curl -H "Authorization: Bearer $TOKEN" \
  "http://localhost:5000/api/reports/{id}/export?format=prompt&target=claude" \
  -o report-prompt.txt
```

Downloads a plain-text prompt suitable for AI agents (Claude, GPT, etc.) with:

- No code blocks or backticks
- Instruction-only format
- Metrics and issue summaries
- Implementation tasks and acceptance criteria

### Report Generation Plugins

**Community Edition (Default):**

- Uses `CommunityHeuristicNarrativeGenerator`
- Analyzes log patterns heuristically
- Generates metrics, top issues, and recommendations
- No external API calls required

**Pro Edition (with License):**

- Uses `OpenAiNarrativeGenerator` when `LICENSE_KEY` is set
- Leverages OpenAI API for advanced analysis
- Falls back to heuristic generation if API fails
- Requires `AI.Provider=OpenAI` and valid `AI.ApiKey`

### Enabling Pro Features

1. Set `LICENSE_KEY` environment variable:

   ```bash
   export LICENSE_KEY=LC-PRO-1234567890abcdefghij
   ```

2. Update `appsettings.json`:

   ```json
   {
     "Features": {
       "ProEnabled": true,
       "SemanticClustering": false,
       "Integrations": false
     },
     "AI": {
       "Provider": "OpenAI",
       "ApiKey": "sk-...",
       "Model": "gpt-4o-mini"
     }
   }
   ```

3. Restart backend - reports will now use OpenAI-powered generation

## Scripts

**Create migration:**

```bash
bash scripts/create-migration.sh MigrationName
```

**Run migrations:**

```bash
bash scripts/run-migrations.sh
```

## Future Enhancements

**Plugin Ecosystem:**

- Semantic clustering strategy plugin (ML-based)
- Email integration sink for report distribution
- Slack webhook integration sink
- PagerDuty incident creation sink
- Custom narrative generator plugins
- Anomaly detection strategy plugin

**Platform Features:**

- Full upload/ingestion UI implementation
- Real-time log streaming via SignalR
- Advanced analytics dashboard
- Alert rules and notifications
- S3/Blob storage for files
- Elasticsearch integration for scale
- Grafana/Prometheus metrics
- Multi-organization invite system
- Billing integration for Pro edition
- Plugin marketplace for community contributions

## License

Proprietary - All rights reserved
