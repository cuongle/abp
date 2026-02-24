# User Management (Angular + .NET + Dapper + SQL Server)

This sample includes:
- Login using username/password
- Password change flow
- Azure AD SSO login
- Role-based authorization (`Admin`, `User`)
- Angular frontend with plain CSS
- .NET 8 backend with Dapper + SQL Server

## Backend

```bash
cd backend/UserManagement.Api
dotnet restore
dotnet run
```

Backend default URL: `http://localhost:5000` (or local Kestrel random port)

## Frontend

```bash
cd frontend
npm install
npm run start
```

Frontend URL: `http://localhost:4200`

## Database

Run `database/init.sql` in SQL Server Management Studio.

## Azure AD SSO setup

Update backend `appsettings.json`:
- `AzureAd:TenantId`
- `AzureAd:ClientId`
- `AzureAd:ClientSecret`

Add redirect URI in Azure app registration:
- `http://localhost:5000/auth/sso/callback`

## API endpoints

- `POST /api/auth/login`
- `GET /api/auth/sso`
- `GET /api/auth/sso/callback`
- `POST /api/users/change-password` (JWT required)
- `GET /api/users/me` (JWT required)
- `GET /api/admin/users` (Admin role required)
