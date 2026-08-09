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
- **Key packages:** Blazorise 1.7.5 (Bootstrap5 + FontAwesome), System.Text.Json (built-in)
- **Entry:** `Program.cs` → `App.razor` → `Layout/MainLayout.razor`
- **Pages:**
  - `Pages/Home.razor` — home/landing page (`/`)
  - `Pages/DigitalResume.razor` — resume view fetched from API (`/digitalresume`)
  - `Pages/Projects.v2.razor` — links to projects and external resources (`/projects`)
  - `Pages/GeneralSection.razor` — reusable profile bullet list component
  - `Pages/WorkExperienceSection.razor` — reusable XP section component
  - `Pages/ContactSection.razor` — reusable contact list component
  - `Pages/EducationSection.razor` — reusable education list component
  - `Pages/CustomSections.razor` — reusable custom skills/tech section component
- **Models:** `Models/DigitalResumeModel.cs` — POCOs matching the API JSON shape; uses `System.Text.Json` serialization attributes; `ContactTypeEnum`, `CustomTypeEmun` (note typo in original preserved for compat)
- **Static assets:** `wwwroot/` — `css/app.css`, fonts (CaviarDreams), images, PDFs (`jmResume.4.2025.pdf`, `jmResume.7.2024.pdf`), favicon
- **Config:** `wwwroot/appsettings.Development.json` — `API_Prefix` for local dev; `staticwebapp.config.json` — SWA routing rules
- **Backend URL:** `https://406resumeapi-gqa7cuczcudxdpg6.westus2-01.azurewebsites.net` (hardcoded fallback in `Program.cs`; overridden by `appsettings.Development.json` locally)
- **API call:** `GET /api/resumes/myresume` in `Pages/DigitalResume.razor`
- **Tests:** `BlazorClient.Tests/` — bUnit 1.34.4 (xUnit) project, 24 tests covering ContactSection, DigitalResumePage, GeneralSection, and WorkExperienceSection

### AngularClient (`AngularClient/`)
- **Type:** Angular SPA (standalone components, no NgModules)
- **Framework:** Angular 19 / TypeScript 5.6
- **Build:** `@angular/build:application`
- **Test runner:** `@angular/build:karma` (Karma + Jasmine; updated from `@angular-devkit/build-angular:karma`)
- **Deploy:** Azure Static Web Apps (workflow: `deploy-angular.yml`)
- **Live URL:** https://angular.406jem.com
- **Key packages:** Angular Material 19, Bootstrap 5.3, Bootstrap Icons, ng-bootstrap 18
- **Entry:** `src/main.ts` → `src/app/app.component.ts`
- **Routing:** `src/app/app.routes.ts` — `home`, `digitalresume`, `projects`
- **Components (all standalone):**
  - `app/header/` — nav bar with mobile hamburger menu, logo display
  - `app/home/` — landing page
  - `app/projects/` — projects/links page
  - `app/digital-resume/` — main resume view, fetches data from API
    - `contact-section/` — contact list with Bootstrap Icons
    - `education-section/` — education list
    - `general-section/` — profile bullet list
    - `work-experience-section/` — job cards with hover effects
    - `custom-sections/` — tech/skills lists (input is `customItems`, not `sections`)
  - `app/spinner/` — loading overlay
- **Services:** `app/services/data/resume-data.service.ts` — `HttpClient`-based, calls ResumeFunctions API
- **Interfaces:** `app/interfaces/resume.interface.ts` — TypeScript interfaces mirroring the C# models
- **Styles:** `src/styles.css` — global; each component has its own `.css`
- **Config:** `angular.json`, `tsconfig.json`
- **Backend URL:** `https://406resumeapi-gqa7cuczcudxdpg6.westus2-01.azurewebsites.net` (in `src/environments/environment.prod.ts`)
- **API call:** `GET /api/resumes/myresume` in `resume-data.service.ts`
- **Tests:** 36 Karma/Jasmine specs across all components and the data service; run with `npx ng test --watch=false --browsers=ChromeHeadless`

### ResumeFunctions (`ResumeFunctions/`)
- **Type:** Azure Functions v4 (isolated worker, plain `HostBuilder` — no ASP.NET Core integration)
- **Framework:** .NET 10 / `Microsoft.NET.Sdk`
- **Azure app name:** `406resumeapi`
- **Deploy:** Azure Functions App Service (workflow: `deploy-functions.yml`)
- **Live URL:** https://406resumeapi-gqa7cuczcudxdpg6.westus2-01.azurewebsites.net
- **Staging slot:** `staging` — deployed to before production; URL set in repo variable `AZURE_FUNCTIONS_STAGING_URL`
- **Key packages:** `Microsoft.Azure.Functions.Worker` 2.52.0, `Microsoft.Azure.Functions.Worker.Extensions.Http` 3.3.0, `Microsoft.Azure.Functions.Worker.Sdk` 2.0.7, Newtonsoft.Json 13.0.3
- **Entry:** `Program.cs` — `new HostBuilder().ConfigureFunctionsWorkerDefaults()`, plus `ConfigureServices` registering `WorkerOptions.Serializer` as `JsonObjectSerializer(JsonSerializerDefaults.Web)` so HTTP response bodies are camelCase (see Wire serialization below)
- **Functions:** `ResumeApi.cs` — uses `HttpRequestData`/`HttpResponseData` (not ASP.NET Core types)
  - `myResume` — `GET /api/resumes/myresume` (Anonymous auth) — **primary endpoint used by both clients**
  - `resumes` — `GET /api/resumes` (Function auth) — returns full array
  - Constructor takes an optional `resumeDataPath` (defaults to `Path.Combine(AppContext.BaseDirectory, "StaticData", "Resumes", "JustinMann_062024.json")`), injectable for tests. **Never build this path from `Environment.CurrentDirectory`** — the Linux Functions host doesn't guarantee CWD equals the deployment folder, which previously caused the endpoint to 500 in staging/production.
- **Data:** `StaticData/Resumes/JustinMann_062024.json` — resume JSON source of truth (copied to output on build)
- **Wire serialization:** The isolated worker's `WorkerOptions.Serializer` (used by `WriteAsJsonAsync`, i.e. the actual HTTP response) is set to `JsonSerializerDefaults.Web` (camelCase) in `Program.cs`. This is separate from `JsonFileReader`, which reads the static JSON file with its own serializer instance (Newtonsoft, PascalCase source data) — don't conflate the two when debugging casing issues. Blazor's `GetFromJsonAsync` deserializes case-insensitively so it's unaffected either way; Angular's typed `HttpClient` is not, so a casing mismatch here silently renders blank instead of erroring.
- **CORS:** Not configured — both SWA clients are on different origins. Add if browser CORS errors appear.
- **Default route prefix:** `api` (Azure Functions default for isolated worker — no `routePrefix` override in `host.json`)
- **Tests:** `ResumeFunctions.Tests/` — xUnit project; 11 unit tests + 5 integration tests
  - Integration tests use `[Trait("Category", "Integration")]` to separate from unit tests
  - Integration tests require `FUNCTIONS_STAGING_URL` env var; filtered in CI with `--filter "Category=Integration"`
  - `WriteAsJsonAsync` requires `WorkerOptions.Serializer` registered in `FunctionContext.InstanceServices` — configured in test setup via `services.Configure<WorkerOptions>(opts => opts.Serializer = new JsonObjectSerializer())`

### MyResumeApi (`MyResumeApi/`)
- **Status: DEPRECATED / NOT IN USE**
- This was the original ASP.NET Core Web API backend. It has been replaced by ResumeFunctions as part of the Azure account migration. The project still exists in the repo but is not deployed and has no active workflow.
- **Do not use or deploy this project.** Both clients now point to `406resumeapi.azurewebsites.net` (the Azure Functions app).

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
  GET https://406resumeapi-gqa7cuczcudxdpg6.westus2-01.azurewebsites.net/api/resumes/myresume
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
| `pipeline-stage0-security.yml` | Daily cron / `dependabot_alert` created / manual | Autonomous pipeline — turns open Dependabot alerts into feature-request issues |
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
stage0-security (cron daily / dependabot_alert) → opens issue per vulnerable project, labels "claude","security"
stage1 (label: claude)        → analyzes issue, labels "ready-for-branch"
stage2 (label: ready-for-branch)  → creates feature/issue-N-* branch + draft PR, labels "ready-for-coding"
stage3 (label: ready-for-coding)  → implements + builds + pushes, labels "ready-for-review"
stage4 (label: ready-for-review)  → reviews PR; approves + labels "review-approved", OR requests changes + labels "needs-revision"
stage5 (label: needs-revision, or PR review changes_requested) → addresses feedback, re-labels "ready-for-review" (loops back to stage4)
```

- **Stage 0 (ideate)** checks `gh issue list --label claude --state open` / `gh pr list --state open` first and skips if a prior cycle is still in flight — the weekly cron won't pile up concurrent branches. Web research is capped via `claude_args: "--max-turns 20"` plus an explicit "at most 4 searches" instruction in the prompt.
- **Stage 0 (security)** is a separate, dedicated agent watching GitHub's own Dependabot alerts (`gh api repos/.../dependabot/alerts`) — it does **not** wait for the ideate agent's in-flight check, since a real vulnerability shouldn't queue behind a content idea. It groups all currently-open alerts by `manifest_path` (one issue per affected project, not per-CVE — a single project can have dozens of alerts), dedupes against existing open `security`-labeled issues so it doesn't reopen the same tracking issue every day, and caps itself at 3 new issues per run. If alerts require a breaking/major version bump (no patched version exists on the current major), it says so explicitly and prefixes the title `[Major Upgrade]` — these need closer review than a routine dependency bump. Requires the `CLAUDE_NOTIFY_PAT` to additionally have **"Dependabot alerts: Read-only"** permission granted (set on the PAT itself in GitHub's token settings — not something `gh`/the workflow can grant).
- **Human approval gate:** `main` has no required reviews/status checks, but nothing in stages 0–5 merges a PR — only approves/labels it. Since `deploy-blazor.yml`/`deploy-angular.yml` already build an Azure SWA PR preview on every PR touching their paths (and tear it down on close), the review step is: open the PR's preview URL, check it live, then merge (or close) it yourself. ResumeFunctions has no per-PR preview — only a `staging` slot deployed on push to `main` — so backend-only changes aren't previewable before merge.
- **Why every stage's `github_token` is `CLAUDE_NOTIFY_PAT`, not `GITHUB_TOKEN`:** GitHub does not let the default `GITHUB_TOKEN` trigger another workflow run (anti-recursion protection) — a label added with `GITHUB_TOKEN` would silently fail to fire the next stage's `issues: labeled` trigger. This is the same fix already applied to deploy-pipeline failure notifications (see below); every pipeline stage (0 through 5) uses `secrets.CLAUDE_NOTIFY_PAT` for this reason. **Do not revert any stage to `GITHUB_TOKEN`** — the chain will stop advancing silently (no error, the next stage just never runs) and is easy to miss since each stage looks correct in isolation.
- Labels used as pipeline state: `claude`, `ready-for-branch`, `ready-for-coding`, `ready-for-review`, `needs-revision`, `review-approved`, plus `security` (informational tag, doesn't drive pipeline state) — all pre-created in the repo.

---

## Azure Resources (Current Account)

| Resource | Type | Name / URL |
|----------|------|-----------|
| Blazor frontend | Azure Static Web App | https://406jem.com |
| Angular frontend | Azure Static Web App | https://angular.406jem.com |
| Resume API (production) | Azure Functions App | `406resumeapi` → https://406resumeapi-gqa7cuczcudxdpg6.westus2-01.azurewebsites.net |
| Resume API (staging) | Azure Functions Slot | `406resumeapi/staging` — URL in repo variable `AZURE_FUNCTIONS_STAGING_URL` |

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
  - ResumeFunctions: xUnit + NSubstitute; test project in `ResumeFunctions/ResumeFunctions.Tests/`; `InternalsVisibleTo` exposes `JsonFileReader`; `<Compile Remove="ResumeFunctions.Tests\**" />` prevents main project from picking up test files; tests pass a temp-file path directly into `ResumeApi`'s `resumeDataPath` constructor param rather than mutating `Environment.CurrentDirectory`
  - BlazorClient: bUnit (1.34.4); test project in `BlazorClient/BlazorClient.Tests/`; `BlockingFakeHttpHandler` (using `TaskCompletionSource`) needed to test loading states before async HTTP completes
  - AngularClient: Karma + Jasmine; 36 specs; use `provideRouter([])` in test beds for components with `RouterLink`; use `toHaveBeenCalledTimes(1)` not `toHaveBeenCalledOnce()`
- **Azure Functions deploy:** Always use `dotnet publish` in a single step — never split `dotnet build` + `dotnet publish --no-build`, as this prevents `functions.metadata` from being generated
- **`upload-artifact@v4` hidden files:** The deploy workflow must include `include-hidden-files: true` on the upload step. `actions/upload-artifact@v4.4.0+` excludes hidden folders by default; the `.azurefunctions/` directory (required by the Functions host) starts with `.` and will be silently dropped without this flag, causing "0 functions found (Custom)" at runtime.
- **Claude Code failure notifications:** Deploy workflows create a GitHub issue (via curl + `CLAUDE_NOTIFY_PAT`, since fine-grained PATs can't use `gh issue create`'s GraphQL mutation) then immediately post an `@claude` comment (`gh issue comment` with `GH_TOKEN` also set to `CLAUDE_NOTIFY_PAT`). Both steps use the PAT, not `GITHUB_TOKEN` — see the pipeline token note above for why. Note: `claude-code-action` does not support `push` event contexts and cannot be called inline from a push-triggered workflow.
