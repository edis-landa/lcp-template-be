# LCP Template — Backend

ASP.NET Core Web API on `net10.0`. Solution: `LCP.Template.BE.slnx` (open this one, at the repo root). Single project: `LCP.Template.BE/`.

## Commands

```bash
dotnet build LCP.Template.BE.slnx
dotnet format LCP.Template.BE.slnx                       # apply .editorconfig
dotnet format LCP.Template.BE.slnx --verify-no-changes   # check only
dotnet run --project LCP.Template.BE --launch-profile http   # http://localhost:5254
```

## Coding standards

The full standards are the source of truth and are imported below. The `.editorconfig` at the repo root enforces formatting, naming, and style; run `dotnet format` after editing C# files.

Rules that are easiest to miss:

- Layering is strictly Controller → Engine → Service. Controllers call one engine method and hold no business logic or `try/catch`. Every engine and service has an interface in `Interfaces/`.
- All I/O is async, method names end in `Async`, and `CancellationToken cancellationToken` is the last parameter and is passed down. Never `.Result` / `.Wait()`.
- Folders are flat per layer: `Controllers/`, `Engines/`, `Services/`, etc. No feature/area subfolders unless explicitly requested.
- File-scoped namespaces matching the folder path; Allman braces; braces on every `if`/loop; primary constructors for DI.
- Logging uses `[LoggerMessage]` source generation in `partial` classes, with PascalCase placeholders. Never log secrets, tokens, or PII.
- Errors are typed exceptions handled by a global exception handler (RFC 7807 `ProblemDetails`). No empty `catch`; rethrow with `throw;`.
- DTOs are `record` types with `init` properties; request DTOs get a FluentValidation validator invoked in the engine.
- Every controller has `[Authorize]`, `[ProducesResponseType]` for each status, and XML docs. Data access is scoped to the caller's tenant.
- Secrets never go in `appsettings*.json` — User Secrets locally, Key Vault when deployed.
- No new compiler warnings.

## Current state of this repo

- The standards were adopted from the LCP Backoffice Backend. Parts that describe that system (Salesforce, SharePoint, Admin Backend, `lcp-common`, its known deviations) do not exist here yet; the general rules still apply. See the status note at the top of the standards doc.
- The WeatherForecast sample is the reference for structure: `Controllers/` → `Engines/` (interface in `Interfaces/Engines/`), domain model in `Models/`, response DTO in `ModelsDto/Responses/`, DI in `Extensions/ApplicationServiceCollectionExtensions.cs`. Its `[AllowAnonymous]` and manual mapping are known deviations (standards §20).
- A PostToolUse hook runs `dotnet format` on every edited `.cs` file, which removes unused `using` directives. Add a `using` in the same edit as the code that needs it, or the hook deletes it before the next edit.
- Entra ID (Microsoft Identity Web) authentication is registered only when an `AzureAd` config section exists (`Program.cs`). Without it, `[Authorize]` endpoints fail because no scheme is registered.

@LCP.Template.BE/docs/CODING_STANDARDS.md
