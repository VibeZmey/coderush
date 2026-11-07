# CodeRush backend

The backend has four projects, one for each layer. Only `Api` is an ASP.NET Core
application; the other projects are class libraries.

```text
backend/
  Api/
    Api.csproj
    Contracts/Auth/
    Endpoints/
    Properties/
    Program.cs
    appsettings.json
    Dockerfile
  Application/
    Application.csproj
    Common/
      Dto/
      Interfaces/
    Features/Accounts/
      GetAccount/
      Login/
      Logout/
      Register/
    DependencyInjection.cs
  Domain/
    Domain.csproj
    Models/
  Infrastructure/
    Infrastructure.csproj
    Identity/
    Persistence/
      Migrations/
    DependencyInjection.cs
  backend.sln
```

`Api` contains Minimal API endpoints and HTTP contracts. `Application/Features`
groups commands, queries, handlers, and results by use case. Shared application
DTOs and interfaces live in `Application/Common`.

`Domain/Models` contains versioned problems, single-player training matches,
per-task timers and outcomes, drafts, submissions, and execution jobs. See
`Domain/Models/README.md` for relationships and future persistence constraints.
Matches, the problem catalog, assigned match problems, code runs and execution
jobs are registered in `AppDbContext`. Submission checking and drafts are not mapped yet.
ASP.NET Core Identity users and persistence belong to `Infrastructure`.
Application code uses interfaces instead of infrastructure implementations.
`Api/Program.cs` composes the application using `AddApplication` and
`AddInfrastructure`.

Project references point inward: `Application` references `Domain`,
`Infrastructure` references `Application` (and receives `Domain` transitively),
and `Api` references `Application` and `Infrastructure` for composition.

Build from this directory with `dotnet build backend.sln` and run with
`dotnet run --project Api/Api.csproj`. Configuration and launch profiles live
in `Api` and `Api/Properties`, respectively.

EF Core migrations remain in `Infrastructure/Persistence/Migrations`, in the
Infrastructure assembly. Existing migration IDs and database mappings
are preserved. Future migrations can be added from this directory with
`dotnet ef migrations add <Name> --project Infrastructure/Infrastructure.csproj --startup-project Api/Api.csproj --output-dir Persistence/Migrations`.

Docker Compose uses this directory as its build context and `Api/Dockerfile`.

## Creating a match

`POST /api/matches/training` requires the existing authentication cookie. Example body:

```json
{
  "count": 10,
  "difficulty": "Easy",
  "timePerProblemSeconds": 300
}
```

The endpoint calls `IHandler<CreateTrainingMatchCommand, CreateTrainingMatchResult>`
directly. Each mode has its own route, request, command and handler.
Training settings are validated and saved in `Matches` with status `Created`.
The authenticated user's ID is assigned by the server. Defaults: 2 tasks, Easy,
300 seconds per task. Accepted limits: 1–100 tasks, Easy/Medium/Hard difficulty,
1–86400 seconds per task. Invalid settings return 400;
an unauthenticated request returns 401. Unknown settings are rejected.

Successful requests return 201:

```json
{
  "id": "<match UUID>",
  "status": "Created",
  "createdAt": "<UTC timestamp>"
}
```

Creation currently saves only the session and its requested settings: it does
not assign tasks, start timers, or invoke Judge0. Tasks are assigned by the start action.
The `AddTrainingMatches` migration creates the table, discriminator, owner foreign
key, history index and training setting checks. Existing startup code applies
migrations; alternatively run `dotnet ef database update --project Infrastructure/Infrastructure.csproj --startup-project Api/Api.csproj`
with the application's PostgreSQL connection configured.

## Starting a match

`POST /api/matches/start` requires the authentication cookie and a JSON body:

```json
{
  "matchId": "<match UUID>"
}
```

`StartMatchHandler` checks ownership and the `Created` state, calls `Match.Start()`
and saves the change. A successful response is 200 with `id`, `status` (`InProgress`)
and `startedAt` in UTC. A missing `matchId` field returns 400; a missing match or another user's
match returns 404; a non-created match returns 409. No additional locking or
concurrent-start protection is implemented. A repeated call does not reset the timestamp.

Starting selects exactly Count different published tasks of the chosen difficulty,
using the latest published version of each non-archived task with an enabled runtime.
Selection is random and contains no repeated task. Insufficient tasks return 409
and leave the match Created, without partial assignment. Version IDs are frozen
for the session. The first task is opened immediately with its own UTC deadline.

## Training session lifecycle

- GET /api/matches returns the signed-in user's latest 50 sessions and results.
- GET /api/matches/{id} returns editor data, current task number, total count,
  serverTime, deadline, allowed runtime templates/drafts and public samples only.
- POST /api/matches/{id}/next skips an active task or advances after an expired
  task. Send an empty JSON object. The next task receives the full configured time.
- POST /api/matches/{id}/leave ends early, preserving previous outcomes and marking
  active/unopened remaining tasks Abandoned. Send an empty JSON object.
- PUT /api/matches/{id}/draft saves matchProblemId, programmingLanguageId,
  sourceCode and standardInput for the active task and runtime. Empty code is valid
  for a draft. Completed/expired tasks cannot be changed.

All session routes require authentication and ownership; other users receive 404.
Deadlines are stored once. Read/action handlers reconcile expired tasks using the
server clock, so reloads, disconnection and application restarts do not reset time.
No per-match timer process or browser unload handler is needed. When a task expires,
the next task stays NotStarted until the player explicitly advances. The last
task's expiry finishes the session; FinishedAt records the deadline rather than
the later time when the result was read. Refreshing state does not extend a timer.

Explicit leave completes with FinishReason=Abandoned. Closing a tab or navigating
away leaves the session resumable and the current clock running. Results contain
per-task outcomes, start/close times, run counts, counts of started/unopened tasks
and time spent working. They remain in PostgreSQL for history/statistics. A full
set of skipped/timed-out tasks finishes normally with FinishReason=Finished.

AddTrainingSessionResultsAndDrafts adds FinishReason and CodeDrafts and has been
applied to the project database. Draft IDs and assigned task IDs are preallocated
GUIDs. The repository tracks changes without rewriting immutable problem versions.
Judge0 execution and solution checking are still separate features: CodeRun only
queues execution, and skipping a task never marks it Solved. Solved outcomes will
be produced by the trusted solution checker, not by the player's next/leave APIs.

To add a mode: define its request, application command, result and handler;
register the handler in DI, add a dedicated route and map its entity in EF.
There is no shared creation dispatcher or JSON type discriminator. The old
`POST /api/matches` route is no longer mapped.

Tests: `dotnet test Tests/Tests.csproj`. Contract, validation and EF SQL
checks do not require a running PostgreSQL instance. A real database round trip
requires a separate integration check.

## Requesting a code run

`POST /api/code/run` requires the authentication cookie. Example body:

```json
{
  "matchProblemId": "<assigned task UUID>",
  "programmingLanguageId": 71,
  "sourceCode": "print(input())",
  "standardInput": "hello\n"
}
```

`matchProblemId` identifies a MatchProblem, not the catalog's Problem or ProblemVersion.
The handler checks the owning user, a running match, an active and unclosed task,
its start/deadline and an enabled runtime allowed by that problem version.
Limits are copied from ProblemVersion and apply equally to every language; clients cannot set limits, identity or
expected output. Source is required (up to 65536 UTF-8 bytes); input is optional
(empty by default, up to 1048576 UTF-8 bytes). Neither can contain U+0000 because
PostgreSQL text cannot store it. Code and input are otherwise preserved exactly.

CodeRun and its ExecutionJob are saved together in one SaveChanges transaction.
The response is 202 Accepted:

```json
{
  "id": "<code run UUID>",
  "status": "Pending",
  "createdAt": "<UTC timestamp>"
}
```

Invalid input or an unavailable runtime returns 400, missing authentication 401,
a missing/foreign task 404, and an inactive/expired task or match 409.
No Judge0 request, worker or result-polling endpoint is implemented at this stage;
accepted jobs remain Pending. Do not poll the backend for results yet.

Apply the AddCodeRuns migration (the existing app startup also applies migrations).
It adds the catalog, language settings, MatchProblems, CodeRuns and ExecutionJobs
needed for access checks and storage. Starting a training match now populates and
activates MatchProblems. An active assignment and its allowed
runtime must already exist to accept a code run. The runtime catalog is seeded
by UseJudge0LanguageIds; tasks are created through the admin endpoint.

## Request validation and authorization

Required JSON fields are declared with C# `required`. DataAnnotations on the HTTP
request DTOs describe non-empty strings, ranges and text limits. One reusable
RequestValidationFilter validates each request at the API boundary, before the
endpoint invokes its handler. It returns 400 ProblemDetails with one error message;
there is no manually accumulated dictionary of field errors. A required Guid must
be present in JSON; an explicit all-zero/unknown Guid is handled as a missing resource.

Protected routes use the default authorization policy, which requires an
authenticated user and the NameIdentifier claim. Endpoints extract that claim;
handlers do not re-check login or missing fields. Commands contain non-nullable
strings and assume a validated request. Any future non-HTTP caller must validate
its own input before creating commands.

Handlers retain business rules: resource ownership, match/task state, deadlines
and allowed runtimes. Identity still enforces password policy and account uniqueness.
Registration failures return 400 with `errors` as a list of `{ code, description }`
objects instead of the previous field-error dictionary. Match creation returns its
created result directly, without a redundant validation-failure result wrapper.

## Admin role and problem creation

The `AddAdminRole` migration inserts the `Admin` Identity role. Registration never
assigns this role, and there is no public endpoint for granting it. After applying
the migration, grant it to a chosen existing account through database administration
(replace the email below):

```sql
INSERT INTO "AspNetUserRoles" ("UserId", "RoleId")
SELECT u."Id", r."Id"
FROM "AspNetUsers" AS u
CROSS JOIN "AspNetRoles" AS r
WHERE u."NormalizedEmail" = upper('your-email@example.com')
  AND r."NormalizedName" = 'ADMIN'
ON CONFLICT DO NOTHING;
```

Sign out and sign back in after assigning the role so the authentication cookie
contains the role claim. Identity's role-aware claims factory is already provided
by the existing AddIdentity<User, IdentityRole> configuration.

`POST /api/problems` requires the `Admin` policy, which includes the existing
authentication and user-ID requirements. Anonymous requests receive 401; signed-in
users without Admin receive 403. The request body contains one complete task:

```json
{
  "key": "sum-two-numbers",
  "title": "Сумма двух чисел",
  "descriptionMarkdown": "Прочитайте два целых числа и выведите **их сумму**.",
  "difficulty": "Easy",
  "publish": true,
  "cpuTimeLimitSeconds": 2,
  "wallTimeLimitSeconds": 10,
  "memoryLimitKilobytes": 128000,
  "outputLimitKilobytes": 1024,
  "languages": [
    {
      "programmingLanguageId": 71,
      "template": "a, b = map(int, input().split())\n"
    }
  ],
  "testCases": [
    { "input": "2 3\n", "expectedOutput": "5\n", "isSample": true },
    { "input": "-10 4\n", "expectedOutput": "-6\n", "isSample": false }
  ]
}
```

`key` is a unique lowercase slug, up to 128 characters. Title is limited to 256
characters. Optional Markdown fields: inputDescriptionMarkdown,
outputDescriptionMarkdown, constraintsMarkdown and explanationMarkdown.
Difficulty is Easy, Medium or Hard. At least one language and one test are required.
Test order is the array order; saved positions start at 1. Empty input, expected
output or template are valid. The API validates nested items with the same request
filter; no field validation is repeated in the handler.

Each language must reference an existing, enabled ProgrammingLanguage in CodeRush,
and cannot be repeated. Each language entry contains only programmingLanguageId
and template. Shared limits belong to ProblemVersion and are supplied at the top
level of the creation request. They default to cpuTimeLimitSeconds=2,
wallTimeLimitSeconds=10, memoryLimitKilobytes=128000 and
outputLimitKilobytes=1024. Limits must be positive; compatibility with a specific
Judge0 instance is part of the future execution integration. Language IDs are the
integer IDs from Judge0 (for example, 71 is Python 3.8.1). Language settings keep
their own Guid IDs and task-specific templates. Catalog editing is separate.

The author comes from the authenticated identity. The handler creates a Problem,
its first ProblemVersion, language settings and test cases. One SaveChanges call
saves the graph atomically. A duplicate key returns 409, unavailable/repeated
languages return 400. Successful creation returns 201 with id, versionId,
versionNumber, status and createdAt. Hidden tests and expected outputs are not
included in the response.

By default publish=false creates a Draft. Set publish=true to create an immutable
Published version immediately. Editing/publishing existing drafts and batch imports
are separate features. The role is not assigned automatically by registration.
The project database has been migrated and the explicitly requested existing
account has been granted Admin; sign in again after a role change.

`MoveExecutionLimitsToProblemVersion` moves existing language limits into the
version before removing the old columns. If they differ, the maximum of each
limit becomes the shared value. Versions without languages receive the defaults.
A rollback copies the version's shared limits to every language; original differences
between languages cannot be restored. Existing ExecutionJob limit snapshots are
unchanged. This migration has been applied to the project database.

## Judge0 language catalog

UseJudge0LanguageIds imports all 71 entries from https://ce.judge0.com/languages/
(snapshot retrieved on 2026-11-03). ProgrammingLanguage.Id is now the exact Judge0
integer language_id and is not database-generated. There is no language-mapping
entity or table. ProblemLanguage keeps its own Guid Id, ProblemVersionId and
Template; its ProgrammingLanguageId points directly to the catalog integer ID.
Execution jobs, submissions, drafts and HTTP requests use that same integer ID.

The initial enabled set includes all catalog versions of C, C++, C#, Python,
JavaScript and Go (27 entries). Other entries are present with IsEnabled=false.
Changing IsEnabled controls future task creation and run requests. No HTTP call
to Judge0 is needed at startup to populate this catalog.

The migration converts existing GUID references by catalog key, exact display
name, or display name plus runtime version. Unmatched legacy languages stop the
migration instead of deleting task history. A rollback preserves logical links
with deterministic GUIDs; restoring the original GUID values requires a backup.

The migration has been applied to the project's PostgreSQL database and the
71 catalog rows have been verified. A complete ready-to-send admin request is
available in Api/Examples/create-problem.json. POST it to /api/problems using
the Admin user's authentication cookie; it creates a published task with Python
71, C++ 54 and JavaScript 63 templates. The example is not inserted automatically.
The public catalog does not install or upgrade compilers in a self-hosted Judge0;
actual execution support must be checked against that instance when integrating it.
