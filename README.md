# FullStackSolutionBlazorCore

![App Screenshot](https://github.com/ksunami/FullStackSolutionBlazorCore/blob/main/docs/images/app-screenshot.png)
![Swagger UI](https://github.com/ksunami/FullStackSolutionBlazorCore/blob/main/docs/images/swagger-ui.png)
![Log Sample](https://github.com/ksunami/FullStackSolutionBlazorCore/blob/main/docs/images/log-sample.png)

> Repository: https://github.com/ksunami/FullStackSolutionBlazorCore

A clean, minimal, and practical full‑stack sample using **Blazor WebAssembly** (Client) + **ASP.NET Core Minimal API** (Server).
The solution emphasizes **clarity over ceremony**, **structured logging with Serilog**, and **simple integration** between client and server.

---

## 🧱 Solution Layout

```
FullStackApp/
├─ FullStackSolution.sln
├─ ServerApp/                 # ASP.NET Core Minimal API
│  ├─ Program.cs              # Endpoints, Serilog, CORS, Swagger, Health
│  ├─ Models/                 # DTOs / entities
│  ├─ Data/                   # In-memory repository
│  ├─ Logs/                   # Serilog writes here (file sink)
│  ├─ appsettings.json        # Cors:AllowedOrigins, Auth:ApiToken, Serilog config
│  ├─ ServerApp.http          # HTTP requests for testing
│  └─ api-test.http           # More test requests
└─ ClientApp/                 # Blazor WebAssembly
   ├─ Program.cs              # HttpClient BaseAddress, Blazored.LocalStorage
   ├─ Pages/                  # Home, FetchProducts
   ├─ Layout/                 # MainLayout, NavMenu
   ├─ Models/                 # Shared client models
   └─ wwwroot/                # Static assets
```

---

## 🏃 Run

### 1) Server (API)
```bash
cd ServerApp
dotnet run
```
- **Swagger**: http://localhost:5206/swagger
- **Health check**: http://localhost:5206/health

> CORS is restricted via `Cors:AllowedOrigins` in `appsettings*.json`. Add your client origin (e.g., `http://localhost:5173` or Blazor dev origin).  
> Auth: minimal bearer token gate for `/api/*` if `Auth:ApiToken` is configured.

### 2) Client (Blazor WebAssembly)
```bash
cd ClientApp
dotnet run
```
The client uses:
```csharp
builder.Services.AddScoped(sp => new HttpClient {{ BaseAddress = new Uri("http://localhost:5206") }});
```
So the app expects the API at **http://localhost:5206**.

---

## 🔌 API (Minimal)

**GET /api/productlist**
- **Query**: `page`, `pageSize`, `search`
- **Caching**: `IMemoryCache` with sliding expiration (10 min) for the full list
- **Logging**: emits count, page, and size via Serilog
- **Auth (optional)**: middleware enforces `Authorization: Bearer <token>` for `/api/*` if `Auth:ApiToken` is present

```http
GET http://localhost:5206/api/productlist?page=1&pageSize=10&search=laptop
Authorization: Bearer <token_if_configured>
```

**Health**: `GET /health`  
**Swagger**: enabled in Development

---

## 🎛 Logging (Serilog)

- Configured via `appsettings*.json` and `builder.Host.UseSerilog()`
- Adds **RequestId** to the log context
- Measures request duration (ms) and writes structured logs to console and file (`Logs/`)

---

## 🧩 ClientApp Highlights

- **Blazor WASM**
- **HttpClient** wired to the server base URL
- **Blazored.LocalStorage** for simple persistence
- **Pages**
  - `Home.razor`: landing
  - `FetchProducts.razor`: search + pagination (`/fetchproducts/{{currentPage}}/{{pageSize}}/{{searchTerm?}}`)

---

## 📸 Screenshots (placeholders)

Put real screenshots in `docs/images/` so they render here:
- `docs/images/app-screenshot.png` — Home / FetchProducts page
- `docs/images/swagger-ui.png` — Swagger endpoint list and schema
- `docs/images/log-sample.png` — Sample of Serilog output (console or file)

```bash
docs/
└─ images/
   ├─ app-screenshot.png
   ├─ swagger-ui.png
   └─ log-sample.png
```

---

## 🧪 .http Files

- `ServerApp.http` and `api-test.http` include ready‑to‑run requests (VS/VS Code REST clients).
- Use them to validate `GET /api/productlist`, headers, and tokens quickly.

---

## 🧠 Copilot‑Assisted Activities (Reflective Summary)

### Activity 1 — Using Microsoft Copilot to Generate and Refine Integration Code
Copilot helped scaffold the **Minimal API** endpoint and the **client integration** (DTOs, `HttpClient` calls, pagination and search). I iterated on its suggestions to keep the surface area minimal and the naming consistent. It also proposed guard clauses and null‑checks that I kept.

### Activity 2 — Debugging and Fixing Integration Issues with Copilot
When the client and server models drifted, Copilot flagged mismatches and suggested fixes (e.g., casing and nullable fields). It also helped trace CORS issues and proposed the policy + middleware placement (`UseCors`) that finally unblocked requests.

### Activity 3 — Creating and Managing JSON with Microsoft Copilot
For sample data and payloads, Copilot generated JSON fixtures and parsing snippets. I used those to verify `search`, `page`, and `pageSize` behavior, and to validate the endpoint with the `.http` files.

### Activity 4 — Optimizing Integration Code for Performance Using Microsoft Copilot
Copilot recommended **IMemoryCache** for the product list, async patterns, and simplified LINQ for paging. Its tips reduced allocations and tightened up the hot path, especially under repeated queries.

> Overall, Copilot acted as a pragmatic pair‑programmer: it accelerated boilerplate, spotted subtle issues early, and nudged the code toward cleaner, more testable patterns—without getting in the way of my style.

---

## ✅ Tips & Notes

- Keep **Minimal API** focused: one responsibility per endpoint, push data shaping to a dedicated repository/service.
- Tune **Serilog** sinks and minimum levels per environment; keep logs structured.
- Use **CORS** lists from config; avoid `AllowAnyOrigin` in production.
- Cache whole lists sparingly; invalidate or reduce TTL if the data changes often.
- Prefer **.http** files for quick end‑to‑end checks while iterating.

---

## 📜 License
MIT
