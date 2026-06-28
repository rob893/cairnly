# CI / Infrastructure

## Azure Bicep (`CI/Azure/`)

Provisions the Cairnly API into its own resource group (`rherber-cairnly-rg-uc-d`),
reusing shared infrastructure that lives in `rherber-shared-rg-ue-d`.

### Resources provisioned

| Module | Resource | Location | Name |
|--------|----------|----------|------|
| `appInsights.bicep` | Application Insights (workspace-based) | Cairnly RG | `rherber-cairnly-ai-uc-d` |
| `appService.bicep` | Web App (.NET 10, system-assigned MI) | Cairnly RG | `rherber-cairnly-api-uc-d` |
| `rbac.bicep` | Monitoring Metrics Publisher on App Insights | Cairnly RG | — |
| `logAnalyticsRbac.bicep` | Monitoring Metrics Publisher on the shared Log Analytics workspace | **shared RG** | — |
| `keyVaultRbac.bicep` | Key Vault Secrets User on the shared vault | **shared RG** | — |
| `acsEmailRbac.bicep` | Communication and Email Service Owner on the shared ACS | **shared RG** | — |
| `nsgPostgresRule.bicep` | Inbound 5432 rule on the VM NSG | **shared RG** | `AllowCairnlyAppServiceToPostgres` |

Shared resources **referenced but not created** (all pre-provisioned in the shared RG):
App Service plan `rherber-shared-asp-uc-d` (Central US), Key Vault `rherber-kv-ue-d`, Log Analytics
`rherber-logworkspace-uw-lws-d`, Communication Services `rherber-acs-g-d` (email), and the database
VM/NSG `rherber-vm-ue-d-nsg`.

### Architecture notes

- **OS**: `appServiceOs` defaults to **Windows** and **must match the existing shared
  plan's OS** so the web app stack is configured correctly (Windows gives richer
  in-portal .NET diagnostics — Profiler, Snapshot Debugger).
- **Database**: PostgreSQL runs on the shared VM (static public IP), not a managed
  Flexible Server. The web app reaches it over the public endpoint on port 5432; the
  `nsgPostgresRule` module opens that port to the web app's outbound IPs only. Those
  IPs change only when the plan's **pricing tier** changes — re-run the deploy if you
  resize the plan across tiers. The actual connection string is a Key Vault secret
  (set out-of-band) and must point at the VM's public IP. PostgreSQL on the VM must be
  configured to accept remote TLS connections (`listen_addresses`, `pg_hba.conf`).
- **Subscription-scoped deploy**: `main.bicep` is `targetScope = 'subscription'` — the
  deployment **creates the Cairnly RG itself** (name from the parameters file) and also
  creates the NSG rule and assigns a Key Vault role in the shared RG (the App Service plan
  there is pre-existing and only referenced). The deploy identity needs Contributor on the
  **subscription** (or on both resource groups).

### Security model

- The Web App uses a **system-assigned managed identity** — no secrets in app settings.
- RBAC role assignments:
  - **Key Vault Secrets User** on the shared Key Vault → `DefaultAzureCredential` reads secrets at startup.
  - **Monitoring Metrics Publisher** on App Insights **and** the backing shared Log Analytics workspace → telemetry publishing.
  - **Communication and Email Service Owner** on the shared ACS → sends email via `DefaultAzureCredential` (the `EmailClient` data plane authenticates with the MI; without this role ACS returns `401`).
- App Insights has **local auth disabled** (`DisableLocalAuth: true`), so telemetry ingestion requires Microsoft Entra auth — the app's `UseAzureMonitor` is configured with `DefaultAzureCredential`, which is why the MI needs the Monitoring Metrics Publisher role above.
- The shared Key Vault has `enableRbacAuthorization: true`; no legacy access policies.

### Deploy infrastructure manually

```bash
az deployment sub create \
  --location centralus \
  --template-file CI/Azure/main.bicep \
  --parameters @CI/Azure/parameters/main.parameters.dev.json
```

The deployment creates the `rherber-cairnly-rg-uc-d` resource group; no need to create it first.

### Parameters (`CI/Azure/parameters/main.parameters.dev.json`)

| Parameter | Default | Description |
|-----------|---------|-------------|
| `cairnlyResourceGroupName` | `rherber-cairnly-rg-uc-d` | Cairnly RG, **created** by the subscription-scoped deploy |
| `location` | `centralus` | Region for the Cairnly RG and its resources (must match the shared plan's region) |
| `namePrefix` | `rherber-cairnly` | Prefix for all resource names |
| `regionToken` | `ue` | Region token used in names |
| `environment` | `d` | Env token; `d` maps to `Development`, anything else → `Production` |
| `appServiceOs` | `Windows` | OS of the existing shared plan (must match it) |
| `appServicePlanName` | `rherber-shared-asp-uc-d` | Existing shared plan (Central US) to host the web app |
| `sharedResourceGroupName` | `rherber-shared-rg-ue-d` | RG holding shared infra |
| `sharedKeyVaultName` | `rherber-kv-ue-d` | Shared Key Vault to read secrets from |
| `sharedLogAnalyticsWorkspaceName` | `rherber-logworkspace-uw-lws-d` | Workspace for App Insights |
| `vmNsgName` | `rherber-vm-ue-d-nsg` | VM NSG to open PostgreSQL on |

---

## GitHub Actions (`.github/workflows/`)

### `ci.yml` — PR / branch validation

Triggers on `pull_request` and `push` to non-main/non-master branches.

- **api** job: `dotnet restore` → `dotnet build -c Release` → `dotnet test`
- **ui** job: `npm ci` → `npm run lint` → `npm run test` → `npm run build`

### `build-and-deploy-api.yml` — API deploy to Azure App Service

Triggers on push to `main`/`master` (paths: `Cairnly.API/**`) and `workflow_dispatch`.

**Auth:** OIDC via `azure/login@v2` — no long-lived credentials stored as secrets.

#### Required secrets

| Secret | Description |
|--------|-------------|
| `AZURE_CLIENT_ID` | App registration / managed identity client ID |
| `AZURE_TENANT_ID` | Azure AD tenant ID |
| `AZURE_SUBSCRIPTION_ID` | Azure subscription ID |

#### Required repository variables

None. The resource group and web app names are derived from the Bicep parameters file
(`CI/Azure/parameters/main.parameters.dev.json`) — the subscription-scoped deploy creates
the RG, and the code-deploy step composes the web app name from the same `namePrefix` /
`regionToken` / `environment` values. Change the names in one place: the parameters file.

#### OIDC setup

1. In Azure: create a federated credential on the app registration.
   - Subject: `repo:<owner>/<repo>:ref:refs/heads/main`
2. Grant the app registration **Contributor** on the resource group (or narrower roles as needed).

### `build-and-deploy-ui.yml` — UI deploy to GitHub Pages

Triggers on push to `main`/`master` (paths: `cairnly-ui/**`) and `workflow_dispatch`.

Uses the official GitHub Pages action flow (`upload-pages-artifact` + `deploy-pages`).

#### Configuration

No repository variables or secrets are required for the UI deploy. The API base URL
is committed, non-secret config in `cairnly-ui/.env.production` (read automatically by
`npm run build`). To target a different API origin, edit that file or add another
`.env.<mode>` and build with `vite build --mode <mode>`.

#### GitHub Pages setup

1. **Settings → Pages → Source → GitHub Actions** (not "Deploy from a branch").
2. For a project site (non-user/org), set `base` in `cairnly-ui/vite.config.ts`:
   ```ts
   base: '/cairnly-template/'  // replace with your repo name
   ```
   For a custom domain or user/org site (`username.github.io`), use `base: '/'`.
   The UI uses `HashRouter`, so client-side routing works without server rewrites.
