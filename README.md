# DynamicExpressionEngine

A small, secure **expression evaluation API** built on **.NET**.

It lets you post an expression plus JSON-like input data and get a computed result back. The project is designed to be safe-by-default by using:

- **API key authentication** (header: `X-API-Key`)
- An **allow-listed function catalog** (only known functions can be executed)
- **Request validation** (expression length, forbidden characters, referenced parameter checks, etc.)

---

## Projects in the solution

- `src/DynamicExpressionEngine.Api` – ASP.NET Core Web API (controllers, auth, Swagger)
- `src/DynamicExpressionEngine.Core` – shared abstractions, models, validation, function catalog/registry
- `src/DynamicExpressionEngine.NCalc` – expression engine implementation using [NCalc](https://github.com/ncalc/ncalc)
- `tests/DynamicExpressionEngine.Tests` – unit tests

---

## Features

- **Evaluate expressions** via `POST /api/evaluate`
- **Function catalog** via `GET /api/functions`
- **Allow-listed functions** (prevents executing arbitrary code)
- **Nested data access** using bracket syntax:
  - Object path: `[person.name]`
  - Array index: `[person.2.name]`
- **FluentValidation** model validation with a friendly error response shape
- **Swagger UI** in Development

---

## Prerequisites

- .NET SDK (the repo targets .NET via SDK-style projects; install the SDK matching the solution’s target framework)

---

## Running locally

From the repository root:

```bash
dotnet restore

dotnet run --project src/DynamicExpressionEngine.Api
```

By default (see `launchSettings.json`) the API listens on:

- HTTP: `http://localhost:5049`
- HTTPS: `https://localhost:7249`

Swagger UI (Development):

- `http://localhost:5049/swagger`

---

## Authentication (API Key)

All endpoints are secured with an API key.

Send it via header:

- `X-API-Key: <your-key>`

The key is configured in `src/DynamicExpressionEngine.Api/appsettings.json`:

```json
{
  "Authentication": {
    "ApiKey": "dev-api-key"
  }
}
```

For production, prefer setting it via environment variable:

- `Authentication__ApiKey=your-strong-key`

---

## API

### 1) Evaluate an expression

**Endpoint**

- `POST /api/evaluate`

**Request body**

```json
{
  "data": {
    "date": "22-02-2026",
    "time": "17:00"
  },
  "expression": "ToString(FormatDate(DateParseExact([date], 'dd-MM-yyyy'),'yyyy-MM-dd') + ' ' + [time] + ':00')"
}
```

**cURL**

```bash
curl -s -X POST "http://localhost:5049/api/evaluate" \
  -H "Content-Type: application/json" \
  -H "X-API-Key: dev-api-key" \
  -d '{
    "data": { "date": "22-02-2026", "time": "17:00" },
    "expression": "ToString(FormatDate(DateParseExact([date], '\''dd-MM-yyyy'\''),'\''yyyy-MM-dd'\'') + '\'' '\'' + [time] + '\'':00'\'')"
  }'
```

**200 OK**

```json
{
  "result": "2026-02-22 17:00:00",
  "error": null
}
```

**400 Bad Request** (example: missing parameter referenced in the expression)

```json
{
  "result": null,
  "error": "Missing parameter 'time'"
}
```

**400 Bad Request** (example: syntax / evaluation error)

```json
{
  "result": null,
  "error": "Invalid expression: ..."
}
```

---

### 2) List supported functions

**Endpoint**

- `GET /api/functions`

**cURL**

```bash
curl -s "http://localhost:5049/api/functions" \
  -H "X-API-Key: dev-api-key"
```

**200 OK**

```json
{
  "functions": [
    {
      "name": "DateParseExact",
      "description": "Parses a string to DateTime using exact format (InvariantCulture).",
      "parameters": ["value", "format"],
      "example": "DateParseExact('22-02-2026','dd-MM-yyyy')"
    }
  ]
}
```

---

## Data access syntax

Your `data` object is a dictionary-like JSON structure.

- Root key: `[time]`
- Nested object: `[person.name]`
- Nested array: `[person.2.name]` (0-based index)

Example:

```json
{
  "data": {
    "person": {
      "name": "Ali"
    },
    "time": "17:00"
  },
  "expression": "ToString([person.name] + ' ' + [time])"
}
```

---

## Validation & safety limits

The request validator enforces several guardrails:

- `expression` is required and has a max length
- forbidden characters are blocked (e.g. `{ } ; \\`)
- referenced parameters must exist in `data` (including nested paths)
- function calls are checked against the allow-list
- limits on number of referenced parameters / function calls

When validation fails, the API returns **HTTP 400** with a simple payload:

```json
{ "error": "<first error message>" }
```

---

## Rate limiting

The API is configured with a fixed-window rate limiter (10 requests / second window).

Note: the limiter is registered in the API startup; if you want it enforced globally, ensure it’s applied in the middleware pipeline (or per endpoint) in `Program.cs`.

---

## Technical explanations

### Why the extra abstraction layers instead of calling NCalc directly?

You might ask: **“Why did you add extra abstraction layers? Why not just use NCalc directly in the controller?”**

Because we may eventually want to replace NCalc with our **own expression engine**. By keeping the API/controller layer dependent on internal abstractions (instead of NCalc types), we can swap the underlying engine with **minimal changes**, ideally without touching controllers or API contracts at all.

This also keeps the Core project reusable and makes it easier to test/extend the engine in isolation.

---

## Cache

NCalc already keeps an internal cache by default (see the NCalc README):

- https://github.com/ncalc/ncalc?tab=readme-ov-file

If we want more control, NCalc also documents how to configure caching via a memory cache plugin:

- https://ncalc.github.io/ncalc/articles/plugins/memory_cache.html

### Why cache matters

Expression evaluation often has its biggest cost at the **parsing/compilation** stage rather than at the final execution stage. Caching previously compiled expressions can significantly reduce CPU usage and improve latency under load (especially when the same expression is evaluated many times with different input data).

When moving to production, we should think about caching strategy (size limits, eviction, memory pressure, expression diversity, etc.) and configure it accordingly.

For now, this repository is intended as a **case study**, so I didn’t add extra caching configuration beyond what NCalc provides by default.

---

## Running tests

From the repository root:

```bash
dotnet test
```

---

## Notes for production

- Always set a strong `Authentication__ApiKey`.
- Run behind HTTPS.
- Keep the function allow-list tight.
- Consider enabling global rate limiting in the middleware pipeline.

---

## License

Add your license here (for example: MIT).
