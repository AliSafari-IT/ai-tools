# Log Copilot

Production-ready log analysis platform with AI-powered incident reporting. Upload Serilog JSON logs and Nginx logs, automatically cluster errors, reconstruct request traces, and generate AI-assisted incident reports.

## Tech Stack

**Backend:**

- ASP.NET Core 8 Web API
- PostgreSQL with EF Core
- JWT Authentication
- Serilog for observability
- BCrypt for password hashing

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

- Multi-tenant user system with role-based access (Admin/Member)
- Upload and parse Serilog JSONL, Nginx access/error logs
- Automatic error clustering by exception signature
- Request trace reconstruction via TraceId/CorrelationId
- AI-powered incident reports with evidence citations (OpenAI-compatible or Mock)
- Tenant isolation at database level
- Secure defaults with JWT + refresh tokens

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
  }
}
```

Change `AI.Provider` to `"OpenAI"` and set `AI.ApiKey` to use real AI provider.

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

- `POST /api/reports` - Generate AI incident report
- `GET /api/reports` - List reports
- `GET /api/reports/{id}` - Get report details

## Architecture

```
src/
├── LogCopilot.Api/          # Web API controllers, auth, DI setup
├── LogCopilot.Application/  # DTOs, interfaces, validation
├── LogCopilot.Domain/       # Entities, enums
└── LogCopilot.Infrastructure/
    ├── Data/                # DbContext, EF configurations
    ├── Parsers/             # Serilog/Nginx log parsers
    ├── AI/                  # AI provider abstraction
    ├── Clustering/          # Error clustering logic
    ├── Tracing/             # Trace reconstruction
    ├── Storage/             # File storage abstraction
    └── Services/            # Auth, ingestion services

frontend/
├── src/
│   ├── components/         # Reusable UI components
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

## Testing

```bash
cd tests/LogCopilot.Tests
dotnet test
```

Includes:

- Clustering signature hash tests
- Serilog JSON parser tests

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

- Full upload/ingestion UI implementation
- Real-time log streaming via SignalR
- Advanced analytics dashboard
- Alert rules and notifications
- S3/Blob storage for files
- Elasticsearch integration for scale
- Grafana/Prometheus metrics
- Multi-organization invite system
- Billing integration

## License

Proprietary - All rights reserved
