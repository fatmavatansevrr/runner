# Antigravity — Adaptive Running App

> Calm, guilt-free running planner. Connected MVP Skeleton — Step 1 complete.

---

## Project Structure

```
runner/
├── mobile/                        # Flutter mobile app
│   ├── lib/
│   │   ├── main.dart
│   │   ├── app.dart
│   │   ├── core/
│   │   │   ├── routing/           # go_router config + route constants
│   │   │   ├── theme/             # app_colors, app_text_styles, app_spacing, app_radius, app_theme
│   │   │   ├── network/           # ApiClient (Dio wrapper)
│   │   │   └── widgets/           # Shared: buttons, cards, bottom sheet, empty/loading state
│   │   └── features/
│   │       ├── auth/              # Auth / Welcome page
│   │       ├── onboarding/        # Intro carousel, Goal selection, (+ more in Step 5)
│   │       ├── plan/              # Plan generation, preview, details (Step 5)
│   │       ├── home/              # Home page (6 states, Step 5/6)
│   │       ├── calendar/          # Calendar page (Step 5/6)
│   │       ├── training_day/      # Training day detail modal (Step 5/6)
│   │       ├── pending_confirmation/ # Pending confirmation (Step 5/6)
│   │       ├── profile/           # Profile page
│   │       └── settings/          # Settings placeholder
│   └── pubspec.yaml
│
└── backend/                       # .NET 9 Web API
    ├── RunningApp.sln
    ├── RunningApp.Api/            # Controllers, Program.cs, appsettings
    ├── RunningApp.Application/    # Service interfaces, DTOs, Adaptation engine interface
    ├── RunningApp.Domain/         # Entities, Enums
    ├── RunningApp.Infrastructure/ # (future: external service adapters)
    └── RunningApp.Persistence/    # AppDbContext, EF Core, migrations (Step 2)
```

---

## How to Run the Backend

### Prerequisites
- .NET 9 SDK
- PostgreSQL running locally (default: `localhost:5432`)

### Setup
```powershell
# Update connection string if needed
# backend/RunningApp.Api/appsettings.json → ConnectionStrings.DefaultConnection

cd backend
dotnet restore
dotnet run --project RunningApp.Api
```

Swagger UI will be available at: **http://localhost:5001/swagger**

> **Note:** Migrations do not exist yet. Step 2 will add `dotnet ef migrations add InitialCreate`.
> The API can start but any endpoint that touches the DB will throw until migrations are applied.

---

## How to Run the Flutter App

### Prerequisites
- Flutter SDK (stable channel)
- Android Studio or VS Code with Flutter extension

### Setup
```bash
cd mobile

# Install Inter font files into assets/fonts/ (download from Google Fonts)
# or temporarily remove the fonts: block from pubspec.yaml

flutter pub get
flutter run
```

> **Note:** The app currently navigates Auth → Intro Carousel → Goal Selection → Home shell.
> No real API calls are made yet (Step 6).

---

## What Is Intentionally Placeholder

| Item | Status | Added in |
|---|---|---|
| Real Adaptive Engine | ❌ Not implemented | Never (Phase 2) |
| `PlaceholderAdaptationEngine` | ✅ Present | Step 1 |
| All backend service implementations | ❌ Stub only | Step 3 |
| EF Core migrations | ❌ Not created | Step 2 |
| Seed plan templates | ❌ Not seeded | Step 2 |
| Flutter API integration | ❌ Static data only | Step 6 |
| Onboarding remaining pages | ❌ Stub folders | Step 5 |
| Real auth (JWT/Firebase) | ❌ Mock userId | Step 3+ |
| Completion / Not Today modals | ❌ TODO comments | Step 5/6 |
| Calendar grid widget | ❌ Placeholder | Step 5 |
| Inter font files | ❌ Must be downloaded | Step 5 |
| Assets (images, illustrations) | ❌ Placeholder icons | Step 5 |

---

## Next Step — Step 2

Step 2 covers the backend domain and database:

1. **Write EF Core migrations** (`dotnet ef migrations add InitialCreate`)
2. **Apply migrations** (`dotnet ef database update`)
3. **Seed 3 plan templates** (`habit_5k_beginner_3day_km_v1`, `habit_5k_beginner_4day_km_v1`, `race_5k_beginner_3day_km_v1`)
4. **Seed daily tips** (5–10 rows across workout types)
5. **Verify AppDbContext** relationships compile and apply correctly
6. **Test all entities** with a simple integration test or manual EF query

---

## Tech Decisions Made in Step 1

| Decision | Choice | Reason |
|---|---|---|
| State management | `flutter_riverpod` | Clean, testable, matches feature-based structure |
| Navigation | `go_router` | Shell routes for bottom nav, named routes |
| HTTP client | `dio` | Interceptor support for auth headers (Step 6) |
| Date formatting | `intl` | Standard Flutter date/locale formatting |
| Backend framework | .NET 9 Web API | Specified in brief |
| ORM | EF Core 9.0.1 + Npgsql | Specified in brief |
| API docs | Swashbuckle 7.3.1 | Swagger UI in development |
| Font | Inter | Extracted from design references |
| Color palette | Extracted from PNGs | No invented colors |
