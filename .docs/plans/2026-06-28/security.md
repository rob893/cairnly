# Security Research — 2026-06-28

## Executive Summary

Cairnly's security posture is strong: tenant isolation is enforced both in repositories (user-scoped `Where` clauses) and services (`CanAccess`/`IsUserAuthorizedForResource`), comparisons are timing-safe, OAuth uses CSPRNG state + PKCE, JWTs are HMAC-SHA512 with audience/issuer/lifetime validation, access tokens live in memory (not localStorage), CSP blocks inline script, errors are generic, and X-Forwarded-* is trusted only from an allowlist. No new Critical/High findings this cycle. The items below are Medium/Low hardening gaps — primarily an OAuth ID-token audience that is unvalidated in config, PII (email) written to logs, and a rate-limit gap on anonymous password endpoints.

## Status

| #   | Finding                                              | Impact | Effort | Status         |
| --- | --------------------------------------------------- | ------ | ------ | -------------- |
| 1   | Google ID-token audience validation disabled        | Medium | Low    | ✅ Done (W1)   |
| 2   | PII (emails) logged in account-recovery flows       | Medium | Low    | ✅ Done (W1)   |
| 3   | Anonymous password endpoints bypass strict bucket   | Low    | Low    | ✅ Done (W1)   |
| 4   | Logout leaves CSRF/OAuth cookies + JWT usable       | Low    | Low    | ✅ Done (W1)   |
| 5   | CSP meta frame-ancestors; CORS AllowAnyHeader       | Low    | Medium | ⬜ Pending     |

## Findings

### 1. Google ID-token audience validation effectively disabled (empty audience list)
- **Description:** `ValidateIdTokenAsync` sets `ValidationSettings.Audience = authSettings.GoogleOAuthAudiences`, but `GoogleOAuthAudiences` is `[]` in `appsettings.json` and is not set in `appsettings.Production.json`. The Google library skips the audience check when the list is empty, so a signed Google ID token minted for *any* OAuth client would pass validation. Exploitation requires injecting a foreign ID token into the exchange (mitigated by PKCE + our client secret), so impact is bounded, but the audience pin is a defense-in-depth control that is currently off. There is no startup guard requiring the audience to match `GoogleOAuthClientId`.
- **Location:** `Cairnly.API/Services/Auth/GoogleOAuthService.cs:69-79`; config `Cairnly.API/appsettings.json:35`; not overridden in `appsettings.Production.json:5-12`.
- **Impact:** Medium
- **Effort:** Low (<1hr)
- **Dependencies:** None
- **Breaking Changes:** No (set the value to the existing Google client id)
- **Recommendation:** Default `GoogleOAuthAudiences` to `[GoogleOAuthClientId]` and validate at startup that the list is non-empty (mirroring the APISecret length check in `AuthenticationServiceCollectionExtensions`). Fail fast if empty in non-dev environments.

### 2. PII (email addresses) written to logs in account-recovery flows
- **Description:** Forgot-password, reset-password, and confirm-email log the raw email address. App Insights ingestion (50% sampling) persists these warning/error rows, so a log compromise exposes the email of every user who triggered recovery and links emails to user ids. The rest of the app deliberately logs only `UserId`, so this is inconsistent with the established standard.
- **Location:** `Cairnly.API/Services/Domain/UserService.cs:409-411` (forgot), `451-454` (reset), `469` and `477-478` (confirm).
- **Impact:** Medium
- **Effort:** Low (<1hr)
- **Dependencies:** None
- **Breaking Changes:** No
- **Recommendation:** Log `user.Id` (or a hash) instead of `request.Email`/`user.Email`. For the "user not found" paths, omit the identifier entirely to avoid storing attacker-supplied emails.

### 3. Anonymous password endpoints bypass the strict auth rate-limit bucket
- **Description:** The rate limiter only tightens partitions for paths under `/api/v1/auth/` (and `/register`). `POST /api/v1/users/forgotPassword` and `POST /api/v1/users/resetPassword` are `[AllowAnonymous]` but fall into the default bucket (50 req / 15s). Per-user throttles inside the service blunt email-bombing, but the reset-token submission endpoint is left at the generous default. They should share the stricter auth limits.
- **Location:** `Cairnly.API/ApplicationStartup/ServiceCollectionExtensions/RateLimiterServiceCollectionExtensions.cs:46-70`; endpoints `Cairnly.API/Controllers/V1/UsersController.cs:295-325`.
- **Impact:** Low
- **Effort:** Low (<1hr)
- **Dependencies:** None
- **Breaking Changes:** No
- **Recommendation:** Add path matches for `/api/v1/users/forgotpassword`, `/resetpassword`, and `/emailconfirmations` to the strict bucket, or move recovery endpoints under `/auth`.

### 4. Logout leaves CSRF/OAuth cookies and access JWT usable
- **Description:** `LogoutAsync` revokes refresh tokens and deletes the `refresh_token` cookie but leaves `csrf_token` and `oauth_flow` cookies, and the bearer JWT stays valid for its full 60-minute lifetime (no revocation list). A token captured before logout still authorizes requests until expiry. Acceptable for a hobby app, but worth tightening given financial data.
- **Location:** `Cairnly.API/Controllers/V1/AuthController.cs:477-504`; lifetime `appsettings.json:33`.
- **Impact:** Low
- **Effort:** Low (<1hr)
- **Dependencies:** None
- **Breaking Changes:** No
- **Recommendation:** Also delete `csrf_token` (and `oauth_flow`) on logout; consider shortening access-token lifetime to ~15 min so the post-logout window is small.

### 5. CSP delivered via `<meta>` cannot set frame-ancestors; CORS allows any header
- **Description:** Production CSP is injected via `<meta http-equiv>`, which cannot enforce `frame-ancestors`, so clickjacking protection depends on a host-layer `X-Frame-Options`/`frame-ancestors` that is not in the repo. CORS also uses `AllowAnyHeader()` with credentials; origins are correctly restricted, so impact is minor, but headers should be allowlisted defensively.
- **Location:** `cairnly-ui/src/utils/csp.ts:25-37`; `Cairnly.API/ApplicationStartup/ApplicationBuilderExtensions/CorsApplicationBuilderExtensions.cs:19-24`.
- **Impact:** Low
- **Effort:** Medium (1-4hr, infra coordination)
- **Dependencies:** None
- **Breaking Changes:** No
- **Recommendation:** Add `X-Frame-Options: DENY` / CSP `frame-ancestors 'none'` at nginx/App Service. Replace `AllowAnyHeader()` with an explicit allowlist (Authorization, Content-Type, X-CSRF-Token, X-Correlation-Id).
