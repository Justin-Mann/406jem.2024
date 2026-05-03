# 406JEM Web Apps — Codebase Map

## Solution Overview

**File:** `406JemWebApps.sln`  
A personal portfolio/resume showcase with two frontend clients (Blazor WASM + Angular SPA), a REST API, and an Azure Functions API. Both frontends render the same digital resume data from the same backend.

---

## Projects

### BlazorClient (`BlazorClient/`)
- **Type:** Blazor WebAssembly (static SPA)
- **Framework:** .NET 9 / `Microsoft.NET.Sdk.BlazorWebAssembly`
- **Root namespace:** `BlazorApp.BlazorClient`
- **Deploy:** Azure Static Web Apps (workflow: `azure-static-web-apps-thankful-island-023668f10.yml`)
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

### AngularClient (`AngularClient/`)
- **Type:** Angular SPA (standalone components, no NgModules)
- **Framework:** Angular 19 / TypeScript 5.6
- **Build:** `@angular/build:application`
- **Deploy:** Azure Static Web Apps (workflow: `azure-static-web-apps-brave-bush-0375f9910.yml`)
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
- **Services:** `app/services/data/resume-data.service.ts` — `HttpClient`-based, calls `MyResumeApi`
- **Interfaces:** `app/interfaces/resume.interface.ts` — TypeScript interfaces mirroring the C# models
- **Styles:** `src/styles.css` — global; each component has its own `.css`
- **Config:** `angular.json`, `tsconfig.json`

### MyResumeApi (`MyResumeApi/`)
- **Type:** ASP.NET Core Web API
- **Framework:** .NET 9 / `Microsoft.NET.Sdk.Web`
- **Deploy:** Azure App Service (workflow: `azure-webapps-dotnet-resumeapi-core.yml`)
- **Live URL:** https://myresumeapi20250620071718.azurewebsites.net
- **Key packages:** Swashbuckle.AspNetCore (Swagger UI)
- **Controllers:** `Controllers/ResumeController.cs` — returns `DigitalResumeModel` from a static JSON file
- **Data:** `ResumeDataFile/JustinMann_062024.json` — the resume JSON source of truth
- **Models:** `Models/DigitalResumeModel.cs` — shared model with BlazorClient
- **Swagger:** enabled in development; endpoint `GET /resumes/myresume`

### ResumeFunctions (`ResumeFunctions/`)
- **Type:** Azure Functions v4 (isolated worker)
- **Framework:** .NET 9 / `Microsoft.NET.Sdk`
- **Deploy:** Azure Functions (workflow: `azure-webapps-dotnet-core.yml`)
- **Key packages:** `Microsoft.Azure.Functions.Worker` 2.x, `Azure.Storage.Blobs`, Newtonsoft.Json
- **Data:** `StaticData/Resumes/JustinMann_062024.json`
- **Note:** Separate HTTP-trigger implementation of the same resume API; Azure blob storage integration stubbed in

### Client (`Client/`)
- **Type:** Legacy stub — only contains `Pages/Home.razor`; appears to be a leftover from initial scaffolding
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
Static JSON files  →  ResumeFunctions (Azure Fn) or MyResumeApi (ASP.NET)
                                    ↓
                   GET /resumes/myresume
                                    ↓
                  BlazorClient  |  AngularClient
                    (WASM)      |     (SPA)
```

---

## CI/CD Workflows (`.github/workflows/`)

| File | Trigger | Target |
|------|---------|--------|
| `azure-static-web-apps-thankful-island-023668f10.yml` | push/PR to `main` | Blazor → Azure SWA |
| `azure-static-web-apps-brave-bush-0375f9910.yml` | push/PR to `main` | Angular → Azure SWA |
| `azure-webapps-dotnet-resumeapi-core.yml` | push/PR to `main` | MyResumeApi → Azure App Service |
| `azure-webapps-dotnet-core.yml` | push/PR to `main` | ResumeFunctions → Azure Functions |
| `claude.yml` | PR events | Claude Code PR review/assistance |
| `stage1-5-*.yml` | Manual / PR | Multi-agent CI/CD pipeline |

---

## Conventions

- **Blazor:** Razor components in `Pages/` (not using the standard `Components/` structure); no code-behind files; styles scoped inline or in `app.css`
- **Angular:** Standalone components only — no `NgModule`; `inject()` pattern preferred over constructor injection; new `@if`/`@for` control flow (not `*ngIf`/`*ngFor`); signal-based `input()` where feasible
- **.NET version:** .NET 9 across all C# projects
- **Serialization:** System.Text.Json for Blazor and API; Newtonsoft.Json only in ResumeFunctions (Azure SDK compat)
- **No unit tests** currently in C# projects; Angular has Karma/Jasmine spec files (mostly scaffolded stubs)
