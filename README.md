# Cairnly

> **Cairnly** is a personal finance app for managing budgets and tracking spend over time. It's a
> full-stack application — a .NET 10 Web API + a React SPA — wired for auth, observability, and
> deployment to Azure App Service (API) and GitHub Pages (UI).

## What's included

- **Versioned REST API** (`/api/v1`, `/api/v2`) with an interactive **Scalar** UI.
- **Authentication** out of the box: ASP.NET Identity, JWT access tokens, HttpOnly refresh-token
  cookies with double-submit CSRF, and **Google + GitHub social logins**.
- **EF Core + PostgreSQL** with the repository pattern, a generated initial migration, and a seeder CLI.
- **Result pattern** (`Result<T>` → `ProblemDetailsWithErrors`) and **cursor-based pagination**.
- **Observability**: Application Insights via OpenTelemetry, correlation IDs on every response.
- **Secret management**: Azure Key Vault config provider (non-Dev), `appsettings.Local.json` (Dev).
- CORS, rate limiting, health checks, global exception handling, forwarded-headers hardening.
- **React 19 + Vite + Tailwind v4 + HeroUI v3** SPA with TanStack Query, axios auth plumbing, and a
  sample authenticated page that calls the API and pages through data.
- **CI/CD**: GitHub Actions (PR validation, API → App Service via OIDC, UI → GitHub Pages) and
  **Azure Bicep** that provisions the whole environment.
- Sample **Hello World** (v1 + v2) and a **Notes** resource demonstrating the full stack end-to-end
  (these are disposable demos — see [Using this template](#using-this-template) to remove them).

## Tech stack

| Layer    | Tech                                                                                                                                            |
| -------- | ----------------------------------------------------------------------------------------------------------------------------------------------- |
| Backend  | .NET 10, ASP.NET Core, EF Core, PostgreSQL, ASP.NET Identity, JWT, OpenTelemetry/App Insights                                                   |
| Frontend | React 19, Vite, TypeScript, Tailwind v4, HeroUI v3, TanStack Query, axios                                                                       |
| Infra    | Azure Bicep (subscription-scoped) — App Service on a shared plan, App Insights + Log Analytics, Key Vault, ACS email, PostgreSQL on a shared VM |
| CI/CD    | GitHub Actions (OIDC to Azure; GitHub Pages)                                                                                                    |
| Tests    | xUnit + Moq (API), Vitest (UI unit), Playwright (UI e2e)                                                                                        |

## Repository structure

```
.
├─ Cairnly.API/          # .NET 10 Web API (extension-driven startup, v1/v2, auth, EF Core)
├─ Cairnly.API.Tests/    # xUnit + Moq, mirrored folders
├─ cairnly-ui/          # React + Vite SPA (HeroUI v3, Vitest)
├─ CI/Azure/                # Bicep infra (modules + dev parameters) — see Deployment below
├─ .github/workflows/       # ci.yml, build-and-deploy-api.yml, build-and-deploy-ui.yml
├─ AGENTS.md                # architecture & conventions (read this!)
└─ Cairnly.slnx          # solution
```

See **[`AGENTS.md`](./AGENTS.md)** for architecture/conventions and the [Deployment](#deployment)
section below for the full infrastructure and deployment guide.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 24+](https://nodejs.org/)
- A PostgreSQL instance (local Docker is fine: `docker run --name pg -e POSTGRES_PASSWORD=postgres -p 5432:5432 -d postgres`)
- (Optional) [`dotnet-ef`](https://learn.microsoft.com/ef/core/cli/dotnet): `dotnet tool install --global dotnet-ef`
- (Optional, for deploy) Azure CLI + Bicep

## Quick start

### 1. API

```bash
# From the repo root — create your local secrets file:
cp Cairnly.API/appsettings.Local.example.json Cairnly.API/appsettings.Local.json
# Edit it: set Postgres connection, a JWT APISecret (>= 64 chars, for HMAC-SHA512), and OAuth client IDs/secrets.

# Apply the migration to your database:
dotnet ef database update --project Cairnly.API
# ...or use the seeder CLI:  dotnet run --project Cairnly.API -- seeder migrate seed --password <SeederPassword>

# Trust the ASP.NET dev cert (once) so the HTTPS refresh-cookie flow works:
dotnet dev-certs https --trust

# Run with hot reload:
npm run start                        # => https://localhost:7234
```

Open the **Scalar API docs** at `https://localhost:7234/scalar/v1` (and `/scalar/v2`). The
`GET /api/v1/hello/ping` endpoint is anonymous; `GET /api/v1/hello` and the `Notes` endpoints require a
bearer token (register/login via `/api/v1/auth/*`).

### 2. UI

```bash
cd cairnly-ui
npm install
# .env.local already points at the local API; adjust VITE_API_BASE_URL if needed.
npm run dev                          # => http://localhost:5180
```

## Configuration & secrets

Configuration is layered: `appsettings.json` → `appsettings.{Environment}.json` →
`appsettings.Local.json` (Dev only, git-ignored) → **Azure Key Vault** (non-Dev).

### Local (development)

Put secrets in `Cairnly.API/appsettings.Local.json` (copied from the `.example`). Never commit it.

### Production (Azure Key Vault)

In non-Development environments the app loads secrets from the Key Vault at `KeyVaultUrl` using its
managed identity. The `PrefixKeyVaultSecretManager` only reads secrets prefixed with `Cairnly--` (or
`All--`) and maps `--` → `:`. Create these Key Vault secrets:

| Key Vault secret name                              | Maps to config key                                                            |
| -------------------------------------------------- | ----------------------------------------------------------------------------- |
| `Cairnly--Authentication--APISecret`               | `Authentication:APISecret` (JWT signing key)                                  |
| `Cairnly--Postgres--DefaultConnection`             | `Postgres:DefaultConnection` (point at the VM's public IP, `SslMode=Require`) |
| `Cairnly--Authentication--GoogleOAuthClientSecret` | Google OAuth secret                                                           |
| `Cairnly--Authentication--GitHubOAuthClientSecret` | GitHub OAuth secret                                                           |

> The Application Insights connection string is injected by the Bicep as the `ApplicationInsightsConnectionString`
> app setting, so it does **not** need a Key Vault secret. You may still override it with one
> (`Cairnly--ApplicationInsightsConnectionString`) if you prefer.

After deploying, also add your GitHub Pages origin to the API's allowed CORS origins (via
`appsettings.Production.json`, an app setting `Cors__AllowedOrigins__0`, or Key Vault).

## API at a glance

- **Versioning**: URL segment — `GET /api/v1/...`, `GET /api/v2/...`.
- **Auth**: global `[Authorize]`; opt out with `[AllowAnonymous]`. JWT bearer + refresh-token cookie + CSRF.
- **Errors**: `ProblemDetailsWithErrors` + `X-Correlation-Id` header on every response.
- **Pagination**: cursor-based — query with `?first=20&after=<cursor>&includeTotal=true`.
- **Sample endpoints**: `GET /api/v1/hello`, `GET /api/v2/hello`, `GET /api/v1/hello/ping`,
  and full CRUD under `GET|POST|PUT|DELETE /api/v1/notes`.

## Testing

```bash
dotnet test                                   # API: xUnit + Moq
cd cairnly-ui && npm run test             # UI unit: Vitest
cd cairnly-ui && npm run test:e2e         # UI e2e: Playwright (needs the app running)
```

## Deployment

Cairnly deploys to **Azure App Service** (API) and **GitHub Pages** (UI). Infrastructure is
provisioned with **Azure Bicep** (`CI/Azure/`). The API lives in its own resource group
(`rherber-cairnly-rg-uc-d`, Central US) and **reuses shared infrastructure** that already exists in
`rherber-shared-rg-ue-d` (App Service plan, Key Vault, Log Analytics, ACS email, and the PostgreSQL VM).

### Architecture overview

| Resource                               | Created by Bicep? | Location   | Name                                      |
| -------------------------------------- | ----------------- | ---------- | ----------------------------------------- |
| Application Insights (workspace-based) | ✅ Cairnly RG     | Central US | `rherber-cairnly-ai-uc-d`                 |
| Web App (.NET 10, system-assigned MI)  | ✅ Cairnly RG     | Central US | `rherber-cairnly-api-uc-d`                |
| App Service plan                       | ❌ referenced     | Central US | `rherber-shared-asp-uc-d`                 |
| Key Vault                              | ❌ referenced     | shared RG  | `rherber-kv-ue-d`                         |
| Log Analytics workspace                | ❌ referenced     | shared RG  | `rherber-logworkspace-uw-lws-d`           |
| Communication Services (email)         | ❌ referenced     | shared RG  | `rherber-acs-g-d`                         |
| PostgreSQL host (VM + NSG)             | ❌ referenced     | shared RG  | `rherber-vm-ue-d` / `rherber-vm-ue-d-nsg` |

Bicep also creates these **role assignments / rules** (no standalone resources):

- **Key Vault Secrets User** on the shared vault → the Web App MI reads secrets via `DefaultAzureCredential`.
- **Monitoring Metrics Publisher** on App Insights **and** the backing Log Analytics workspace → telemetry.
- **Communication and Email Service Owner** on the shared ACS → the MI sends email (without it ACS returns `401`).
- An inbound **5432** rule (`AllowCairnlyAppServiceToPostgres`) on the VM NSG, scoped to the Web App's outbound IPs.

**Key design points**

- **Subscription-scoped deploy**: `main.bicep` is `targetScope = 'subscription'`, so the deployment
  **creates the Cairnly resource group itself** and assigns roles in the shared RG. The deploy identity
  needs **Contributor on the subscription** (or on both the Cairnly and shared resource groups).
- **App Service plan OS**: `appServiceOs` defaults to **Windows** and **must match the existing shared
  plan's OS** (Windows gives richer in-portal .NET diagnostics — Profiler, Snapshot Debugger).
- **App Insights local auth is disabled** (`DisableLocalAuth: true`); ingestion requires Microsoft Entra
  auth, which is why the MI gets the Monitoring Metrics Publisher role and the app's `UseAzureMonitor`
  is configured with `DefaultAzureCredential`.
- **Database**: PostgreSQL runs on the shared **VM** (static public IP), not a managed Flexible Server.
  The Web App reaches it over the public endpoint on port 5432; the NSG rule restricts that port to the
  app's outbound IPs, and Postgres enforces TLS + password auth (see the one-time VM setup below).

### First-time deployment

> Steps 1–5 are **one-time setup**. After that, deploys are just `git push` (step 6).

#### 1. Provision the Azure infrastructure (Bicep)

```bash
az deployment sub create \
  --location centralus \
  --template-file CI/Azure/main.bicep \
  --parameters @CI/Azure/parameters/main.parameters.dev.json
```

This creates the `rherber-cairnly-rg-uc-d` RG, the App Insights + Web App, and all the role
assignments / NSG rule above. No need to create the RG first.

#### 2. Configure GitHub OIDC for the API deploy workflow

The API deploy authenticates to Azure with **OIDC** (no long-lived secrets). On your Azure AD app
registration (or a user-assigned identity), add a **federated credential**:

- Subject: `repo:<owner>/<repo>:ref:refs/heads/main`

Grant that identity **Contributor** on the Cairnly RG (and shared RG if it also runs the Bicep step),
then set these repo **secrets** (Settings → Secrets and variables → Actions → Secrets):

| Secret                  | Description                                   |
| ----------------------- | --------------------------------------------- |
| `AZURE_CLIENT_ID`       | App registration / managed identity client ID |
| `AZURE_TENANT_ID`       | Azure AD tenant ID                            |
| `AZURE_SUBSCRIPTION_ID` | Azure subscription ID                         |

No repo **variables** are required — the RG and web app names come from the Bicep parameters file.

#### 3. Create the Key Vault secrets

The app loads secrets from the shared Key Vault (`rherber-kv-ue-d`) at startup via its managed identity.
See [Production (Azure Key Vault)](#production-azure-key-vault) for the full list. At minimum, create:

- `Cairnly--Authentication--APISecret` — JWT signing key (≥ 64 chars for HMAC-SHA512)
- `Cairnly--Postgres--DefaultConnection` — the PostgreSQL connection string (see step 5)
- `Cairnly--Authentication--GoogleOAuthClientSecret`, `Cairnly--Authentication--GitHubOAuthClientSecret` — if using social login

#### 4. One-time PostgreSQL setup on the VM

The database lives on the shared VM, so it must be prepared once to accept the App Service over TLS.
SSH into the VM (`ssh azure-vm` or `ssh <user>@<vm-public-ip>`) and, as the `postgres` superuser:

```bash
# Create the database and an app login (use a strong password)
sudo -u postgres psql -c "CREATE DATABASE cairnly;"
sudo -u postgres psql -c "CREATE ROLE cairnlyapp_user LOGIN PASSWORD '<STRONG_PASSWORD>';"
sudo -u postgres psql -c "GRANT ALL PRIVILEGES ON DATABASE cairnly TO cairnlyapp_user;"

# Ensure Postgres listens on all interfaces and TLS is enabled
sudo -u postgres psql -c "SHOW listen_addresses;"   # expect '*'  (else set listen_addresses='*' in postgresql.conf)
sudo -u postgres psql -c "SHOW ssl;"                # expect 'on' (Ubuntu enables it with the ssl-cert snakeoil cert)

# Allow the app to connect to the cairnly DB over SSL only.
# 0.0.0.0/0 is safe here: the VM NSG already restricts port 5432 to the App Service outbound IPs,
# and this rule additionally requires TLS + the scram password and is scoped to one db + one user.
HBA="$(sudo -u postgres psql -tAc 'SHOW hba_file;')"
sudo cp "$HBA" "${HBA}.bak.$(date +%s)"
echo "hostssl cairnly         cairnlyapp_user 0.0.0.0/0               scram-sha-256" | sudo tee -a "$HBA"
sudo -u postgres psql -c "SELECT pg_reload_conf();"
```

> The VM uses a self-signed TLS cert, so the client connection string must set
> `Trust Server Certificate=true` (step 5). For a verified chain, install a CA-signed cert and use
> `SSL Mode=VerifyFull` instead.

#### 5. Set the PostgreSQL connection string secret

Point it at the VM's **public IP** and require TLS. Npgsql connects unencrypted by default, which the
`hostssl` rule above rejects with `no pg_hba.conf entry for host … no encryption`, so `SSL Mode=Require`
(and `Trust Server Certificate=true` for the self-signed cert) is **mandatory**:

```bash
az keyvault secret set \
  --vault-name rherber-kv-ue-d \
  --name "Cairnly--Postgres--DefaultConnection" \
  --value "Host=<VM_PUBLIC_IP>;Database=cairnly;Username=cairnlyapp_user;Password=<STRONG_PASSWORD>;SSL Mode=Require;Trust Server Certificate=true"
```

If you change this secret after the app is already running, restart the Web App so it reloads Key Vault
config: `az webapp restart -g rherber-cairnly-rg-uc-d -n rherber-cairnly-api-uc-d`.

#### 6. Deploy the code & verify

Enable the workflow triggers (see [Using this template](#using-this-template)) and push to `main`, or
run the deploy workflows manually (`workflow_dispatch`). The API deploys to App Service via OIDC and the
UI deploys to GitHub Pages. Then verify the database wiring end-to-end:

```bash
curl https://rherber-cairnly-api-uc-d.azurewebsites.net/health
# expect 200 with {"status":"Healthy", ... "DataContext": {"status":"Healthy"} ...}
```

Finally, add your GitHub Pages origin to the API's allowed CORS origins (via `appsettings.Production.json`,
an app setting `Cors__AllowedOrigins__0`, or a Key Vault secret).

### Bicep parameters (`CI/Azure/parameters/main.parameters.dev.json`)

| Parameter                         | Default                         | Description                                                                       |
| --------------------------------- | ------------------------------- | --------------------------------------------------------------------------------- |
| `cairnlyResourceGroupName`        | `rherber-cairnly-rg-uc-d`       | Cairnly RG, **created** by the subscription-scoped deploy                         |
| `location`                        | `centralus`                     | Region for the Cairnly RG and its resources (must match the shared plan's region) |
| `namePrefix`                      | `rherber-cairnly`               | Prefix for all resource names                                                     |
| `regionToken`                     | `uc`                            | Region token used in names                                                        |
| `environment`                     | `d`                             | Env token; `d` maps to `Development`, anything else → `Production`                |
| `appServiceOs`                    | `Windows`                       | OS of the existing shared plan (must match it)                                    |
| `appServicePlanName`              | `rherber-shared-asp-uc-d`       | Existing shared plan (Central US) to host the web app                             |
| `sharedResourceGroupName`         | `rherber-shared-rg-ue-d`        | RG holding shared infra                                                           |
| `sharedKeyVaultName`              | `rherber-kv-ue-d`               | Shared Key Vault to read secrets from                                             |
| `sharedCommunicationServiceName`  | `rherber-acs-g-d`               | Shared ACS resource for sending email                                             |
| `sharedLogAnalyticsWorkspaceName` | `rherber-logworkspace-uw-lws-d` | Workspace for App Insights                                                        |
| `vmNsgName`                       | `rherber-vm-ue-d-nsg`           | VM NSG to open PostgreSQL on                                                      |

### GitHub Actions workflows (`.github/workflows/`)

> Triggers are intentionally commented out so the template repo stays idle. Enable the
> `pull_request`/`push` blocks after configuring the secrets above.

- **`ci.yml`** — PR / branch validation. **api**: `dotnet restore` → `build -c Release` → `test`;
  **ui**: `npm ci` → `lint` → `test` → `build`.
- **`build-and-deploy-api.yml`** — API → App Service on push to `main`/`master` (paths `Cairnly.API/**`)
  and `workflow_dispatch`. Auth is OIDC via `azure/login@v2`. Requires the three `AZURE_*` secrets above;
  the optional infra step runs the Bicep deploy on `workflow_dispatch`.
- **`build-and-deploy-ui.yml`** — UI → GitHub Pages on push to `main`/`master` (paths `cairnly-ui/**`)
  and `workflow_dispatch`. No secrets/variables required — the API base URL is committed, non-secret
  config in `cairnly-ui/.env.production`.

**GitHub Pages setup:** Settings → Pages → Source → **GitHub Actions** (not "Deploy from a branch").
For a project site, set `base` in `cairnly-ui/vite.config.ts` to `'/<repo-name>/'`; for a custom domain
or user/org site (`username.github.io`), use `base: '/'`. The UI uses `HashRouter`, so client-side
routing works without server rewrites.
