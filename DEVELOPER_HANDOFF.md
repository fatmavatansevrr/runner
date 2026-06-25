# Developer Handoff Guide

Welcome to the Antigravity codebase! This guide is designed to get a new developer up to speed on codebase patterns, common development workflows, and the planned order for production features.

---

## 1. Quick Start

### 1.1 Backend Setup
1. Verify PostgreSQL is running locally on port `5432`.
2. Update the connection string in [backend/RunningApp.Api/appsettings.json](file:///c:/Users/vatan/Desktop/runner/backend/RunningApp.Api/appsettings.json).
3. Apply the existing migrations:
   ```bash
   cd backend
   dotnet ef database update --project RunningApp.Persistence --startup-project RunningApp.Api
   ```
4. Start the backend:
   ```bash
   dotnet run --project RunningApp.Api
   ```

### 1.2 Frontend Setup
1. Verify the Flutter SDK is installed and on your path.
2. Launch a simulator or connect a test device.
3. Configure the base URL in [mobile/lib/core/network/api_client.dart](file:///c:/Users/vatan/Desktop/runner/mobile/lib/core/network/api_client.dart) if running on an Android emulator (use `http://10.0.2.2:5001/api/v1`) or physical device (use local IP).
4. Run the app:
   ```bash
   cd mobile
   flutter pub get
   flutter run
   ```

---

## 2. Key Code Areas

### 2.1 Backend
- **Database Context**: [AppDbContext.cs](file:///c:/Users/vatan/Desktop/runner/backend/RunningApp.Persistence/AppDbContext.cs) handles relational configuration, snake_case enum converters, and data seeding.
- **Application Services**: [QueryAndMutationServices.cs](file:///c:/Users/vatan/Desktop/runner/backend/RunningApp.Application/Services/QueryAndMutationServices.cs) implements service interfaces (querying plans, logging runs, resolving confirmations).
- **Plan Instantiation Logic**: [PlanServices.cs](file:///c:/Users/vatan/Desktop/runner/backend/RunningApp.Application/Services/PlanServices.cs) extracts templates, maps slots, and assigns dates based on chosen starting parameters.

### 2.2 Frontend
- **App Router**: [app_router.dart](file:///c:/Users/vatan/Desktop/runner/mobile/lib/core/routing/app_router.dart) registers pre-auth routes, main tab shell layout, and overlay routes.
- **Theme Tokens**: [app_colors.dart](file:///c:/Users/vatan/Desktop/runner/mobile/lib/core/theme/app_colors.dart) outlines the color palette.
- **API Client**: [api_client.dart](file:///c:/Users/vatan/Desktop/runner/mobile/lib/core/network/api_client.dart) wraps Dio, converting requests to snake_case and providing default timeouts.

---

## 3. Common Workflows

### 3.1 DB Schema Migration
If you modify or add domain models in `RunningApp.Domain`:
1. Add migration:
   ```bash
   dotnet ef migrations add <MigrationName> --project RunningApp.Persistence --startup-project RunningApp.Api
   ```
2. Update local database:
   ```bash
   dotnet ef database update --project RunningApp.Persistence --startup-project RunningApp.Api
   ```

### 3.2 Testing Changes (Reset Database)
To quickly wipe the database and test the onboarding flow again:
- Trigger a POST request to `/api/v1/testing/reset` via Swagger or Postman, or use the dev utility reset command.
- Hot restart the Flutter app to return to the pre-auth Welcome page.

---

## 4. Technical Debt Items (Refactoring Candidates)

Before moving to production, address the following debt items:

- **Mock Authentication**: Replace the hardcoded `mock-user-001` identifier with a real user claims principal using ASP.NET Core Authentication middleware and JWT validation.
- **Hardcoded API URL**: Move the API URL in Flutter out of the hardcoded client code and reference it via `--dart-define` or compilation environmental variables.
- **Infrastructure Project**: Implement concrete adapter implementations in `RunningApp.Infrastructure` rather than stubs.
- **Error Handling**: Replace raw `e.toString()` exposures in snackbars with user-friendly messages, while sending complete exceptions to a log ingestion service.
- **Unit & Integration Tests**: Write test suites for both frontend Riverpod providers/repositories and backend services/controllers (zero tests exist currently).

---

## 5. Future Implementation Order (Phase 2 Roadmap)

When starting Phase 2, implement features in the following logical sequence:

1. **Authentication (JWT)**: Setup identity provider, add token interceptors to Dio client, and authorize API endpoints.
2. **Additional Plan Templates**: Seed additional schedules for Marathon, Half Marathon, and 10K distances, supporting varying runner experience levels.
3. **Wearable Integrations**: Wire Apple HealthKit / Google Fit SDKs in the mobile app, and create endpoints in the backend to sync activity logs.
4. **Adaptive Engine implementation**: Replace the `PlaceholderAdaptationEngine` with real rescheduling algorithms (e.g., shifting runs, increasing rest intervals) based on compliance metrics.
5. **Real-time Notifications**: Setup Firebase Cloud Messaging for morning workout reminders and weekly summaries.
