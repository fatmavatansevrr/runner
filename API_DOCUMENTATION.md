# Antigravity API Documentation

This document lists all endpoints implemented in the ASP.NET Core Web API backend, including request/response payloads, error behaviors, authentication notes, and mock-user logic.

---

## 1. Authentication & Mock Behavior

In Phase 1, there is **no authentication provider integration** (e.g. Firebase, Cognito, or JWT verification).
- All incoming requests assume they are made on behalf of a single mock identity: `mock-user-001`.
- Controllers never reference that literal directly. They resolve the current user via `ICurrentUserAccessor.UserId`, which delegates to `IIdentityProvider.GetCurrentIdentity()` — today backed by `MockIdentityProvider`, the only place in the solution that knows `"mock-user-001"`. See the root `README.md` §6 for how a real identity provider plugs into this abstraction later.
- If database tables do not contain records for `mock-user-001` (for instance, if the app is opened for the first time), the `GET /api/v1/me/bootstrap` endpoint will indicate that no active profile or plan exists.

---

## 2. Global Conventions
- **Routing prefix**: `/api/v1`
- **JSON Property Naming**: Lowercase snake_case (e.g., `goal_type` instead of `GoalType`), handled by EF Core and ASP.NET JSON serialization policies.
- **Date formats**: ISO 8601 strings (e.g., `2026-06-25T00:00:00Z`).

---

## 3. Endpoints Detail

### 3.1 Bootstrap
#### `GET /api/v1/me/bootstrap`
Used on application launch to determine the state of the user. Checks if user profile and active plans exist.

- **Request**: None.
- **Success Response (200 OK)**:
  ```json
  {
    "has_profile": true,
    "has_active_plan": true
  }
  ```

---

### 3.2 Plans & Generation

#### `POST /api/v1/plans/generate-preview`
Generates a draft training plan preview based on user parameters, before committing it to the database.

- **Request Body**:
  ```json
  {
    "goal_type": "habit",
    "goal_distance": "five_k",
    "level": "beginner",
    "days_per_week": 3,
    "unit": "km",
    "race_name": null,
    "race_date": null,
    "target_finish_time_seconds": null
  }
  ```
- **Success Response (200 OK)**:
  ```json
  {
    "template_id": "habit_5k_beginner_3day_km_v1",
    "total_weeks": 8,
    "estimated_end_date": "2026-08-20T00:00:00Z",
    "preview_days": [
      {
        "week_number": 1,
        "day_number": 1,
        "day_type": "easy",
        "title": "Easy Run",
        "description": "Conversation pace run",
        "planned_distance_km": 3.0,
        "planned_duration_min": 25,
        "is_long_run": false
      },
      {
        "week_number": 1,
        "day_number": 2,
        "day_type": "rest",
        "title": "Rest Day",
        "description": "No running today.",
        "planned_distance_km": 0.0,
        "planned_duration_min": 0,
        "is_long_run": false
      }
    ]
  }
  ```

#### `POST /api/v1/plans/confirm`
Saves and activates the generated plan preview, creating database records for user profile and training calendar days.

- **Request Body**:
  ```json
  {
    "goal_type": "habit",
    "goal_distance": "five_k",
    "level": "beginner",
    "days_per_week": 3,
    "unit": "km",
    "race_name": null,
    "race_date": null,
    "target_finish_time_seconds": null,
    "start_date": "2026-06-25T00:00:00Z",
    "long_run_day": "Sunday"
  }
  ```
- **Success Response (200 OK)**:
  ```json
  {
    "plan_id": "4b6c3d9a-41f2-43bb-a579-3fb2e3ccdf54",
    "message": "Training plan created and activated successfully."
  }
  ```

#### `GET /api/v1/plans/active/home`
Fetches today's workout details, the current week summary, active plan stats, and a daily tip.

- **Request**: None.
- **Success Response (200 OK)**:
  ```json
  {
    "active_plan": {
      "plan_id": "4b6c3d9a-41f2-43bb-a579-3fb2e3ccdf54",
      "goal_type": "habit",
      "goal_distance": "five_k",
      "level": "beginner",
      "progress_text": "Week 1 of 8"
    },
    "today_workout": {
      "day_id": "c1f7a39d-2fb8-410a-9d33-3d9a6df7b29a",
      "date": "2026-06-25T00:00:00Z",
      "day_type": "easy",
      "status": "planned",
      "title": "Easy 3.0k Run",
      "description": "Run at a conversational, easy pace for 3.0 km.",
      "planned_distance_km": 3.0,
      "planned_duration_min": 25,
      "planned_pace_min_km": 8.33,
      "intensity": "easy",
      "actual_distance_km": null,
      "actual_duration_min": null,
      "is_long_run": false,
      "can_mark_complete": true,
      "can_mark_not_today": true
    },
    "daily_tip": {
      "title": "Hydration is Key",
      "message": "Drink water throughout the day, not just before running."
    },
    "week_summary": [
      {
        "day_id": "c1f7a39d-2fb8-410a-9d33-3d9a6df7b29a",
        "date": "2026-06-25T00:00:00Z",
        "day_type": "easy",
        "status": "planned",
        "planned_distance_km": 3.0,
        "actual_distance_km": null
      }
    ],
    "has_pending_confirmations": false
  }
  ```

#### `GET /api/v1/plans/active/calendar?month=YYYY-MM`
Fetches a list of workouts for a specific month. Populates rest day placeholders if a date has no explicit workout in the plan database.

- **Query Parameters**: `month` (format `YYYY-MM`).
- **Success Response (200 OK)**:
  ```json
  [
    {
      "day_id": "c1f7a39d-2fb8-410a-9d33-3d9a6df7b29a",
      "date": "2026-06-25T00:00:00Z",
      "day_type": "easy",
      "status": "planned",
      "title": "Easy Run",
      "description": "Easy run.",
      "planned_distance_km": 3.0,
      "planned_duration_min": 25,
      "actual_distance_km": null,
      "actual_duration_min": null,
      "is_long_run": false,
      "can_mark_complete": true,
      "can_mark_not_today": true
    }
  ]
  ```

#### `GET /api/v1/plans/active/details`
Fetches high-level metrics of the active plan for display on the Profile and Plan Details screens.

- **Request**: None.
- **Success Response (200 OK)**:
  ```json
  {
    "plan_id": "4b6c3d9a-41f2-43bb-a579-3fb2e3ccdf54",
    "goal_type": "habit",
    "goal_distance": "five_k",
    "level": "beginner",
    "total_weeks": 8,
    "completed_weeks_count": 0,
    "total_completed_distance": 0.0,
    "weeks": []
  }
  ```

#### `POST /api/v1/plans/{planId}/cancel`
Cancels the active training plan and sets its status to `Cancelled`.

- **Route Parameters**: `planId` (GUID)
- **Request Body**:
  ```json
  {
    "reason": "Injured"
  }
  ```
- **Success Response (200 OK)**:
  ```json
  {
    "plan_id": "4b6c3d9a-41f2-43bb-a579-3fb2e3ccdf54",
    "status": "cancelled",
    "message": "Training plan stopped successfully."
  }
  ```

---

### 3.3 Training Days & Logging

#### `GET /api/v1/training-days/{dayId}`
Fetches full details of a specific training day.

- **Route Parameters**: `dayId` (GUID)
- **Success Response (200 OK)**:
  ```json
  {
    "day_id": "c1f7a39d-2fb8-410a-9d33-3d9a6df7b29a",
    "date": "2026-06-25T00:00:00Z",
    "day_type": "easy",
    "status": "planned",
    "title": "Easy Run",
    "description": "Run conversational pace.",
    "planned_distance_km": 3.0,
    "planned_duration_min": 25,
    "planned_pace_min_km": 8.33,
    "intensity": "easy",
    "actual_distance_km": null,
    "actual_duration_min": null,
    "can_mark_complete": true,
    "can_mark_not_today": true
  }
  ```

#### `POST /api/v1/training-days/{dayId}/complete`
Logs the completion details (actual distance and duration) of a training day.

- **Route Parameters**: `dayId` (GUID)
- **Request Body**:
  ```json
  {
    "actual_distance_km": 3.2,
    "actual_duration_min": 24,
    "notes": "Felt great, pushed slightly faster."
  }
  ```
- **Success Response (200 OK)**:
  ```json
  {
    "day_id": "c1f7a39d-2fb8-410a-9d33-3d9a6df7b29a",
    "status": "completed",
    "actual_distance_km": 3.2,
    "actual_duration_min": 24
  }
  ```

#### `POST /api/v1/training-days/{dayId}/not-today-decisions`
Indicates that the user is skipping today's run. Creates an unconfirmed decision.

- **Route Parameters**: `dayId` (GUID)
- **Request Body**:
  ```json
  {
    "reason": "Too busy"
  }
  ```
- **Success Response (200 OK)**:
  ```json
  {
    "decision_id": "c04481b2-132d-45db-b27b-23ac78cf95a1",
    "day_id": "c1f7a39d-2fb8-410a-9d33-3d9a6df7b29a",
    "status": "pending_confirmation",
    "message": "Not today choice logged. Requires confirmation."
  }
  ```

---

### 3.4 Not Today Decisions & Pending Confirmations

#### `POST /api/v1/not-today-decisions/{decisionId}/confirm`
Confirms the decision to skip a workout, shifting it into a confirmed missed day.

- **Route Parameters**: `decisionId` (GUID)
- **Request Body**: None (or empty json `{}`)
- **Success Response (200 OK)**:
  ```json
  {
    "decision_id": "c04481b2-132d-45db-b27b-23ac78cf95a1",
    "status": "confirmed",
    "message": "Not today decision confirmed. Schedule updated."
  }
  ```

#### `GET /api/v1/pending-confirmations`
Retrieves all workouts marked "Not Today" that require final user resolution (Move to Rest, Shift to Tomorrow, or Mark Completed).

- **Success Response (200 OK)**:
  ```json
  [
    {
      "pending_confirmation_id": "7fa84b81-c7ee-45df-b749-cc2fa6df19ba",
      "day_id": "c1f7a39d-2fb8-410a-9d33-3d9a6df7b29a",
      "date": "2026-06-24T00:00:00Z",
      "day_type": "easy",
      "planned_distance_km": 3.0,
      "reason": "Too busy"
    }
  ]
  ```

#### `POST /api/v1/pending-confirmations/resolve`
Resolves a pending workout confirmation with an action.

- **Request Body**:
  ```json
  {
    "pending_confirmation_id": "7fa84b81-c7ee-45df-b749-cc2fa6df19ba",
    "action": "rest", 
    "logged_distance_km": null,
    "logged_duration_min": null
  }
  ```
  *Note: `action` values must be one of `rest` (treat as rest/missed), `tomorrow` (reschedule to next rest day), or `log` (log retroactively).*
- **Success Response (200 OK)**:
  ```json
  {
    "pending_confirmation_id": "7fa84b81-c7ee-45df-b749-cc2fa6df19ba",
    "resolved": true,
    "action_taken": "rest"
  }
  ```

---

### 3.5 Profile & Settings

#### `GET /api/v1/profile/overview`
Gets general user profile stats for the Profile tab.

- **Success Response (200 OK)**:
  ```json
  {
    "name": "Jane Doe",
    "running_background": "beginner",
    "weekly_streak": 2,
    "all_time_runs": 12,
    "all_time_distance_km": 42.5
  }
  ```

#### `GET /api/v1/settings/preferences`
Fetches active notifications settings (returns static JSON payload).

- **Success Response (200 OK)**:
  ```json
  {
    "reminder_style": "balanced",
    "workout_reminders_enabled": true,
    "evening_reminder_enabled": true,
    "reminder_time": "08:00"
  }
  ```

---

### 3.6 Dev Utilities

#### `POST /api/v1/testing/reset`
**Development Environment Only**. Wipes all records for `mock-user-001` (user profiles, plans, calendar days, workout logs, decisions, previews) allowing a clean, fresh start.

- **Success Response (200 OK)**:
  ```json
  {
    "message": "Database cleared for user 'mock-user-001'. Active plan, progress, and logs have been reset."
  }
  ```
- **Error Response (403 Forbidden)**: Returned if the environment is not `Development`.
  ```
  "Testing endpoints are only available in Development mode."
  ```

---

## 4. Error Response Format

Every 4xx/5xx response uses the same standardized envelope
(`RunningApp.Api/ErrorHandling/ApiErrorResponse.cs`), produced by the global
exception handler:

```json
{
  "errorCode": "VALIDATION_ERROR",
  "message": "Invalid month format. Expected YYYY-MM.",
  "correlationId": "451b0af957fc4397b890a174ec78fba2"
}
```

`errorCode` is one of:

| errorCode | HTTP status | Thrown by |
|---|---|---|
| `NOT_FOUND` | 404 | `NotFoundAppException` (e.g. missing preview, plan, training day, decision) |
| `CONFLICT` | 409 | `ConflictAppException` (e.g. an expired plan preview) |
| `VALIDATION_ERROR` | 400 | `ArgumentException` (e.g. invalid `month` query parameter) |
| `INTERNAL_ERROR` | 500 | anything else — the real exception is logged server-side with the same `correlationId`, never echoed to the client |

Send a `X-Correlation-Id` request header to control/trace the `correlationId`
yourself; otherwise the server generates one and echoes it back as a
response header too.

Note: this error envelope intentionally uses camelCase, distinct from the
snake_case used by every success-response DTO — clients can branch on
status code alone without needing to know each endpoint's success shape.
