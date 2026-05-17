# 406JEM Web Apps

A personal portfolio and digital resume showcase with two frontend clients and a shared backend API.

**Live sites:**
- Blazor client — https://406jem.com
- Angular client — https://angular.406jem.com
- API — https://406resumeapi.azurewebsites.net/resumes/myresume

---

## Projects

### BlazorClient
Blazor WebAssembly SPA on .NET 10. Uses Blazorise (Bootstrap5 + FontAwesome) for UI components. Deployed to Azure Static Web Apps.

### AngularClient
Angular 19 SPA with standalone components, Angular Material, and Bootstrap 5. Built with the Angular CLI using signal-based inputs and the `@if`/`@for` control flow syntax. Deployed to Azure Static Web Apps.

### ResumeFunctions
Azure Functions v4 isolated worker on .NET 10. The sole backend — serves the resume data from a static JSON file at `GET /resumes/myresume`. Deployed to Azure Functions (Consumption plan).

---

## Architecture

```
StaticData/JustinMann_062024.json
            ↓
   ResumeFunctions (Azure Functions)
   GET /resumes/myresume
            ↓
  BlazorClient   |   AngularClient
  (Azure SWA)    |   (Azure SWA)
```

---

## CI/CD

Workflows in `.github/workflows/` are path-isolated — each only triggers when its own source files change:

| Workflow | Trigger path | Target |
|----------|-------------|--------|
| `deploy-angular.yml` | `AngularClient/**` | Azure SWA |
| `deploy-blazor.yml` | `BlazorClient/**` | Azure SWA |
| `deploy-functions.yml` | `ResumeFunctions/**` | Azure Functions |
| `claude-review.yml` | PR opened/updated | Auto code review |
| `claude-maintain-md.yml` | PR opened/updated | Keeps CLAUDE.md current |
| `claude-code.yml` | `@claude` in issues/PRs | Interactive Claude assistance |
| `pipeline-stage1-5-*.yml` | Issue labels | Multi-agent feature pipeline |

---

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Frontend (Blazor) | .NET 10, Blazor WASM, Blazorise |
| Frontend (Angular) | Angular 19, TypeScript 5.6, Angular Material |
| Backend | Azure Functions v4, .NET 10 |
| Hosting | Azure Static Web Apps (×2), Azure Functions Consumption |
| CI/CD | GitHub Actions, Claude Code Actions |
