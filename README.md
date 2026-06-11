# LUCY Identity Service (.NET)

User & Payment service foundation for LUCY. Current scope covers week 1-2 Identity:

- Register/Login.
- Password hashing with PBKDF2-SHA256.
- HMAC JWT access tokens.
- Roles: `Lucy`, `Pro`, `Super`.
- Anonymous persona field for LUCY users.
- Protected profile endpoint and sample role-protected mentor endpoint.
- PostgreSQL-backed user storage with EF Core code-first migrations.
- Anonymous privacy profiles isolated from private account data.
- Refresh token login sessions.
- Internal profile/token endpoints for Node.js and Java services.
- `Super` user management endpoints.
- Configurable JWT lifetimes for short-lived access tokens and database-backed refresh tokens.

## Architecture

The solution uses 3 layers:

- `Lucy.Identity.Api`: presentation layer, controllers, HTTP auth handler, JWT implementation, app configuration.
- `Lucy.Identity.Domain`: business layer, entities, DTO contracts, identity service, password hashing, repository/token abstractions.
- `Lucy.Identity.Infrastructure`: data layer, EF Core `IdentityDbContext`, PostgreSQL repository, migrations.

## Run

```powershell
docker compose up -d
cd SWD-Lucy_Dotnet\Lucy.Identity.Api
dotnet run
```

Default HTTP URL: `http://localhost:5095`.
Swagger UI: `http://localhost:5095/swagger`.

The default local connection string is:

```text
Host=localhost;Port=5432;Database=lucy_identity;Username=postgres;Password=postgres
```

Override it with `ConnectionStrings__IdentityDb` for another PostgreSQL instance. The API applies EF Core migrations automatically when it starts, but the database itself must already exist.

Development seeds a local Super account when `appsettings.Development.json` is active:

```text
super@lucy.local / SuperPassword123
```

## Docker

The Identity API can be packaged with the included multi-stage `Dockerfile`.

Build the image:

```powershell
docker build -t lucy-identity-api:latest .
```

Run API + PostgreSQL locally:

```powershell
Copy-Item .env.docker.example .env
docker compose up -d --build
```

Default container URLs:

- Identity API: `http://localhost:5095`
- PostgreSQL: `localhost:5432`

Useful checks:

```powershell
docker compose ps
curl.exe http://localhost:5095/swagger/index.html
```

For deployment, set these environment variables outside source control:

```text
ConnectionStrings__IdentityDb=Host=<postgres-host>;Port=5432;Database=<db>;Username=<user>;Password=<password>
Jwt__Issuer=lucy.identity
Jwt__Audience=lucy.clients
Jwt__SigningKey=<strong-shared-secret>
Jwt__AccessTokenMinutes=15
Jwt__RefreshTokenDays=30
LUCY_ENABLE_HTTPS_REDIRECTION=false
ASPNETCORE_URLS=http://+:8080
```

`Jwt__SigningKey` must match the Gateway, Realtime, and LMS services. The container listens on port `8080`; `docker-compose.yml` maps it to host port `5095`.

HTTPS redirection is disabled by default in the container because LUCY routes traffic through the Gateway/reverse proxy. Terminate TLS at the proxy/load balancer, or set `LUCY_ENABLE_HTTPS_REDIRECTION=true` only when the container is configured with HTTPS correctly.

## Migrations

Migrations live in `Lucy.Identity.Infrastructure/Persistence/Migrations`.

Create a new migration:

```powershell
dotnet ef migrations add <MigrationName> --project Lucy.Identity.Infrastructure --startup-project Lucy.Identity.Api --output-dir Persistence\Migrations
```

Apply migrations manually:

```powershell
dotnet ef database update --project Lucy.Identity.Infrastructure --startup-project Lucy.Identity.Api
```

## API

### Register

`POST /api/auth/register`

```json
{
  "email": "mentor@lucy.local",
  "password": "Password123",
  "displayName": "Lucy Mentor",
  "role": "Pro",
  "avatarPersona": "calm-blue"
}
```

### Login

`POST /api/auth/login`

```json
{
  "email": "mentor@lucy.local",
  "password": "Password123"
}
```

### Current User

`GET /api/auth/me`

Header:

```text
Authorization: Bearer <accessToken>
```

### Update My Profile

`PUT /api/auth/me/profile`

```json
{
  "displayName": "Lucy Mentor",
  "avatarPersona": "focused-green",
  "anonymousDisplayName": "Mentor Green",
  "publicBio": "Conversation practice mentor",
  "languageLevel": "Advanced"
}
```

### Refresh

`POST /api/auth/refresh`

```json
{
  "refreshToken": "<refreshToken>"
}
```

### Logout

`POST /api/auth/logout`

```json
{
  "refreshToken": "<refreshToken>"
}
```

### List Users

Requires a `Super` token.

`GET /api/users`

### Get User

Requires a `Super` token.

`GET /api/users/{id}`

### Update User Role

Requires a `Super` token.

`PUT /api/users/{id}/role`

```json
{
  "role": "Pro"
}
```

### Update User Status

Requires a `Super` token.

`PATCH /api/users/{id}/status`

```json
{
  "status": "Suspended"
}
```

### Internal Public Profile

`GET /api/internal/users/{id}/public-profile`

Returns only public-safe identity fields for Java/Node.js integrations.

### Internal Room Identity

`GET /api/internal/users/{id}/room-identity`

Returns persona-safe identity for audio rooms.

### Internal Token Validation

`POST /api/internal/tokens/validate`

```json
{
  "accessToken": "<accessToken>"
}
```

## Production Notes

Replace `Jwt:SigningKey` in `appsettings.json` with an environment-specific secret and store the PostgreSQL connection string outside source control.
