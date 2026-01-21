#!/bin/bash

if [ -z "$1" ]; then
  echo "Usage: ./create-migration.sh <MigrationName>"
  exit 1
fi

cd src/LogCopilot.Api

echo "Creating migration: $1"
dotnet ef migrations add $1 --project ../LogCopilot.Infrastructure/LogCopilot.Infrastructure.csproj --output-dir Data/Migrations

echo "Migration created!"
