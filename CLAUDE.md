# 406JEM Web Apps — Codebase Map

## Solution Overview

**File:** `406JemWebApps.sln`  
A personal portfolio/resume showcase with two frontend clients (Blazor WASM + Angular SPA) and an Azure Functions REST API. Both frontends render the same digital resume data from the same backend.

---

## Projects

### BlazorClient (`BlazorClient/`)
- **Type:** Blazor WebAssembly (static SPA)
- **Framework:** .NET 10 / `Microsoft.NET.Sdk.BlazorWebAssembly`
- **Root namespace:** `BlazorApp.BlazorClient`
- **Deploy:** Azure Static Web Apps (workflow: `deploy-blazor.yml`)
- **Live URL:** https://406jem.com
- **Key packages:** MudBlazor 7.x (Material component library; registered via `builder.Services.AddMudServices()` in `Program.cs`), Microsoft.AspNetCore.Components.Authorization 10.0.0, System.Text.Json (built-in) — migrated off Blazorise/Bootstrap5/FontAwesome (#43, 2026-08-14); the Blazorise packages and `wwwroot/css/bootstrap/` were removed entirely, not left installed alongside MudBlazor
- **Theme:** `Theme/JemTheme.cs` — a `MudTheme` (`JemTheme.Default`, applied via `MudThemeProvider` in `App.razor`) themed to the existing brand tokens (`#1e2d5a` navy / `#245a8e` blue, Caviar Dreams headings) rather than Material's default Roboto/color-role look — deliberate design constraint from #43, mirrors the approach AngularClient took with Angular Material in #42
- **Entry:** `Program.cs` → `App.razor` (wrapped in `<CascadingAuthenticationState>`) → `Layout/MainLayout.razor`
- **Pages:**
  - `Pages/Home.razor` — home/landing page (`/`)
  - `Pages/DigitalResume.razor` — resume view fetched from API (`/digitalresume`)
  - `Pages/Projects.v2.razor` — links to projects and external resources (`/projects`); renders `GitHubActivitySection` (see GitHub Activity display below)
  - `Pages/GitHubActivitySection.razor` — GitHub Activity card rendered on the Projects page (#68/#69); loading/hidden/ready states, renders nothing (not an empty card) when hidden
  - `Pages/GeneralSection.razor` — reusable profile bullet list component
  - `Pages/WorkExperienceSection.razor` — reusable XP section component
  - `Pages/ContactSection.razor` — reusable contact list component
  - `Pages/EducationSection.razor` — reusable education list component
  - `Pages/CustomSections.razor` — reusable custom skills/tech section component
  - `Pages/Auth/Login.razor` (`/login`), `Pages/Auth/Register.razor` (`/register`) — auth forms; register auto-logs-in on success
  - `Pages/Testimonials.razor` (`/testimonials`) — **the gated feature proving login works end-to-end** (see Auth section below): list is public, the post form only renders inside `<AuthorizeView><Authorized>`, delete button only for `<AuthorizeView Roles="admin">`
- **Models:** `Models/DigitalResumeModel.cs` — POCOs matching the API JSON shape; uses `System.Text.Json` serialization attributes; `ContactTypeEnum`, `CustomTypeEmun` (note typo in original preserved for compat); `Models/AuthModels.cs` — `AuthResponse`, `ErrorResponse`, `TestimonialItem`; `Models/GitHubActivityModels.cs` — `GitHubActivitySettingsDto`, `GitHubRepoModel` (see GitHub Activity display below)
- **Auth (`Services/`):** see the shared "User Accounts (Phase 1)" and "Cross-client cookie session (#47)" sections below — `JwtAuthenticationStateProvider` (custom `AuthenticationStateProvider`; since #47 just holds whatever state `AuthenticationService` last told it, no token decoding), `AuthenticationService` (register/login/logout HTTP calls; hydrates on startup via `GET /api/auth/me`), `SessionCookieHandler` (`DelegatingHandler` that sets fetch credentials + the CSRF header on every request), all registered scoped in `Program.cs`
- **GitHub Activity (`Services/GitHubActivityService.cs`):** see "GitHub Activity display (#68/#69)" below — registered in `Program.cs` with two `HttpClient`s (the DI-registered API client, plus a separate un-credentialed one pointed at `https://api.github.com/`)
- **Static assets:** `wwwroot/` — `css/app.css`, fonts (CaviarDreams), images, PDFs (`jmResume.4.2025.pdf`, `jmResume.7.2024.pdf`), favicon
- **Config:** `wwwroot/appsettings.Development.json` — `API_Prefix` for local dev; `staticwebapp.config.json` — SWA routing rules
- **Backend URL:** `https://api.406jem.com` (hardcoded fallback in `Program.cs`; overridden by `appsettings.Development.json` locally) — custom domain in front of `406resumeapi`, required for #47's cross-subdomain cookie session (same registrable domain as `406jem.com`/`angular.406jem.com`); underlying `406resumeapi-gqa7cuczcudxdpg6.westus2-01.azurewebsites.net` still resolves too
- **API calls:** `GET /api/resumes/myresume` in `Pages/DigitalResume.razor`; `/api/auth/*` and `/api/testimonials*` — see Auth section
- **Tests:** `BlazorClient.Tests/` — bUnit 1.34.4 (xUnit) project, tests covering ContactSection, DigitalResumePage, GeneralSection, WorkExperienceSection, ProjectsPage, and `AuthenticationService`
  - bUnit gotcha: `JSInterop.SetupVoid(...)` (unlike real JS interop) does **not** auto-complete — the awaited `InvokeVoidAsync` call hangs forever unless you chain `.SetVoidResult()`. Cost a live debugging session (dotnet test hung indefinitely with no error) before being traced to this.
  - bUnit + MudBlazor: any component under test that renders MudBlazor components should inherit `Tests/Helpers/MudBunitTestContext.cs` instead of raw bUnit `TestContext` — it calls `Services.AddMudServices()` (MudBlazor's inputs/popover/key-interceptor/resize-observer resolve services from DI) and sets `JSInterop.Mode = JSRuntimeMode.Loose` so MudBlazor's internal JS interop calls don't each need per-call `Setup`/`SetVoidResult()`.

### AngularClient (`AngularClient/`)
- **Type:** Angular SPA (standalone components, no NgModules)
- **Framework:** Angular 22 / TypeScript 6.0 (upgraded 2026-08-09 from Angular 19 via the autonomous pipeline, PR #19, to close a Dependabot-flagged XSS CVE with no fix on the 19.x line — see the pipeline section below)
- **Build:** `@angular/build:application`
- **Test runner:** `@angular/build:karma` (Karma + Jasmine; updated from `@angular-devkit/build-angular:karma`)
- **Deploy:** Azure Static Web Apps (workflow: `deploy-angular.yml`)
- **Live URL:** https://angular.406jem.com
- **Key packages:** Angular Material 22, Bootstrap 5.3, Bootstrap Icons, ng-bootstrap 21
- **Entry:** `src/main.ts` → `src/app/app.component.ts`
- **Routing:** `src/app/app.routes.ts` — `home`, `digitalresume`, `projects` eager; `login`, `register`, `testimonials` lazy via `loadComponent` (keeps the initial bundle from growing further past the pre-existing `angular.json` 600kb budget warning)
- **Components (all standalone):**
  - `app/header/` — nav bar with mobile hamburger menu, logo display; also renders Log In/Register or "Hi {user}"/Log Out based on `AuthService` signals
  - `app/home/` — landing page
  - `app/projects/` — projects/links page; renders `app-github-activity` (see GitHub Activity display below)
  - `app/github-activity/` — GitHub Activity card rendered on the Projects page (#68/#69); `loading`/`repos` signals, renders nothing (not an empty card) once loaded with no result
  - `app/digital-resume/` — main resume view, fetches data from API
    - `contact-section/` — contact list with Bootstrap Icons
    - `education-section/` — education list
    - `general-section/` — profile bullet list
    - `work-experience-section/` — job cards with hover effects
    - `custom-sections/` — tech/skills lists (input is `customItems`, not `sections`)
  - `app/spinner/` — loading overlay
  - `app/auth/login/`, `app/auth/register/` — template-driven (`FormsModule`/`ngModel`) auth forms; register auto-logs-in on success
  - `app/testimonials/` — **the gated feature proving login works end-to-end** (see Auth section below): list is public, the post form only renders when `authService.isAuthenticated()`, delete button only when `authService.isAdmin()`
- **Services:** `app/services/data/resume-data.service.ts` — `HttpClient`-based, calls ResumeFunctions API; `app/services/data/testimonials-data.service.ts` — list/create/delete for testimonials; `app/services/auth/auth.service.ts` — register/login/logout, exposes `isAuthenticated`/`isAdmin`/`username` as signals; since #47, session state is hydrated by calling `GET /api/auth/me` in the constructor (the session cookie itself is httpOnly/unreadable) rather than decoding a stored token; `app/services/auth/auth.interceptor.ts` — functional `HttpInterceptorFn` that sets `withCredentials: true` on every request and, on mutating requests, echoes the `XSRF-TOKEN` cookie into an `X-XSRF-TOKEN` header (registered via `withInterceptors` in `app.config.ts`); `app/services/data/github-activity-data.service.ts` — see "GitHub Activity display (#68/#69)" below; deliberately uses native `fetch()` for the GitHub call, not `HttpClient`, to bypass `auth.interceptor.ts`'s blanket `withCredentials: true`
- **Interfaces:** `app/interfaces/resume.interface.ts` — TypeScript interfaces mirroring the C# models; `app/interfaces/auth.interface.ts` — auth/testimonial request/response shapes; `app/interfaces/github-activity.interface.ts` — `GitHubActivitySettings`, `GitHubRepo`
- **Styles:** `src/styles.css` — global; each component has its own `.css`
- **Config:** `angular.json`, `tsconfig.json`
- **Backend URL:** `https://api.406jem.com` (in `src/environments/environment.prod.ts`) — see BlazorClient's Backend URL note above; same custom domain, same reason
- **API calls:** `GET /api/resumes/myresume` in `resume-data.service.ts`; `/api/auth/*` and `/api/testimonials*` — see Auth section
- **Tests:** Karma/Jasmine specs across all components, the data services, `AuthService`, and `authInterceptor`; run with `npx ng test --watch=false --browsers=ChromeHeadless`

### ResumeFunctions (`ResumeFunctions/`)
- **Type:** Azure Functions v4 (isolated worker, plain `HostBuilder` — no ASP.NET Core integration)
- **Framework:** .NET 10 / `Microsoft.NET.Sdk`
- **Azure app name:** `406resumeapi`
- **Deploy:** Azure Functions App Service (workflow: `deploy-functions.yml`)
- **Live URL:** https://api.406jem.com (custom domain, bound 2026-08-11 for #47's cross-subdomain cookie session — same registrable domain as `406jem.com`/`angular.406jem.com`; underlying `406resumeapi-gqa7cuczcudxdpg6.westus2-01.azurewebsites.net` still resolves too)
- **Staging slot:** `staging` — deployed to before production; URL set in repo variable `AZURE_FUNCTIONS_STAGING_URL`
- **Key packages:** `Microsoft.Azure.Functions.Worker` 2.52.0, `Microsoft.Azure.Functions.Worker.Extensions.Http` 3.3.0, `Microsoft.Azure.Functions.Worker.Sdk` 2.0.7, Newtonsoft.Json 13.0.3, `Azure.Data.Tables` 12.11.0, `System.IdentityModel.Tokens.Jwt` 8.22.0
- **Entry:** `Program.cs` — `new HostBuilder().ConfigureFunctionsWorkerDefaults(...)`, plus `ConfigureServices` registering `WorkerOptions.Serializer` as `JsonObjectSerializer(JsonSerializerDefaults.Web)` so HTTP response bodies are camelCase (see Wire serialization below), the `Auth/` DI services below, and `JwtAuthenticationMiddleware` as a global `IFunctionsWorkerMiddleware`
- **Functions:** `ResumeApi.cs` — uses `HttpRequestData`/`HttpResponseData` (not ASP.NET Core types)
  - `myResume` — `GET /api/resumes/myresume` (Anonymous auth) — **primary endpoint used by both clients, unaffected by the auth work below**
  - `resumes` — `GET /api/resumes` (Function auth) — returns full array
  - Constructor takes an optional `resumeDataPath` (defaults to `Path.Combine(AppContext.BaseDirectory, "StaticData", "Resumes", "JustinMann_062024.json")`), injectable for tests. **Never build this path from `Environment.CurrentDirectory`** — the Linux Functions host doesn't guarantee CWD equals the deployment folder, which previously caused the endpoint to 500 in staging/production.
- **Functions:** `GitHubActivitySettingsApi.cs` — see "GitHub Activity display (#68/#69)" below
- **Data:** `StaticData/Resumes/JustinMann_062024.json` — resume JSON source of truth (copied to output on build)
- **Wire serialization:** The isolated worker's `WorkerOptions.Serializer` (used by `WriteAsJsonAsync`, i.e. the actual HTTP response) is set to `JsonSerializerDefaults.Web` (camelCase) in `Program.cs`. This is separate from `JsonFileReader`, which reads the static JSON file with its own serializer instance (Newtonsoft, PascalCase source data) — don't conflate the two when debugging casing issues. Blazor's `GetFromJsonAsync` deserializes case-insensitively so it's unaffected either way; Angular's typed `HttpClient` is not, so a casing mismatch here silently renders blank instead of erroring.
- **CORS:** Configured (2026-08-11) for the cookie-based cross-client session (#47) — see "Cross-client cookie session (#47)" under the Auth section below.
- **Default route prefix:** `api` (Azure Functions default for isolated worker — no `routePrefix` override in `host.json`)
- **Tests:** `ResumeFunctions.Tests/` — xUnit project; 43 unit tests + 5 integration tests (up from 11 unit tests, all additions are auth/testimonials coverage)
  - Integration tests use `[Trait("Category", "Integration")]` to separate from unit tests
  - Integration tests require `FUNCTIONS_STAGING_URL` env var; filtered in CI with `--filter "Category=Integration"`
  - `WriteAsJsonAsync` requires `WorkerOptions.Serializer` registered in `FunctionContext.InstanceServices` — configured in test setup via `services.Configure<WorkerOptions>(opts => opts.Serializer = new JsonObjectSerializer())`; `Tests/Helpers/TestFunctionContextFactory.cs` centralizes this plus optional pre-authenticated `ClaimsPrincipal` seeding for auth-guard tests
  - `TestHttpRequestData` (in `Tests/Helpers/`) grew optional `body`/`method`/`headers` constructor params (defaulted, so existing GET-only tests are unaffected) to support POST/DELETE requests with JSON bodies and an `Authorization` header

#### User Accounts (Phase 1 — in-app auth, `Auth/` folder)

Issue #25. Two account types — self-registered **visitor** accounts and one seeded **admin** account (not publicly registrable) — backed by a `Role` claim (`AccountRoles.Visitor` / `AccountRoles.Admin`). This is Phase 1 (in-app username/password); a future phase integrates Microsoft Entra ID.

- **Persistence — Azure Table Storage, not a new SQL resource:** `TableUserStore`/`TableTestimonialStore` open a `TableServiceClient` built from the **same `AzureWebJobsStorage` connection string the Functions host already has** for its own bookkeeping (`Program.cs`) — no second storage account or paid SQL resource to provision, matching the Consumption-plan/isolated-worker setup. Two tables, auto-created on first use: `Users` (PartitionKey `"user"`, RowKey = lowercased username) and `Testimonials` (PartitionKey `"testimonial"`, RowKey = a GUID).
- **Password hashing:** `Pbkdf2PasswordHasher` — PBKDF2-SHA256 via the .NET built-in `Rfc2898DeriveBytes.Pbkdf2` (no third-party crypto package needed), 210,000 iterations (OWASP 2023 guidance), format `"{iterations}.{base64 salt}.{base64 hash}"`. Never plaintext, never reversible encryption.
- **Identity provider seam (the Entra extensibility point):** `IIdentityProvider.AuthenticateAsync(username, password)` is implemented today by `LocalPasswordIdentityProvider` only. A future Entra phase adds a second implementation (e.g. validating an Entra-issued token) — `AuthApi`, `JwtAuthTokenService`, and the auth guard middleware don't need to change for that.
- **Login rate limiting / lockout:** persisted **on the `UserAccountEntity` itself** (`FailedLoginAttempts`, `LockoutUntilUtc`), not in an in-memory dictionary — an in-memory counter would reset per Consumption-plan instance and not actually protect anything once the app scales out. 5 failed attempts locks the account for 5 minutes (`LocalPasswordIdentityProvider`).
- **JWT issuance/validation:** `JwtAuthTokenService`, HMAC-SHA256, 2-hour expiry, claims are the literal `System.Security.Claims.ClaimTypes.Name`/`ClaimTypes.Role` URI strings (confirmed by inspection: `System.IdentityModel.Tokens.Jwt` 8.x does **not** apply the old short-name outbound claim mapping by default). Since #47 the JWT itself never leaves the server (httpOnly cookie only — see below); `AuthApi.Me` reads `ClaimTypes.Role` server-side to build the `GET /api/auth/me` response, so that's now the only place the claim type string matters.
- **Auth guard:** `JwtAuthenticationMiddleware` (`IFunctionsWorkerMiddleware`, registered globally in `Program.cs`) parses a `Bearer` token if present on *every* request and stashes the resulting `ClaimsPrincipal` in `FunctionContext.Items` — but a missing/invalid token is never itself an error at the middleware layer, since anonymous endpoints (`myResume`, `resumes`, `GET /api/testimonials`) must keep working unauthenticated. Endpoints that require login check `context.GetAuthenticatedUser()` themselves (`FunctionContextAuthExtensions.cs`) and return 401/403. The header-parsing logic is split into an `internal TryAuthenticate(...)` method specifically so tests don't need to fake the `GetHttpRequestDataAsync()` static extension (which can't be mocked with NSubstitute).
- **Admin seeding:** `AdminAccountSeeder` (`IHostedService`) runs on cold start, reads `Auth:AdminUsername`/`Auth:AdminEmail`/`Auth:AdminPassword` from config, and creates the admin row if it doesn't already exist. Logs a warning and skips (doesn't crash startup) if `Auth:AdminPassword` isn't set.
- **Endpoints:** `AuthApi.cs` — `POST /api/auth/register` (Anonymous trigger, visitor role only), `POST /api/auth/login` (issues the JWT), `POST /api/auth/logout` (204 no-op — JWT is stateless; logout is a client-side token discard, bounded by the 2-hour expiry).
- **The gated feature proving the chain works — Testimonials (`TestimonialsApi.cs`):** `GET /api/testimonials` is public; `POST /api/testimonials` requires any logged-in user; `DELETE /api/testimonials/{id}` requires the `admin` role. This is the minimal end-to-end proof the issue asked for — not a real comments product.
- **Required app settings** (Azure Functions app settings / Key Vault references in prod, `local.settings.json` `Values` locally — `local.settings.json` is gitignored, never commit it): `Auth:JwtSigningKey` (≥32 bytes, `JwtAuthTokenService` throws on startup if missing/short), `Auth:AdminUsername` (defaults to `admin`), `Auth:AdminEmail`, `Auth:AdminPassword` (required for the admin account to be seeded at all), plus the cookie settings below.

#### GitHub Activity display (#68/#69)

A "GitHub Activity" card on the Projects page in both clients, showing recently-updated public repos for whichever admin owns the site's public Projects page — configurable per-admin, hidden entirely unless that admin has turned it on. Split across two issues landed together: #69 (settings storage/API) and #68 (public display). No GitHub API calls happen server-side — `ResumeFunctions` only stores/serves what an admin configured; the actual GitHub repo fetch happens client-side, direct to GitHub's public unauthenticated REST API.

- **Settings storage:** `GitHubActivitySettingsEntity` in a dedicated `GitHubActivitySettings` Table Storage table (same storage account as everything else — see the User Accounts persistence note above), fixed `PartitionKey`, RowKey = normalized owner username, one row per owner. `TableGitHubActivitySettingsStore` implements `IGitHubActivitySettingsStore`.
- **Endpoints (`GitHubActivitySettingsApi.cs`):** `GET`/`PUT /api/github-activity-settings/mine` (requires `AccountRoles.ResumeAdmin` or higher — own settings only); `GET /api/github-activity-settings/public` — resolves the current `SiteConfig.PublicProjectsOwnerId`, returns that owner's settings if `Enabled`, else 404. `GitHubActivitySettingsDto` has no GitHub-fetch fields — just `Enabled`, `GitHubUsername`, `RepoCount` (default 5), `PinnedRepoNames`.
- **Client-side fetch, not a server proxy:** `BlazorClient/Services/GitHubActivityService.cs` and `AngularClient/src/app/services/data/github-activity-data.service.ts` both call `GET /api/github-activity-settings/public` first, then hit `https://api.github.com/users/{username}/repos?sort=pushed&per_page=100&type=owner` directly from the browser. **Both deliberately avoid their app's normal credentialed HTTP client for the GitHub call** — GitHub's API responds with a wildcard CORS origin, which the browser rejects outright on a credentialed request: Blazor constructs a second, separate `HttpClient` (no `SessionCookieHandler`) in `Program.cs`; Angular's service uses native `fetch()` instead of `HttpClient` specifically to bypass `auth.interceptor.ts`'s blanket `withCredentials: true`.
- **Selection algorithm (`GitHubActivityService.SelectRepos` / `selectRepos()`, kept identical in both clients):** filter out forks, then pinned repos first (in pinned order, silently skipping pinned names that don't match any fetched repo), then fill remaining slots up to `RepoCount` by most-recently-pushed (`pushed_at`). Pinned repos count toward `RepoCount`, they don't add to it.
- **Fail silently, not visibly:** any failure — settings fetch, GitHub fetch, feature disabled, no settings configured — resolves to `null`/no rendering. Both `GitHubActivitySection.razor` (Blazor) and `GitHubActivityComponent` (Angular) render nothing at all in that case, not an empty or broken-looking card. A loading state shows while the fetch is in flight.

#### Cross-client cookie session (#47)

Issue #47 replaced the bearer-token/`sessionStorage` model above with a shared browser session, since `406jem.com` and `angular.406jem.com` are the same registrable domain.

- **Session cookie, not a token in the response body:** `POST /api/auth/login` sets an httpOnly, `SameSite=Lax` cookie (`406jem_auth`, see `Auth/Cookies/CookieNames.cs`) carrying the JWT, built/cleared by `Auth/Cookies/AuthCookieService.cs`. `AuthResponse`/`Dtos.cs` no longer has a `Token` field — the JWT is never in a response body a script could read. `JwtAuthenticationMiddleware` now reads the cookie first, falling back to an `Authorization: Bearer` header only if no cookie is present (kept for a future non-browser/bearer client, e.g. the Kotlin mobile app in #34, which can't share a browser cookie jar regardless).
- **`GET /api/auth/me`:** returns `{ username, role }` for the current session or 401. Since the cookie is unreadable from JS by design, both clients call this once on startup (`AuthenticationService.InitializeAsync()` in Blazor, the `AuthService` constructor in Angular) to hydrate local auth state instead of decoding a stored token. `BlazorClient/Services/JwtClaimsParser.cs` and `AngularClient/src/app/services/auth/jwt.util.ts` were deleted — there's no client-side token to decode anymore.
- **`Auth:CookieDomain` app setting:** set to `406jem.com` on the **production** slot only (done 2026-08-11) — a `Domain=406jem.com` attribute is invalid (and silently rejected by the browser) on a response from any other host, including the `staging` slot and local dev. Leave unset on staging/local, which yields a host-only cookie. This required `406resumeapi`'s production slot to first be reachable at a `406jem.com` subdomain — see the custom domain note under ResumeFunctions above (`api.406jem.com`, bound 2026-08-11); setting this before the custom domain existed would have made browsers silently reject the cookie entirely.
- **`Auth:CookieSecure` app setting (new, optional):** defaults to `true` (Secure cookie). Set to `false` only for local HTTP dev — a `Secure` cookie is silently dropped by the browser on a non-https origin.
- **CSRF (new):** `Auth/Middleware/CsrfProtectionMiddleware.cs` is a double-submit-cookie check — every POST/PUT/PATCH/DELETE that carries the `406jem_auth` cookie must also echo the non-httpOnly `XSRF-TOKEN` cookie's value in an `X-XSRF-TOKEN` header, or it gets a 403. `/api/auth/login` and `/api/auth/register` are exempt (no session cookie exists yet to double-submit against). Both frontends' interceptors/handlers (`AngularClient/src/app/services/auth/auth.interceptor.ts`, `BlazorClient/Services/SessionCookieHandler.cs`) attach this automatically on every mutating request — pages/components don't need to do anything themselves.
- **CORS is a manual Azure Functions app setting, not code:** Azure Functions' CORS (including `Access-Control-Allow-Credentials`, needed for a cookie-based session) is a platform-level Function App setting (Portal → CORS blade, or `az functionapp cors add`/`az functionapp cors credentials`) — isolated-worker app code can't implement it itself, since preflight `OPTIONS` requests are answered by the Functions host before they ever reach the worker process. **Configured 2026-08-11**: production's CORS allowed-origins list includes `https://406jem.com` and `https://angular.406jem.com` with "Access-Control-Allow-Credentials" enabled. Full chain verified end-to-end in production the same day: login sets a `406jem_auth` cookie with `domain=406jem.com`, `GET /api/auth/me` resolves it correctly, and the anonymous `myResume`/`resumes` endpoints remain unaffected.

### Client (`Client/`)
- **Type:** Legacy stub — only contains `Pages/Home.razor`; leftover from initial scaffolding
- **Not part of active builds**

---

## Shared Design Language

Both frontend clients maintain visual/functional parity:
- **Color palette:** `#1e2d5a` (nav/heading dark navy), `#245a8e` (hover/link blue), `#ced7eb` (note backgrounds), white/light-grey cards
- **Typography:** "Caviar Dreams" custom font (`CaviarDreams.ttf`) for headings and callout text; system sans-serif for body
- **Navigation:** horizontal link bar on desktop, hamburger drawer on mobile
- **Layout:** container/row/col Bootstrap grid; footer image (bojack-samuri_bookends) fixed to bottom with low opacity
- **Resume layout:** 8-col main column (profile + XP) + 4-col sidebar (contact, education, custom sections)
- **Card hover:** XP cards subtly lift/glow on hover

---

## Data Flow

```
StaticData/Resumes/JustinMann_062024.json
            ↓
  ResumeFunctions (Azure Functions, isolated worker)
  GET https://api.406jem.com/api/resumes/myresume
            ↓
  BlazorClient (https://406jem.com)  |  AngularClient (https://angular.406jem.com)
```

---

## CI/CD Workflows (`.github/workflows/`)

| File | Trigger | Description |
|------|---------|-------------|
| `deploy-blazor.yml` | push to `main` (BlazorClient/** paths) | Test → deploy Blazor to Azure SWA |
| `deploy-angular.yml` | push to `main` (AngularClient/** paths) | Test → deploy Angular to Azure SWA |
| `deploy-functions.yml` | push to `main` (ResumeFunctions/** paths) | Unit test → build → deploy staging → integration test → promote to production |
| `claude-code.yml` | `issue_comment`, `issues`, PR events | Claude Code agent — responds to `@claude` mentions; also fires when deploy pipelines post failure comments |
| `claude-review.yml` | PR events | Claude automated review |
| `claude-maintain-md.yml` | push to `main` | Auto-update CLAUDE.md via Claude |
| `pipeline-stage0-ideate.yml` | Weekly cron (Mon 15:00 UTC) / manual | Autonomous pipeline — researches and proposes one site improvement |
| `pipeline-stage0-security.yml` | Daily cron (13:00 UTC) / manual | Autonomous pipeline — turns open Dependabot alerts into feature-request issues |
| `pipeline-stage1-intake.yml` | Issue labeled `claude` | Autonomous pipeline — intake |
| `pipeline-stage2-branch.yml` | Issue labeled `ready-for-branch` | Autonomous pipeline — branch + draft PR |
| `pipeline-stage3-implement.yml` | Issue labeled `ready-for-coding` | Autonomous pipeline — implement |
| `pipeline-stage4-review.yml` | Issue labeled `ready-for-review` | Autonomous pipeline — review |
| `pipeline-stage5-iterate.yml` | Issue labeled `needs-revision`, or PR review `changes_requested` | Autonomous pipeline — iterate on feedback |

### ResumeFunctions deploy pipeline jobs

```
test (unit) → build → deploy-staging → integration-test → promote
```

- **test** — `dotnet test --filter "Category!=Integration"`; on failure creates GitHub issue + `@claude` comment to invoke Claude Code
- **build** — `dotnet publish` → uploads artifact with `include-hidden-files: true`
- **deploy-staging** — deploys artifact to `staging` slot via `AZURE_RESUMEFUNCTIONS_STAGING_PUBLISH_PROFILE`
- **integration-test** — waits 20s for cold start, then `dotnet test --filter "Category=Integration"` with `FUNCTIONS_STAGING_URL`; on failure creates GitHub issue + `@claude` comment
- **promote** — `az functionapp deployment slot swap` staging → production via `AZURE_CREDENTIALS`

### Autonomous site-improvement pipeline (`pipeline-stage0*.yml`, `pipeline-stage1-5*.yml`)

Fully autonomous loop: an agent proposes an idea (or a security agent surfaces a vulnerability), another implements it, another reviews it, and the result lands as a normal PR against `main` — with an Azure SWA PR preview environment for both frontends — for a human to approve or reject. Nothing in this chain merges to `main` on its own.

```
stage0-ideate (cron, weekly)      → opens issue, labels "claude"
stage0-security (cron, daily)     → opens issue per vulnerable project, labels "claude","security"
stage1 (label: claude)        → analyzes issue, labels "ready-for-branch"
stage2 (label: ready-for-branch)  → creates feature/issue-N-* branch + draft PR, labels "ready-for-coding"
stage3 (label: ready-for-coding)  → implements + builds + pushes, labels "ready-for-review"
stage4 (label: ready-for-review)  → reviews PR; approves + labels "review-approved", OR requests changes + labels "needs-revision"
stage5 (label: needs-revision, or PR review changes_requested) → addresses feedback, re-labels "ready-for-review" (loops back to stage4)
```

- **Stage 0 (ideate)** checks `gh issue list --label claude --state open` / `gh pr list --state open` first and skips if a prior cycle is still in flight — the weekly cron won't pile up concurrent branches. Web research is capped via `claude_args: "--max-turns 20"` plus an explicit "at most 4 searches" instruction in the prompt.
- **Stage 0 (security)** is a separate, dedicated agent watching GitHub's own Dependabot alerts (`gh api repos/.../dependabot/alerts`) — it does **not** wait for the ideate agent's in-flight check, since a real vulnerability shouldn't queue behind a content idea. It groups all currently-open alerts by `manifest_path` (one issue per affected project, not per-CVE — a single project can have dozens of alerts), dedupes against existing open `security`-labeled issues so it doesn't reopen the same tracking issue every day, and caps itself at 3 new issues per run. If alerts require a breaking/major version bump (no patched version exists on the current major), it says so explicitly and prefixes the title `[Major Upgrade]` — these need closer review than a routine dependency bump. Requires the `CLAUDE_NOTIFY_PAT` to additionally have **"Dependabot alerts: Read-only"** permission granted (set on the PAT itself in GitHub's token settings — not something `gh`/the workflow can grant). Triggered by daily cron + `workflow_dispatch` only — **`dependabot_alert` is not a valid Actions trigger** (not in GitHub's events-that-trigger-workflows list); an earlier version tried it and GitHub rejected the entire workflow file at parse time (a `startup_failure` completing in 0s, distinguishable from an actual run failure). Don't reintroduce it.
- **Human approval gate:** `main` has no required reviews/status checks, but nothing in stages 0–5 merges a PR — only approves/labels it. Since `deploy-blazor.yml`/`deploy-angular.yml` already build an Azure SWA PR preview on every PR touching their paths (and tear it down on close), the review step is: open the PR's preview URL, check it live, then merge (or close) it yourself. ResumeFunctions has no per-PR preview — only a `staging` slot deployed on push to `main` — so backend-only changes aren't previewable before merge.
- **Why every stage's `github_token` is `CLAUDE_NOTIFY_PAT`, not `GITHUB_TOKEN`:** GitHub does not let the default `GITHUB_TOKEN` trigger another workflow run (anti-recursion protection) — a label added with `GITHUB_TOKEN` would silently fail to fire the next stage's `issues: labeled` trigger. This is the same fix already applied to deploy-pipeline failure notifications (see below); every pipeline stage (0 through 5) uses `secrets.CLAUDE_NOTIFY_PAT` for this reason. **Do not revert any stage to `GITHUB_TOKEN`** — the chain will stop advancing silently (no error, the next stage just never runs) and is easy to miss since each stage looks correct in isolation.
- **Why every stage also sets `claude_args: "--permission-mode bypassPermissions"`:** discovered when the first live test of `pipeline-stage0-security.yml` came back `conclusion: success` after 4 turns and ~12 seconds having created nothing — the run's `permission_denials_count` was 3. In a headless/scheduled context there's no human to answer a tool-use permission prompt, so Claude Code's default permission mode silently denies the tool call (e.g. every `gh` command) and the agent just stops, and the action still reports overall success. `--permission-mode bypassPermissions` (equivalent to `--dangerously-skip-permissions`) is required on every stage for this pipeline to do anything at all. If a stage run finishes fast with an oddly small `num_turns` and nothing changed (no issue/PR/comment appeared), check `permission_denials_count` in the run log before assuming the prompt logic is at fault.
- Labels used as pipeline state: `claude`, `ready-for-branch`, `ready-for-coding`, `ready-for-review`, `needs-revision`, `review-approved`, plus `security` (informational tag, doesn't drive pipeline state) — all pre-created in the repo.
- **Stage 4 checks CI status before code quality, not instead of it:** first full end-to-end test (2026-08-09, issue #18/PR #19, the Angular 19→22 major upgrade the security agent surfaced) showed Stage 4 approving a PR whose latest commit had a *failed* `deploy-angular.yml` check, because it was only reading the diff, not the checks. Fixed by having Stage 4 run `gh pr checks <PR> --watch` first and treat any failing check as automatic "changes needed" regardless of code quality. **Agents can't fix everything themselves:** the actual failure was `deploy-angular.yml` pinning Node 20 while the upgraded Angular 22 CLI requires Node ≥22.22.3 — a CI workflow file fix, which `CLAUDE_NOTIFY_PAT` structurally cannot push (no `Workflows` permission granted, by design — see the PAT permissions table above). That class of fix needs a human. Confirmed in the same test run that this restriction actually holds: no stage attempted or was able to touch `.github/workflows/**`.
- **Confirmed working (2026-08-09 live test):** the full chain — stage0-security → stage1 → stage2 → stage3 → stage4 (request changes) → stage5 (fix) → stage4 (approve) — ran end to end with zero manual intervention beyond the initial `workflow_dispatch`, and correctly cycled through a real review/revise loop rather than rubber-stamping. The pre-existing `deploy-angular.yml` failure-notification path and Stage 4's review fired concurrently on the same PR without colliding — the generic `claude-code.yml` correctly stayed out of the way because Stage 4's review body didn't contain `@claude`.
- **`deploy-angular.yml` builds in the Actions runner now, not Azure's remote Oryx build** (`skip_app_build: true`, `app_location` pointed straight at `./AngularClient/dist/angular-client/browser`, no `output_location`). Also discovered during the same 2026-08-09 test: Azure's Oryx build only supports a fixed, curated list of Node versions, and as of that date none of them satisfy Angular 22's own minimum (needs 22.22.3+, 24.15.0+, or 26.0.0+; Oryx topped out at 22.22.0 and 24.13.0) — a real platform gap, not a config typo, so don't "fix" it by fiddling with `engines.node` again. If `skip_app_build` is ever removed, expect this to resurface for any future Angular major bump. Note also: with `skip_app_build: true`, `app_location` must point directly at the pre-built output — `output_location` is not applied relative to it the way it is during a normal Oryx build (learned by getting "Failed to find a default file" first).

---

## Azure Resources (Current Account)

| Resource | Type | Name / URL |
|----------|------|-----------|
| Blazor frontend | Azure Static Web App | https://406jem.com |
| Angular frontend | Azure Static Web App | https://angular.406jem.com |
| Resume API (production) | Azure Functions App | `406resumeapi` → https://api.406jem.com (custom domain; underlying https://406resumeapi-gqa7cuczcudxdpg6.westus2-01.azurewebsites.net still resolves) |
| Resume API (staging) | Azure Functions Slot | `406resumeapi/staging` — URL in repo variable `AZURE_FUNCTIONS_STAGING_URL` |
| User accounts / testimonials storage | Azure Table Storage | Reuses the storage account already backing `AzureWebJobsStorage` for `406resumeapi` — no separate resource |

**GitHub secrets required:**
- `AZURE_STATIC_WEB_APPS_API_TOKEN_*` — SWA deploy tokens (one per SWA, auto-named by Azure)
- `AZURE_RESUMEFUNCTIONS_PUBLISH_PROFILE` — publish profile for `406resumeapi` production slot
  - SCM Basic Auth must be **enabled** on the Functions App (Azure Portal → Configuration → General settings → SCM Basic Auth Publishing Credentials → On)
- `AZURE_RESUMEFUNCTIONS_STAGING_PUBLISH_PROFILE` — publish profile for the `staging` slot
- `AZURE_CREDENTIALS` — service principal JSON for slot swap (`az ad sp create-for-rbac --role contributor`)
- `CLAUDE_CODE_OAUTH_TOKEN` — Claude Code OAuth token for automated PR fixes
- `CLAUDE_NOTIFY_PAT` — fine-grained PAT (issues/PRs/contents write) used anywhere an automated action needs to create/label an issue or PR *and* have that action trigger a further workflow run — deploy-failure notifications and all 6 stages of the autonomous pipeline. Must stay a real PAT; `GITHUB_TOKEN` cannot substitute (see pipeline token note above)

**GitHub repository variables required:**
- `AZURE_RESOURCE_GROUP` — resource group that owns `406resumeapi`
- `AZURE_FUNCTIONS_STAGING_URL` — base URL of the staging slot (no trailing slash)

---

## Conventions

- **Blazor:** Razor components in `Pages/` (not using the standard `Components/` structure); no code-behind files; styles scoped inline or in `app.css`
- **Angular:** Standalone components only — no `NgModule`; `inject()` pattern preferred over constructor injection; new `@if`/`@for` control flow (not `*ngIf`/`*ngFor`); signal-based `input()` where feasible; `ComponentRef` must be imported from `@angular/core`, not `@angular/core/testing`
- **.NET version:** .NET 10 across all C# projects
- **Serialization:** System.Text.Json for Blazor; ResumeFunctions uses Newtonsoft.Json (`JsonFileReader`) to read the static resume file, but the HTTP wire format is System.Text.Json camelCase via `WorkerOptions.Serializer` (see Wire serialization above) — the two serializers are independent, don't assume a fix to one affects the other
- **Testing:**
  - ResumeFunctions: xUnit + NSubstitute; test project in `ResumeFunctions/ResumeFunctions.Tests/`; `InternalsVisibleTo` exposes `JsonFileReader` (and now the `internal` auth-guard methods in `Auth/Middleware/`); `<Compile Remove="ResumeFunctions.Tests\**" />` prevents main project from picking up test files; tests pass a temp-file path directly into `ResumeApi`'s `resumeDataPath` constructor param rather than mutating `Environment.CurrentDirectory`; `Tests/Helpers/FakeUserStore.cs` is a plain in-memory `IUserStore` (not an NSubstitute mock) for tests that need real stateful mutation, e.g. lockout counters incrementing across calls
  - BlazorClient: bUnit (1.34.4); test project in `BlazorClient/BlazorClient.Tests/`; `BlockingFakeHttpHandler` (using `TaskCompletionSource`) needed to test loading states before async HTTP completes; components rendering MudBlazor should inherit `Tests/Helpers/MudBunitTestContext.cs`, not raw bUnit `TestContext` (see BlazorClient's Tests note above); see the bUnit `SetupVoid`/`.SetVoidResult()` gotcha noted above
  - AngularClient: Karma + Jasmine; 64 specs; use `provideRouter([])` in test beds for components with `RouterLink`; use `toHaveBeenCalledTimes(1)` not `toHaveBeenCalledOnce()`; RxJS/Jasmine gotcha — `let x: T | null = null; obs.subscribe(v => x = v); /* sync flush */ expect(x)...` can fail to compile (TS2345, "Expected<null>") because TypeScript's control-flow analysis narrows `x` to the literal `null` type at the initializer and doesn't see the closure's reassignment as reachable; declare without a literal initializer instead (`let x: T | null | undefined;`) as done in `auth.service.spec.ts`
- **Azure Functions deploy:** Always use `dotnet publish` in a single step — never split `dotnet build` + `dotnet publish --no-build`, as this prevents `functions.metadata` from being generated
- **`upload-artifact@v4` hidden files:** The deploy workflow must include `include-hidden-files: true` on the upload step. `actions/upload-artifact@v4.4.0+` excludes hidden folders by default; the `.azurefunctions/` directory (required by the Functions host) starts with `.` and will be silently dropped without this flag, causing "0 functions found (Custom)" at runtime.
- **Claude Code failure notifications:** Deploy workflows create a GitHub issue (via curl + `CLAUDE_NOTIFY_PAT`, since fine-grained PATs can't use `gh issue create`'s GraphQL mutation) then immediately post an `@claude` comment (`gh issue comment` with `GH_TOKEN` also set to `CLAUDE_NOTIFY_PAT`). Both steps use the PAT, not `GITHUB_TOKEN` — see the pipeline token note above for why. Note: `claude-code-action` does not support `push` event contexts and cannot be called inline from a push-triggered workflow.
