#!/bin/bash

cd src/LogCopilot.Api

echo "Running database migrations..."
dotnet ef database update --project ../LogCopilot.Infrastructure/LogCopilot.Infrastructure.csproj

echo "Migrations completed!"
