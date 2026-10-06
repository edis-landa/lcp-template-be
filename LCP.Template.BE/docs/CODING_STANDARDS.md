# Coding Standards — LCP Backoffice Backend

These standards describe how code is written in this repository. They are derived from the existing codebase, `.editorconfig`, and project settings. When in doubt, match the surrounding code.

> **Keywords**: **MUST** = required for PR approval. **SHOULD** = expected unless there is a documented reason. **MAY** = optional.
>
> **Status in this repository**: these standards were adopted from the LCP Backoffice Backend. The general rules (formatting, naming, layering, async, logging, error handling, testing, PR process) apply here as written. Parts that describe that system's specific code do not exist in this repo yet. Treat them as the target pattern, not as a description of current code:
>
> - Salesforce, SharePoint, Admin Backend, Key Vault integration, and the `lcp-common` submodule.
> - Named reference classes (`ServiceCaseController`, `ServiceCaseEngine`, `GlobalExceptionHandler`, `CorrelationIdMiddleware`, `ApplicationServiceCollectionExtensions`, `CustomWebApplicationFactory`, and similar).
> - The test project and `.markdownlint.json`.
> - [§20 Known Deviations to Fix](#20-known-deviations-to-fix), which lists issues in the Backoffice Backend code, not in this repo.

---

## Table of Contents

1. [Tooling and Formatting](#1-tooling-and-formatting)
2. [Architecture and Layering](#2-architecture-and-layering)
3. [Project Structure and Namespaces](#3-project-structure-and-namespaces)
4. [Naming Conventions](#4-naming-conventions)
5. [C# Language Usage](#5-c-language-usage)
6. [Controllers](#6-controllers)
7. [Engines](#7-engines)
8. [Services and External Integrations](#8-services-and-external-integrations)
9. [Models and DTOs](#9-models-and-dtos)
10. [Validation](#10-validation)
11. [Mapping](#11-mapping)
12. [Error Handling](#12-error-handling)
13. [Logging and Telemetry](#13-logging-and-telemetry)
14. [Authorization and Security](#14-authorization-and-security)
15. [Configuration and Secrets](#15-configuration-and-secrets)
16. [Dependency Injection](#16-dependency-injection)
17. [Testing](#17-testing)
18. [Documentation](#18-documentation)
19. [Git, Branching, and Pull Requests](#19-git-branching-and-pull-requests)
20. [Known Deviations to Fix](#20-known-deviations-to-fix)
21. [PR Checklist](#21-pr-checklist)

---

## 1. Tooling and Formatting

- **Target framework**: `net10.0`, with `Nullable` and `ImplicitUsings` enabled in every project. These **MUST NOT** be disabled.
- **`.editorconfig` is the source of truth** for formatting. Run `dotnet format LCP.Template.BE.slnx` before pushing.
- **Indentation**: 4 spaces for `.cs`; 2 spaces for `.csproj`, `.json`, `.yml`/`.yaml`, and XML config files.
- **Encoding**: UTF-8, final newline required, no trailing whitespace (except Markdown).
- **Braces**: Allman style — every opening brace on a new line (`csharp_new_line_before_open_brace = all`).
- **Usings**: `System.*` directives first, then the rest alphabetically. Remove unused usings.
- **Markdown**: must pass `markdownlint` with the repo's `.markdownlint.json` (line length MD013 is disabled).
- The build **MUST** produce no new compiler warnings. XML doc warning `1591` is suppressed project-wide. Do not add any other `NoWarn` entries without discussing them in the PR.

## 2. Architecture and Layering

Requests flow strictly in one direction:

```text
Controller  →  Engine  →  Service  →  External system (Salesforce / SharePoint / Admin Backend)
   (HTTP)      (business logic)   (integration / I/O)
```

| Layer | Responsibility | MUST NOT |
|-------|----------------|----------|
| **Controller** (`Controllers`) | Bind route/body, call one engine method, shape the HTTP result, log entry and exit | Contain business logic, call services directly, catch exceptions for error responses |
| **Engine** (`Engines`) | Validate requests, orchestrate services, enforce scoping (account/asset), map DTOs ↔ responses | Build HTTP requests, know about Salesforce wire formats |
| **Service** (`Services`) | Talk to one external system: HTTP, auth tokens, caching, query building, deserialization | Contain business rules or reference request/response DTOs from the API surface |

- Every engine and service **MUST** have an interface in `Interfaces/Engines` or `Interfaces/Services`. Consumers depend on the interface.
- Cross-cutting concerns (exceptions, middleware, validators, mappings, telemetry) live in `Common/`.
- Shared code reused across LCP services belongs in the `lcp-common` submodule, not copied into this repo.

## 3. Project Structure and Namespaces

- Use **file-scoped namespaces** (`namespace X.Y;`). Block-scoped namespaces (as in `Types/Enums.cs`) are legacy and should be converted when touched.
- **Folders are per layer, flat by default**: controllers go directly in `Controllers/`, engines in `Engines/`, services in `Services/`, and so on. Do **not** create feature or area subfolders (for example `Controllers/Weather/`) unless explicitly requested. Subfolders that classify by kind, such as `Interfaces/Engines` or `ModelsDto/Responses`, are part of the standard layout.
- The namespace **MUST** match the folder path: `LCP.Template.BE.<Folder>` (plus `.<SubFolder>` for the standard subfolders above).
- **One public type per file**, and the file name matches the type name. Small closely related types (for example `Result<T>` and `Error`) **MAY** share a file.

## 4. Naming Conventions

| Element | Convention | Example |
|---------|-----------|---------|
| Classes, records, enums, methods, properties | PascalCase | `ServiceCaseEngine`, `GetCaseByIdAsync` |
| Interfaces | `I` + PascalCase | `IServiceCaseEngine` |
| Private instance fields | `_camelCase` | `_settings` |
| Private static readonly / const | PascalCase | `JsonOptions`, `ValidStatuses` |
| Parameters, locals | camelCase | `deviceId`, `caseDto` |
| Async methods | **MUST** end in `Async` | `CreateCaseAsync` |
| Controllers | `<Resource>Controller` | `ServiceCaseCommentsController` |
| Engines | `<Resource>Engine` | `ServiceCaseCommentsEngine` |
| Request DTOs | `<Verb><Resource>Request` | `UpdateServiceCaseRequest` |
| Response DTOs | `<Resource>Response` | `ServiceCaseResponse` |
| Internal / integration DTOs | `<Resource>Dto` | `ServiceCaseDto`, `DeviceDto` |
| Validators | `<RequestType>Validator` | `CreateServiceCaseRequestValidator` |
| Settings classes | `<System>Settings` + `const string SectionName` | `SalesforceSettings` |
| Authorization | `<Name>Requirement` / `<Name>Handler` | `DeviceAuthorizationRequirement` |
| Logger methods | `Log<Event>` | `LogCaseCreated` |
| Unit test classes | `<ClassUnderTest>UnitTest` | `ServiceCaseEngineUnitTest` |
| Integration test classes | `<Resource>IntegrationTests` | `ServiceCaseIntegrationTests` |
| Test methods | `Method_Scenario_ExpectedResult` | `ProcessCreateCaseAsync_ValidationFailure_ThrowsValidationException` |

- Use full, descriptive names. Common domain abbreviations are fine (`Dto`, `Id`, `Sf` in locals); new abbreviations are not.
- Booleans read as predicates: `IsClosed`, `HasDevice`, `UseFakeHttpExternalServices`.

## 5. C# Language Usage

- **Primary constructors** for DI in controllers, engines, services, handlers, and middleware:

  ```csharp
  public partial class SharePointService(
      HttpClient httpClient,
      IOptions<SharePointSettings> sharePointSettings,
      ILogger<SharePointService> logger) : ISharePointService
  ```

- **`var`**: use it when the type is apparent from the right-hand side. Use explicit types for built-in types and when the type is not obvious.
- Use language keywords (`string`, `int`) instead of BCL names (`String`, `Int32`).
- Do not qualify members with `this.`.
- **Nullable reference types**: treat every warning as a bug. Use `?` for optional values, `required` for mandatory init properties, and `?? string.Empty` / `?? []` for safe defaults. Avoid the `!` null-forgiving operator except in test setup or DI workarounds, and add a comment when you use it.
- Prefer modern syntax: collection expressions (`[]`, `["a", "b"]`), pattern matching (`is not HttpContext httpContext`), switch expressions, `with` expressions on records, and `using var`.
- Use guard clauses with framework helpers at public service boundaries: `ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);`.
- Expression-bodied members only when they fit on one line.
- Always use braces for `if`/`else`/loops. Single-line `if (...) return;` without braces exists in some handlers, but new code **SHOULD** use braces.
- Add parentheses for clarity in mixed arithmetic, relational, and logical expressions.
- Use `StringComparer.OrdinalIgnoreCase` / `StringComparison.Ordinal*` for identifier comparisons. Never rely on culture-sensitive comparisons.

### Async

- All I/O **MUST** be async from top to bottom. Never use `.Result`, `.Wait()`, or `GetAwaiter().GetResult()`.
- Every async method that does I/O **MUST** accept a `CancellationToken cancellationToken` as its **last** parameter and pass it down:
  - Interfaces and engine/service methods: `CancellationToken cancellationToken = default`.
  - Controller actions: `CancellationToken cancellationToken` (no default; ASP.NET binds it).
  - Authorization handlers: use `httpContext.RequestAborted`.
- Return `Task` / `Task<T>`. Use `ValueTask` only when an interface you implement requires it (for example `IExceptionHandler`).
- Never write `async void`.

## 6. Controllers

In the LCP Backoffice Backend, `ServiceCaseController` is the reference implementation. In this repo, use `Controllers/WeatherForecastController.cs` (with its engine `Engines/WeatherForecastEngine.cs`) as the reference for structure, except for the deviations listed in [§20](#20-known-deviations-to-fix).

- Required attributes on the class: `[ApiController]`, `[Authorize(Policy = ...)]`, `[Produces("application/json")]`, `[Route("service/api/v1/...")]`.
- Inherit from `ControllerBase`. Declare the class as `partial` so it can use source-generated logging.
- **Routing**: everything sits under `service/api/v1/`. Use lowercase, plural, kebab-case resource segments (`data-types`, `cases`, `comments`). Name route parameters in camelCase (`{orgId}`, `{deviceId}`, `{caseId}`).
- **Binding**: be explicit with `[FromRoute]`, `[FromBody]`, and `[FromQuery]`.
- **Action names** end in `Async` (`GetCaseAsync`).
- **HTTP semantics**:

| Operation | Verb | Success response |
|-----------|------|------------------|
| Create | `POST` | `201 Created` via `CreatedAtAction(...)` |
| Partial update | `PATCH` | `204 No Content` |
| Read single / list | `GET` | `200 OK` |
| File download | `GET .../download` | `File(bytes, contentType, fileName)` |

- Every action **MUST** declare `[ProducesResponseType]` for its success type and for each error status it can return, using `typeof(ProblemDetails)` for errors.
- Every action **MUST** have XML docs: `<summary>`, `<param>` for each parameter, `<returns>`, and `<response code="...">` for each status.
- Action body pattern: log the start, call **one** engine method, log the result, return.
- Do not use `try/catch` in controllers. Exceptions reach `GlobalExceptionHandler`. Inline `BadRequest(new ProblemDetails { ... })` is acceptable only for route-format checks such as GUID parsing.

## 7. Engines

- Engines own business rules. Responsibilities, in order:
  1. Validate the request with the injected `IValidator<T>` and throw `Common.Exceptions.ValidationException` with errors grouped by property.
  2. Resolve scoping context (for example, look up the Salesforce Asset by `deviceId.ToUpper()` to get `AccountId`/`AssetId`).
  3. Call services, **always scoped** to the caller's account/asset so data never leaks across tenants.
  4. Map results to response DTOs with AutoMapper.
  5. Log success with the correlation ID (`httpContextAccessor.HttpContext?.TraceIdentifier`).
- When an engine catches an exception, it does so **only to log context**, and then it **MUST** rethrow with `throw;` (never `throw ex;`). Use exception filters to skip expected types: `catch (Exception ex) when (ex is not Common.Exceptions.ValidationException)`.
- Engines return response DTOs (`ServiceCaseResponse`), never raw integration DTOs.
- An empty result is not an error. Return `Enumerable.Empty<T>()` / `[]` and log a warning if the situation is unusual.
- Extract repeated logic, such as validation-to-exception conversion or asset lookup, into a private helper or shared extension instead of duplicating it.

## 8. Services and External Integrations

- One service class per external system. Each service implements its interface and is registered as a **typed `HttpClient`** in `ApplicationServiceCollectionExtensions`.
- Every `HttpClient` registration **MUST** include:
  - an explicit `Timeout` (from settings where available),
  - `.AddPolicyHandler(GetRetryPolicy(...))` (exponential backoff with jitter that honors `Retry-After`),
  - `.AddPolicyHandler(GetCircuitBreakerPolicy())`.
- Keep endpoint templates, SOQL templates, cache keys, and magic IDs in a `private static class Constants` inside the service, or in `Constants/` if they are shared.
- **Query building**: never insert untrusted input into SOQL/OData/URLs without escaping it. Use `Uri.EscapeDataString` for URL parts and a SOQL escaping helper for string literals (see [§20](#20-known-deviations-to-fix)).
- Dispose `HttpResponseMessage` with `using var response = ...`.
- Translate external failures into typed exceptions (`SalesforceException` with status code and error code, or `NotFoundException` for missing records). Do not return `null` to mean "not found" across a layer boundary.
- **Caching**: use `IMemoryCache` with explicit expirations taken from settings. Prefix cache keys by entity (`asset_`, `salesforce_access_token`). Cache only data that is safe to share across users, or include the user/org in the key.
- Reuse a `static readonly JsonSerializerOptions` instance. Do not allocate one per call.

## 9. Models and DTOs

- **DTOs are `record` types** with `{ get; init; }` properties. Use `required` for mandatory fields and nullable types for optional ones.
- **Domain models** (`Models/`) are `class` types with `{ get; set; }`.
- Use `[JsonPropertyName("...")]` when the wire name differs from the C# name (for example Salesforce custom fields such as `Tier__c`). Keep C# property names clean (`Tier`, not `Tier__c`).
- Request DTOs document allowed values in XML `<summary>`. Keep those docs in sync with the validator's allowed-values arrays.
- Never return domain models or integration DTOs (`ModelsDto/Salesforce`) from controllers. Always map them to `ModelsDto/Responses`.
- Enums live in `Types/`. When an enum maps to an external numeric code, assign the value explicitly.

## 10. Validation

- Every request body DTO **MUST** have a FluentValidation validator in `Common/Validators/`. Validators are registered automatically by `AddValidatorsFromAssemblyContaining<...>()`.
- Validators are invoked **explicitly in the engine**, not through automatic MVC validation.
- Every rule **MUST** have a human-readable `.WithMessage(...)`, ending with a period.
- Optional fields are validated with `.When(x => !string.IsNullOrEmpty(x.Field))`.
- Keep allowed-value lists as `private static readonly string[]` in the validator. Pair each with a `private static bool BeAValid<Field>(string?)` predicate.
- Mirror Salesforce field limits (for example `Subject` ≤ 255, `Description` ≤ 32000, `CommentBody` ≤ 4000).
- Service-level `ArgumentException` guards are defense in depth. They do not replace validators.

## 11. Mapping

- All AutoMapper maps live in profiles under `Common/Mappings/`. Group maps with comments by direction (`// Request to DTO`, `// DTO to Response`, ...).
- Map nullable source members to non-nullable response members explicitly (`src.Id ?? string.Empty`).
- `Ignore()` destination members that must not be overwritten, such as IDs and audit fields.
- Every new map **MUST** be covered by `SalesforceMappingProfileUnitTest` or an equivalent test. The configuration must stay valid.
- Do not put business logic inside `MapFrom` lambdas beyond null-coalescing and simple navigation.

## 12. Error Handling

- Signal errors by throwing **typed exceptions** from `Common/Exceptions/`. `GlobalExceptionHandler` turns them into RFC 7807 `ProblemDetails` with a `correlationId`:

| Exception | HTTP status |
|-----------|-------------|
| `ValidationException` | 400, with an `errors` dictionary |
| `ForbiddenException` | 403 |
| `NotFoundException` | 404 |
| `SalesforceException` | its `StatusCode`, or 502 |
| anything else | 500 with a generic message |

- Name the domain `ValidationException` in full (`Common.Exceptions.ValidationException`) wherever `FluentValidation` is also imported, to avoid ambiguity.
- To add a new exception type, put it in `Common/Exceptions/` **and** add a case to `GlobalExceptionHandler`, plus a unit test.
- Never expose stack traces, internal URLs, tokens, or raw external payloads in `Detail`.
- Do not swallow exceptions. An empty `catch` is not allowed. A `catch` that continues **MUST** log at `Warning` or above. For example, skipping a device whose asset is missing is acceptable when logged.
- `Result<T>` / `Error` in `Common/Models` exists for flows where failure is expected and is not an exception. Exceptions remain the default pattern.

## 13. Logging and Telemetry

- **Use source-generated logging only**: declare the class `partial` and define `[LoggerMessage]` methods at the **bottom** of the class:

  ```csharp
  [LoggerMessage(LogLevel.Information, "Case updated successfully. CaseId: {CaseId}, CorrelationId: {CorrelationId}")]
  partial void LogCaseUpdatedSuccessfully(string caseId, string correlationId);
  ```

- Do not call `logger.LogInformation(...)` directly in application code (analyzer CA1848). The only existing exception is the Polly callback in DI setup.
- Message templates use **PascalCase placeholders** (`{CaseId}`, `{DeviceId}`). Never use string interpolation inside a template.
- Log levels:
  - `Information`: start and success of operations.
  - `Warning`: expected but notable conditions (not found during fan-out, authorization denied, retries).
  - `Error`: failed operations, just before the rethrow.
- **Never log** secrets, tokens, client secrets, full request bodies, attachment content, or personal contact data (email/phone). Identifiers (case ID, device serial, org ID) are fine.
- Correlation: `CorrelationIdMiddleware` sets `X-Correlation-ID` and a logging scope. Include `CorrelationId` in success/failure logs of engine operations.
- Custom metrics go through `ApplicationInsightsMetrics`, which tolerates a null `TelemetryClient`. Do not inject `TelemetryClient` directly.

## 14. Authorization and Security

- Every controller **MUST** have a class-level `[Authorize(Policy = ...)]`. Device-scoped actions add `[Authorize(Policy = "DeviceAuthorizationPolicy")]`.
- `[AllowAnonymous]` is allowed only on health and ping endpoints.
- Policies are defined in `AddAuthorizationPolicies`. Each policy is a `Requirement` + `Handler` pair under `Authorization/`. Operation keys live in `Constants/OperationKeys.cs`.
- Authorization handlers **fail closed**: on any missing or invalid input, log and `return` without calling `context.Succeed`.
- Authorization alone is not enough. Data access **MUST** also be scoped to the caller's account/asset (see [§7](#7-engines)), so a user can never read another tenant's case by guessing its ID.
- Validate and escape all external identifiers before using them in queries or paths. Reject path traversal in file paths (`..`).
- Keep the security middleware (`SecurityHeadersMiddleware`, rate limiting, HTTPS, CORS allow-list) in place. Do not loosen CORS to `AllowAnyOrigin` outside tests.
- Enforce the 25 MB attachment upload limit at validation time.

## 15. Configuration and Secrets

- Each config section has a strongly typed settings class in `Configuration/` with:
  - `public const string SectionName = "...";`
  - DataAnnotations (`[Required(ErrorMessage = "...")]`, `[Range]`) on mandatory values
  - safe defaults (`= string.Empty`)
- Register with `AddOptions<T>().Bind(...).ValidateDataAnnotations().ValidateOnStart()` so that bad configuration fails at startup.
- Consume settings through `IOptions<T>`. Read `.Value` once into a `private readonly` field.
- **Secrets MUST NOT be committed.** Locally they come from User Secrets. In deployed environments they come from Azure Key Vault. Inject secrets in `PostConfigure` from their Key Vault names.
- `http-client.env.json` and other local credential files stay in `.gitignore`.
- Environment-specific values go in `appsettings.{Environment}.json` (Development, QA, Staging, Production).

## 16. Dependency Injection

- Register everything in `ApplicationServiceCollectionExtensions.AddApplicationServices` (or in a private `Add<Feature>` extension that it calls). Keep `Program.cs` minimal.
- Lifetimes:
  - **Scoped**: engines, services, `ApplicationInsightsMetrics`
  - **Singleton**: stateless helpers (parsers), authorization handlers (inject only singleton-safe dependencies)
  - **Typed HttpClient**: external services (see [§8](#8-services-and-external-integrations))
- Never resolve services manually with `IServiceProvider` in application code, except inside DI factory lambdas.

## 17. Testing

Frameworks: **xUnit**, **Moq**, **FluentAssertions**, `FluentValidation.TestHelper`, and `Microsoft.AspNetCore.Mvc.Testing`.

### Layout

```text
LCP.Template.BE.Tests/
├── UnitTests/<Layer>/<ClassUnderTest>UnitTest.cs
├── IntegrationTests/Controllers/<Resource>IntegrationTests.cs
├── IntegrationTests/Infrastructure/CustomWebApplicationFactory.cs
└── Helpers/              shared fixtures, fake HTTP handlers, stub defaults
```

### Rules

- New or changed logic **MUST** include unit tests. New endpoints **MUST** include integration tests that cover the success path, validation (400), and authorization (403).
- Test method names follow `Method_Scenario_ExpectedResult`.
- Structure each test as **Arrange / Act / Assert**, with the sections separated by blank lines. `// Arrange` comments are optional.
- Use FluentAssertions (`result.Should().Be(...)`, `await act.Should().ThrowAsync<T>()`). Do not use `Assert.*`.
- Create mocks as fields (`private readonly Mock<IService> _mockService = new();`) and build the system under test in the constructor.
- Verify important interactions with `Verify(..., Times.Once)` / `Times.Never`, especially "no external call after validation failure".
- Use `[Theory]` + `[InlineData]` for input variations (null, empty, whitespace, boundary lengths).
- Validators are tested with `TestValidateAsync` and `ShouldHaveValidationErrorFor(...).WithErrorMessage(...)`.
- Integration tests use `CustomWebApplicationFactory`:
  - default mode mocks `IService` / `IAdminBackendService` / `ISharePointService` boundaries,
  - `UseFakeHttpExternalServices = true` runs the real services against fake HTTP handlers.
- Tests **MUST NOT** call real Salesforce, SharePoint, Admin Backend, or Key Vault.
- Put shared test data in `Helpers/` fixtures (`SalesforceServiceMockFixture.DefaultAsset`, etc.) instead of duplicating it.
- Run `dotnet test LCP.Template.BE.slnx` locally before opening a PR. CI runs the same command.

## 18. Documentation

- Public controller actions and request/response DTOs **MUST** have XML docs. They feed Swagger (`GenerateDocumentationFile` is on).
- Engines and services **SHOULD** document non-obvious behavior on the interface.
- Comments explain **why**, not what. Remove commented-out code and stale comments (for example "save to database" where no database write happens).
- Update `README.md` (API reference, configuration table) whenever you add an endpoint, a config section, or an integration. Update `.pipelines/README.md` for pipeline changes and `docs/Salesforce.md` for Salesforce schema or query changes.

## 19. Git, Branching, and Pull Requests

- Branch from `dev` using `feature/<work-item-id>-<short-description>` (for example `feature/feature-150628-testing_implementation`). `dev` is merged into `main` for releases.
- Commit messages are in the imperative mood and describe the change ("Add SharePoint document download endpoint").
- PRs to `dev`/`main` **MUST**:
  - build cleanly and pass all tests in CI,
  - link the Azure DevOps work item,
  - include a description of what changed and how it was tested,
  - get at least one reviewer approval.
- When you change the `lcp-common` submodule, bump the pointer in a dedicated commit with a message such as "Update common".
- Do not commit `bin/`, `obj/`, `.vs/`, user secrets, or local env files.

## 20. Known Deviations to Fix

The current code does not yet meet the standards above in the following places. Fix them when you touch the code. Do not copy these patterns.

### This repository

| Area | Issue | Standard |
|------|-------|----------|
| `WeatherForecastController` | Class-level `[AllowAnonymous]` because Entra ID auth is not configured yet (no `AzureAd` section) | Replace with `[Authorize(Policy = ...)]` once auth is configured ([§14](#14-authorization-and-security)) |
| `WeatherForecastEngine` | Maps the domain model to the response DTO manually instead of with AutoMapper, pending a decision on AutoMapper licensing | Move the map to a profile in `Common/Mappings/` if AutoMapper is adopted ([§11](#11-mapping)) |

### LCP Backoffice Backend (reference only)

> **Not applicable to this repo.** This table lists deviations in the LCP Backoffice Backend code. It is kept as a list of patterns to avoid.

| Area | Issue | Standard |
|------|-------|----------|
| `SalesforceService` SOQL templates | Values are inserted with `string.Format` and are not escaped (`'` / `\`) | Add a SOQL literal escaping helper and apply it to every interpolated value |
| Policy names | `"DeviceAuthorizationPolicy"` and similar are string literals repeated across controllers and DI | Move them to a `Constants/AuthorizationPolicies` class |
| Engine validation | The validation-to-exception block is duplicated in every engine method | Extract it into a shared extension, for example `validator.ValidateAndThrowDomainAsync(...)` |
| `ServiceCaseEngine.ProcessCreateCaseAsync` | Maps to the `ServiceCase` entity and discards it ("save to database" comment) | Remove the dead mapping, or implement persistence |
| `ServiceCaseEngine.GetAllCasesAsync` | `accountId` is taken from the last device in the loop | Make account resolution explicit, or group by account |
| `Types/Enums.cs` | Block-scoped namespace | Use a file-scoped namespace |
| Some handlers | Single-line `if (...) return;` without braces | Use braces |

## 21. PR Checklist

- [ ] Follows Controller → Engine → Service layering, with interfaces for new engines/services
- [ ] All async I/O passes `CancellationToken`, and methods end in `Async`
- [ ] Request DTO has a FluentValidation validator, invoked in the engine
- [ ] Errors use typed exceptions, no swallowed exceptions, rethrow with `throw;`
- [ ] Logging uses `[LoggerMessage]` source generation, with no secrets or PII in logs
- [ ] Endpoint has `[Authorize]`, `[ProducesResponseType]` for every status, and XML docs
- [ ] Data access is scoped to the caller's account/asset, and external input is escaped
- [ ] New settings have a `SectionName`, validation, and `ValidateOnStart`, with secrets only in Key Vault/User Secrets
- [ ] Unit tests added/updated, and integration tests for new endpoints
- [ ] `dotnet format` and `dotnet test` pass locally
- [ ] README / docs updated where relevant
