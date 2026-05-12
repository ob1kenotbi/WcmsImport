# WCMS Import Solution

A .NET 8 Web API that imports content from external WCMS platforms in parallel and notifies upstream systems upon completion.

---

## Project Structure

```
WcmsImport/
├── WcmsImport.Api/
│   ├── Controllers/        # HTTP endpoints
│   ├── Services/           # Core import logic (async + parallel)
│   ├── Repositories/       # Data access (EF Core / IQueryable)
│   ├── Notifications/      # Upstream HTTP notifier
│   ├── Models/             # ContentItem, ImportResult
│   ├── Data/               # EF Core DbContext
│   └── Program.cs          # DI wiring, middleware
│
└── WcmsImport.Tests/
    └── ImportServiceTests.cs  # xUnit unit tests (Moq + FluentAssertions)
```

---

## How to Run

```bash
# Run the API
cd WcmsImport.Api
dotnet run

# Swagger UI available at:
# http://localhost:5000
```

---

## How to Test

```bash
cd WcmsImport.Tests
dotnet test
```

---

## Sample Import Request

```http
POST /api/import
Content-Type: application/json

[
  {
    "title": "Getting Started with Our Platform",
    "body": "Welcome to our platform. Here is how to get started...",
    "sourceSystem": "WordPress",
    "contentType": "Article"
  },
  {
    "title": "Pricing Plans",
    "body": "We offer three pricing tiers...",
    "sourceSystem": "WordPress",
    "contentType": "Page"
  }
]
```

### Sample Response

```json
{
  "success": true,
  "totalRequested": 2,
  "importedIds": [
    "a1b2c3d4-...",
    "e5f6g7h8-..."
  ],
  "failedItems": [],
  "duration": "00:00:00.1240000",
  "summary": "Imported 2/2 items in 124ms. Failed: 0."
}
```

---

## Key Design Decisions

| Decision | Reason |
|---|---|
| `Parallel.ForEachAsync` | True async parallelism — not thread-blocking |
| `ConcurrentBag<T>` | Thread-safe result collection across parallel tasks |
| `IQueryable` in repository | Filtering pushed to SQL, not loaded into memory |
| Per-item error handling | One bad item doesn't abort the whole batch |
| `IUpstreamNotifier` interface | Swap HTTP for RabbitMQ/Service Bus with no service changes |
| InMemory DB (dev) | Run locally with no SQL Server setup required |
| `IHttpClientFactory` | Correct HttpClient lifetime — avoids socket exhaustion |

---

## Extending to Event-Driven (Next Step)

Replace `UpstreamNotifier` HTTP call with a message broker:

```csharp
// Instead of HttpClient POST:
await _bus.Publish(new ContentImportedEvent { ImportedIds = importedIds });

// Upstream systems subscribe independently:
public class ContentAvailableConsumer : IConsumer<ContentImportedEvent>
{
    public async Task Consume(ConsumeContext<ContentImportedEvent> context)
    {
        // React to new content — search index, cache invalidation, etc.
    }
}
```

Libraries: **MassTransit** + RabbitMQ or Azure Service Bus.
