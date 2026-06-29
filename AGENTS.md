# Cairnly

Cairnly is a personal finance app for managing budgets and tracking spend over time. **.NET 10 API +
React SPA**, built on a production-grade foundation: auth (JWT + social logins), EF Core + Postgres,
versioned APIs, observability, and CI/CD to Azure.

## Repo Layout

- `Cairnly.API/` — .NET 10 Web API (EF Core + Identity + Postgres)
- `Cairnly.API.Tests/` — xUnit tests (Moq), mirrored folder structure
- `cairnly-ui/` — React + Vite + TypeScript + Tailwind v4 + HeroUI v3
- `CI/Azure/` — Bicep infra (App Service, App Insights, Key Vault, Postgres)
- `.github/workflows/` — CI + deploy (API → Azure App Service via OIDC, UI → GitHub Pages)

## Commands

Run from repo root unless noted.

**API:** `dotnet build Cairnly.API/Cairnly.API.csproj` · `dotnet test` · `npm run start` (hot reload)

**UI** (from `cairnly-ui/`): `npm i` · `npm run dev` · `npm run lint` · `npm run test` (Vitest) · `npm run test:e2e` (Playwright) · `npm run build`

## API Architecture

- Extension-driven startup: `Program.cs` → `ApplicationStartup/ServiceCollectionExtensions/*` and
  `ApplicationStartup/ApplicationBuilderExtensions/*`.
- Config: `appsettings.json` → env-specific → `appsettings.Local.json` → Azure Key Vault (non-Dev).
- Global auth; use `[AllowAnonymous]` to opt out. URL-segment versioning: `/api/v1/…`, `/api/v2/…`.
- Errors: `ProblemDetailsWithErrors` + `X-Correlation-Id` on every response.
- Service-result pattern: services return `Result<T>`; controllers map failures via
  `ServiceControllerBase.HandleServiceFailureResult`.
- Cursor-based pagination via `CursorPaginatedList<T>` / `ToCursorPaginatedResponse`.
- All async methods must accept and pass `CancellationToken`.

## Auth

- ASP.NET Identity (`User`/`Role`) + JWT bearer access tokens (HMAC-SHA512).
- Social logins: **Google** and **GitHub** (OAuth code exchange → linked account → JWT).
- Refresh tokens stored in an HttpOnly cookie; rotation protected by double-submit CSRF
  (`X-CSRF-Token` header + `csrf_token` cookie). Expired access tokens signal `X-Token-Expired`.

## Data

- DbContext: `Data/DataContext.cs` (Postgres). Repositories extend `Repository<…>` / `IRepository<…>`.
- `DatabaseSeeder` supports CLI args (`seeder` + `migrate`, `seed`, `--password <…>`).

## UI ↔ API Integration

- Base URL: `VITE_API_BASE_URL`.
- `services/axiosConfig.ts`: `X-Correlation-Id`, refresh-token on 401 (`x-token-expired`),
  double-submit CSRF, `withCredentials: true`.
- API calls in `services/api.ts`, cached/managed via TanStack React Query in `hooks/api.ts`.
  Always go through the shared `apiClient` (never raw `fetch`); gate authed queries on `isAuthenticated && !isAuthLoading`.
- Routing uses `HashRouter` (works on GitHub Pages). HeroUI v3 needs no provider; styles via `@heroui/styles`.

## UI Components

- UI must be both desktop and mobile friendly
- The UI uses heroui v3 components. See docs https://heroui.com/react/llms.txt
- Prefer built in components from heroui when available but feel free to make custom components when the heroui components are not suitable.

## Performance

- **Frontend:** lazy-load routes (`React.lazy` + `Suspense`); gate dev-only tooling behind `import.meta.env.DEV` so it tree-shakes out of prod; set a global React Query `staleTime`; use `useInfiniteQuery` for cursor pagination (don't hand-roll); memoize derived arrays; split vendor chunks in `vite.config.ts` (Vite 8/Rolldown → `build.rollupOptions.output.codeSplitting.groups`).
- **Backend:** filter on indexed/normalized columns (`NormalizedUserName`/`NormalizedEmail`) and back every hot filter with an index; fetch only the navigations you need and fold sequential lookups into one query; `ExecuteDeleteAsync` for set deletes, `AsSingleQuery()` for small graphs (global default is `SplitQuery`); use `AddDbContextPool`; reuse stateless objects (`static readonly JsonSerializerOptions`, cached JWT key/credentials/handler).

## Security

- FE and BE run on **different origins** — keep everything cross-origin: auth cookies `SameSite=None; Secure; Domain=<CookieDomain>`; the SPA's build-time CSP `<meta>` `connect-src` must include the API origin (`VITE_API_BASE_URL`).
- OAuth is **backend-initiated**: the server mints a CSPRNG `state` + PKCE verifier bound to an HttpOnly `oauth_flow` cookie and redirects to the provider; callbacks validate `state` with `FixedTimeEquals`.
- JWT signing secret ≥64 bytes (HMAC-SHA512), validated at startup. Use `CryptographicOperations.FixedTimeEquals` + CSPRNG for tokens/secrets; never log tokens.
- Trust `X-Forwarded-*` only from configured proxies; honor `X-Forwarded-Prefix` only against an allowlist. Don't surface internal IDs (correlation/trace) to end users in production.

## Coding Style

- **C#:** Follow `.editorconfig`. `PascalCase` types/members, `I`-prefixed interfaces, `camelCase`
  fields (no `_` prefix). `this.` for instance members. XML docs on public members. Non-entity POCOs →
  `record` with `get; init;`. Repos extend `Repository<…>` / `IRepository<…>`.
- **TypeScript:** ESLint via `eslint.config.js`, format with `npm run prettier`. Prefer method syntax
  `func(): Type {}` over arrow signatures.
- Run `npm run format` from root of repo to format files.

## Testing

- **Backend:** xUnit + Moq in `Cairnly.API.Tests/` (mirrored folders).
- **UI unit:** Vitest (`*.test.ts(x)` / `*.spec.ts(x)` under `src/`).
- **UI e2e:** Playwright in `cairnly-ui/e2e/`.

## Commits & PRs

- Conventional Commits: `feat:`, `fix:`, `chore:`, `test:`. One logical change per commit.
- Flag migration/config impacts.

## Azure

Deploys to Azure **App Service** (API) and **GitHub Pages** (UI). Infra is Bicep under `CI/Azure/`.
Use Azure best-practices tooling when working on Bicep/Key Vault/App Insights.
