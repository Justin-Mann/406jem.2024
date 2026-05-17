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
- **Key packages:** Blazorise 1.7.x (Bootstrap5 + FontAwesome), System.Text.Json (built-in)
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
- **Static assets:** `wwwroot/` — `css/app.css`, fonts (CaviarDreams), images, PDFs, favicon
- **Config:** `wwwroot/appsettings.Development.json` — `API_Prefix` for local dev; `staticwebapp.config.json` — SWA routing rules
- **Backend URL:** `https://406resumeapi-gqa7cuczcudxdpg6.westus2-01.azurewebsites.net` (hardcoded fallback in `Program.cs`; overridden by `appsettings.Development.json` locally)
- **API call:** `GET /api/resumes/myresume` in `Pages/DigitalResume.razor`
- **Tests:** `BlazorClient.Tests/` — bUnit (xUnit) project, 24 tests covering all page components

### AngularClient (`AngularClient/`)
- **Type:** Angular SPA (standalone components, no NgModules)
- **Framework:** Angular 19 / TypeScript 5.6
- **Build:** `@angular/build:application`
- **Test runner:** `@angular/build:karma` (Karma + Jasmine; updated from `@angular-devkit/build-angular:karma`)
- **Deploy:** Azure Static Web Apps (workflow: `deploy-angular.yml`)
- **Live URL:** https://angular.406jem.com
- **Key packages:** Angular Material 19, Bootstrap 5.3, Bootstrap Icons, ng-bootstrap 17
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
- **Entry:** `Program.cs` — `new HostBuilder().ConfigureFunctionsWorkerDefaults().Build().Run()`
- **Functions:** `ResumeApi.cs` — uses `HttpRequestData`/`HttpResponseData` (not ASP.NET Core types)
  - `myResume` — `GET /api/resumes/myresume` (Anonymous auth) — **primary endpoint used by both clients**
  - `resumes` — `GET /api/resumes` (Function auth) — returns full array
- **Data:** `StaticData/Resumes/JustinMann_062024.json` — resume JSON source of truth (copied to output on build)
- **CORS:** Not configured — both SWA clients are on different origins. Add if browser CORS errors appear.
- **Default route prefix:** `api` (Azure Functions default for isolated worker — no `routePrefix` override in `host.json`)
- **Tests:** `ResumeFunctions.Tests/` — xUnit project; 11 unit tests + 5 integration tests
  - Unit tests use `[Trait("Category", "Integration")]` to separate from integration tests
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
| `pipeline-stage1-intake.yml` | Manual / PR | Multi-agent CI/CD pipeline — intake |
| `pipeline-stage2-branch.yml` | Triggered by stage 1 | Multi-agent CI/CD pipeline — branch |
| `pipeline-stage3-implement.yml` | Triggered by stage 2 | Multi-agent CI/CD pipeline — implement |
| `pipeline-stage4-review.yml` | Triggered by stage 3 | Multi-agent CI/CD pipeline — review |
| `pipeline-stage5-iterate.yml` | Triggered by stage 4 | Multi-agent CI/CD pipeline — iterate |

### ResumeFunctions deploy pipeline jobs

```
test (unit) → build → deploy-staging → integration-test → promote
```

- **test** — `dotnet test --filter "Category!=Integration"`; on failure creates GitHub issue + `@claude` comment to invoke Claude Code
- **build** — `dotnet publish` → uploads artifact with `include-hidden-files: true`
- **deploy-staging** — deploys artifact to `staging` slot via `AZURE_RESUMEFUNCTIONS_STAGING_PUBLISH_PROFILE`
- **integration-test** — waits 20s for cold start, then `dotnet test --filter "Category=Integration"` with `FUNCTIONS_STAGING_URL`; on failure creates GitHub issue + `@claude` comment
- **promote** — `az functionapp deployment slot swap` staging → production via `AZURE_CREDENTIALS`

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

**GitHub repository variables required:**
- `AZURE_RESOURCE_GROUP` — resource group that owns `406resumeapi`
- `AZURE_FUNCTIONS_STAGING_URL` — base URL of the staging slot (no trailing slash)

---

## Conventions

- **Blazor:** Razor components in `Pages/` (not using the standard `Components/` structure); no code-behind files; styles scoped inline or in `app.css`
- **Angular:** Standalone components only — no `NgModule`; `inject()` pattern preferred over constructor injection; new `@if`/`@for` control flow (not `*ngIf`/`*ngFor`); signal-based `input()` where feasible; `ComponentRef` must be imported from `@angular/core`, not `@angular/core/testing`
- **.NET version:** .NET 10 across all C# projects
- **Serialization:** System.Text.Json for Blazor; Newtonsoft.Json in ResumeFunctions (Azure SDK compat)
- **Testing:**
  - ResumeFunctions: xUnit + NSubstitute; test project in `ResumeFunctions/ResumeFunctions.Tests/`; `InternalsVisibleTo` exposes `JsonFileReader`; `<Compile Remove="ResumeFunctions.Tests\**" />` prevents main project from picking up test files
  - BlazorClient: bUnit (1.35.x); test project in `BlazorClient/BlazorClient.Tests/`; `BlockingFakeHttpHandler` (using `TaskCompletionSource`) needed to test loading states before async HTTP completes
  - AngularClient: Karma + Jasmine; 36 specs; use `provideRouter([])` in test beds for components with `RouterLink`; use `toHaveBeenCalledTimes(1)` not `toHaveBeenCalledOnce()`
- **Azure Functions deploy:** Always use `dotnet publish` in a single step — never split `dotnet build` + `dotnet publish --no-build`, as this prevents `functions.metadata` from being generated
- **`upload-artifact@v4` hidden files:** The deploy workflow must include `include-hidden-files: true` on the upload step. `actions/upload-artifact@v4.4.0+` excludes hidden folders by default; the `.azurefunctions/` directory (required by the Functions host) starts with `.` and will be silently dropped without this flag, causing "0 functions found (Custom)" at runtime.
- **Claude Code failure notifications:** Deploy workflows create a GitHub issue then immediately post an `@claude` comment (both using `GITHUB_TOKEN`). GitHub allows `issue_comment` events created by `GITHUB_TOKEN` to trigger workflows — this fires `claude-code.yml`. Note: `claude-code-action` does not support `push` event contexts and cannot be called inline from a push-triggered workflow.
