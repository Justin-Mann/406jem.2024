# 406JEM Web Apps — Codebase Map

## Solution Overview

**File:** `406JemWebApps.sln`  
A personal portfolio/resume showcase with two frontend clients (Blazor WASM + Angular SPA) and an Azure Functions REST API. Both frontends render the same digital resume data from the same backend.

---

## Projects

### BlazorClient (`BlazorClient/`)
- **Type:** Blazor WebAssembly (static SPA)
- **Framework:** .NET 9 / `Microsoft.NET.Sdk.BlazorWebAssembly`
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
- **Backend URL:** `https://406resumeapi.azurewebsites.net` (hardcoded fallback in `Program.cs`; overridden by `appsettings.Development.json` locally)
- **API call:** `GET /api/resumes/myresume` in `Pages/DigitalResume.razor`

### AngularClient (`AngularClient/`)
- **Type:** Angular SPA (standalone components, no NgModules)
- **Framework:** Angular 19 / TypeScript 5.6
- **Build:** `@angular/build:application`
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
    - `custom-sections/` — tech/skills lists
  - `app/spinner/` — loading overlay
- **Services:** `app/services/data/resume-data.service.ts` — `HttpClient`-based, calls ResumeFunctions API
- **Interfaces:** `app/interfaces/resume.interface.ts` — TypeScript interfaces mirroring the C# models
- **Styles:** `src/styles.css` — global; each component has its own `.css`
- **Config:** `angular.json`, `tsconfig.json`
- **Backend URL:** `https://406resumeapi.azurewebsites.net` (in `src/environments/environment.prod.ts`)
- **API call:** `GET /api/resumes/myresume` in `resume-data.service.ts`

### ResumeFunctions (`ResumeFunctions/`)
- **Type:** Azure Functions v4 (isolated worker, ASP.NET Core integration)
- **Framework:** .NET 9 / `Microsoft.NET.Sdk`
- **Azure app name:** `406resumeapi`
- **Deploy:** Azure Functions App Service (workflow: `deploy-functions.yml`)
- **Live URL:** https://406resumeapi.azurewebsites.net
- **Key packages:** `Microsoft.Azure.Functions.Worker` 2.0.0, `Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore` 2.0.0, `Microsoft.Azure.Functions.Worker.Sdk` 2.0.0, Newtonsoft.Json 13.0.3
- **Entry:** `Program.cs` — `FunctionsApplication.CreateBuilder` + `ConfigureFunctionsWebApplication()`
- **Functions:** `ResumeApi.cs`
  - `myResume` — `GET /api/resumes/myresume` (Anonymous auth) — **primary endpoint used by both clients**
  - `resumes` — `GET /api/resumes` (Function auth) — returns full array
- **Data:** `StaticData/Resumes/JustinMann_062024.json` — resume JSON source of truth (copied to output on build)
- **CORS:** `builder.Services.AddCors()` is configured but `app.UseCors()` is **not called** — CORS middleware is not active. This needs to be fixed if browser clients start hitting CORS errors.
- **Default route prefix:** `api` (Azure Functions default for isolated worker — no `routePrefix` override in `host.json`)

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
  GET https://406resumeapi.azurewebsites.net/api/resumes/myresume
            ↓
  BlazorClient (https://406jem.com)  |  AngularClient (https://angular.406jem.com)
```

---

## CI/CD Workflows (`.github/workflows/`)

| File | Trigger | Target |
|------|---------|--------|
| `deploy-blazor.yml` | push to `main` (BlazorClient/** paths) | Blazor → Azure SWA |
| `deploy-angular.yml` | push to `main` (AngularClient/** paths) | Angular → Azure SWA |
| `deploy-functions.yml` | push to `main` (ResumeFunctions/** paths) | ResumeFunctions → Azure Functions |
| `claude-code.yml` | PR events | Claude Code PR review/assistance |
| `claude-review.yml` | PR events | Claude automated review |
| `claude-maintain-md.yml` | push to `main` | Auto-update CLAUDE.md via Claude |
| `pipeline-stage1-intake.yml` | Manual / PR | Multi-agent CI/CD pipeline — intake |
| `pipeline-stage2-branch.yml` | Triggered by stage 1 | Multi-agent CI/CD pipeline — branch |
| `pipeline-stage3-implement.yml` | Triggered by stage 2 | Multi-agent CI/CD pipeline — implement |
| `pipeline-stage4-review.yml` | Triggered by stage 3 | Multi-agent CI/CD pipeline — review |
| `pipeline-stage5-iterate.yml` | Triggered by stage 4 | Multi-agent CI/CD pipeline — iterate |

---

## Azure Resources (Current Account)

| Resource | Type | Name / URL |
|----------|------|-----------|
| Blazor frontend | Azure Static Web App | https://406jem.com |
| Angular frontend | Azure Static Web App | https://angular.406jem.com |
| Resume API | Azure Functions App | `406resumeapi` → https://406resumeapi.azurewebsites.net |

**Secrets required in GitHub repo:**
- `AZURE_STATIC_WEB_APPS_API_TOKEN_*` — SWA deploy tokens (one per SWA, auto-named by Azure)
- `AZURE_RESUMEFUNCTIONS_PUBLISH_PROFILE` — publish profile for `406resumeapi` Functions App
  - SCM Basic Auth must be **enabled** on the Functions App (Azure Portal → Configuration → General settings → SCM Basic Auth Publishing Credentials → On)

---

## Known Issues / In-Progress Work

### ResumeFunctions — functions not loading (being fixed)
- **Symptom:** Azure portal shows no functions listed; log stream shows `0 functions found (Custom)` on startup.
- **Root cause:** The CI workflow used `dotnet publish --no-build` after a separate `dotnet build`. The Azure Functions Worker SDK generates `functions.metadata` during MSBuild publish targets — the `--no-build` flag skipped that step, so no metadata was deployed and the runtime found nothing to load.
- **Fix applied:** `deploy-functions.yml` updated at commit `18403f6` to use `dotnet publish --configuration Release --output ./publish` (single step, no `--no-build`). Deploy pending verification.

### ResumeFunctions — CORS middleware not wired up
- `ResumeFunctions/Program.cs` calls `builder.Services.AddCors(...)` but never calls `app.UseCors("AllowOrigin")` after `builder.Build()`. CORS policy is registered but not applied. This won't cause failures while both SWA clients are on the same Azure origin setup, but should be fixed if cross-origin errors appear. Fix: add `var app = builder.Build(); app.UseCors("AllowOrigin"); app.Run();`

---

## Conventions

- **Blazor:** Razor components in `Pages/` (not using the standard `Components/` structure); no code-behind files; styles scoped inline or in `app.css`
- **Angular:** Standalone components only — no `NgModule`; `inject()` pattern preferred over constructor injection; new `@if`/`@for` control flow (not `*ngIf`/`*ngFor`); signal-based `input()` where feasible
- **.NET version:** .NET 9 across all C# projects
- **Serialization:** System.Text.Json for Blazor; Newtonsoft.Json in ResumeFunctions (Azure SDK compat)
- **No unit tests** currently in C# projects; Angular has Karma/Jasmine spec files (mostly scaffolded stubs)
- **Azure Functions deploy:** Always use `dotnet publish` in a single step — never split `dotnet build` + `dotnet publish --no-build`, as this prevents `functions.metadata` from being generated
