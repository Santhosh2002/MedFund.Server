# MedFund Backend

ASP.NET Core modular monolith backend for the MedFund frontend.

## Projects

- `src/MedFund.Api`: HTTP API, controllers, auth policies, Swagger, middleware.
- `src/MedFund.Application`: DTOs, service contracts, validation, shared response models.
- `src/MedFund.Domain`: entities, enums, and core state transition rules.
- `src/MedFund.Infrastructure`: EF Core/PostgreSQL, auth token services, password hashing, seed data, concrete services.
- `tests/MedFund.Tests`: focused unit tests for stable business rules.

## Local Setup

1. Install the .NET 10 SDK.
2. Start PostgreSQL and create/update the connection string in `src/MedFund.Api/appsettings.Development.json` or user secrets.

```powershell
docker compose up -d postgres
```
3. Apply the existing initial migration:

```powershell
dotnet ef database update --project src/MedFund.Infrastructure --startup-project src/MedFund.Api
```

4. To let the application check for pending migrations and apply updates on startup, set:

```json
{
  "Database": {
    "ApplyMigrationsOnStartup": true,
    "SeedDevelopmentDataOnStartup": true
  }
}
```

5. Run the API:

```powershell
dotnet run --project src/MedFund.Api
```

The API listens on `http://localhost:8081/api` and `https://localhost:7081/api`.

## Render Docker Deployment

This backend can deploy to Render as a Docker web service using the root `Dockerfile`.

1. Push this repository to GitHub.
2. In Render, create a Blueprint from the repository, or create a Docker web service manually.
3. If using the Blueprint, Render reads `render.yaml`, creates the `medfund-api` service, and prompts for secret values.
4. Confirm these production environment variables:

```text
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:10000
DATABASE_URL=<Neon Postgres connection string>
Database__ApplyMigrationsOnStartup=true
Database__SeedDevelopmentDataOnStartup=false
Jwt__SigningKey=<generated or manually provided secret>
Email__ApiKey=<Mailjet API key>
Email__ApiSecret=<Mailjet API secret>
Email__FromEmail=<verified sender address>
Email__FrontendBaseUrl=<frontend URL>
```

The API exposes `/health` for Render health checks. When `DATABASE_URL` is present, the app converts the Postgres URL into the Npgsql connection string format used by EF Core, including Neon options such as `sslmode=require` and `channel_binding=require`.

## Development Users

- `patient@medfund.local` / `Password@123` / `PATIENT`
- `hospital@medfund.local` / `Password@123` / `HOSPITAL`
- `insurance@medfund.local` / `Password@123` / `INSURANCE_COMPANY`

## Mailjet Email

Email delivery uses Mailjet through the `IEmailSender` abstraction. Configure these settings with user secrets, environment variables, or `appsettings.Development.json`:

```json
{
  "Email": {
    "Enabled": true,
    "ApiKey": "mailjet-api-key",
    "ApiSecret": "mailjet-api-secret",
    "FromEmail": "no-reply@your-domain.com",
    "FromName": "MedFund",
    "FrontendBaseUrl": "http://localhost:3000",
    "VerifyEmailPath": "/verify-email",
    "ResetPasswordPath": "/reset-password"
  }
}
```

When email is disabled, the Mailjet sender logs that delivery was skipped. Passwords, refresh tokens, reset tokens, and verification tokens are not written to logs.

## Implemented API Groups

- `/api/auth/*`
- `/api/auth/me` and `/api/users/me`
- `/api/patient/*`
- `/api/hospital/*`
- `/api/insurance/*`
- `/api/financing-requests/*`
- `/api/settlements`
- `/api/settlements/{id}`
