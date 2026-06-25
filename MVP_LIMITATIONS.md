# MVP Limitations & Placeholders

This document outlines the architectural boundaries and placeholders implemented in the Antigravity Phase 1 MVP Skeleton. These decisions keep the initial codebase focused on flow validation and visual feedback, while cleanly segregating features reserved for the Phase 2 production roadmap.

---

## 1. Placeholder Adaptation Engine

### Description:
A real adaptive running algorithm needs to parse historic logs, calculate fatigue, analyze HRV/sleep (if integrated), and reschedule upcoming runs dynamically. For the MVP, this complexity is bypassed.

### Implementation:
- **Service**: `RunningApp.Application/Services/PlaceholderAdaptationEngine.cs`
- **Behavior**: The engine implements the `IAdaptationEngine` interface, but always returns `AdaptationAction.NoChange`. 
- **Effect**: If a user logs a workout or skips a run, the system records the decision and advances the program, but the scheduled training days in future weeks remain exactly as originally generated from the template. No workouts are dynamically shortened, lengthened, or shifted.

---

## 2. Mock Authentication

### Description:
The MVP does not integrate a real identity provider (such as Firebase Auth, Keycloak, or Auth0) and does not issue JWT tokens.

### Implementation:
- All API controllers contain a hardcoded string `private const string MockUserId = "mock-user-001";` and bind all database queries directly to this key.
- The mobile frontend has no login/password validation logic. Clicking "Sign In" or "Sign Up" immediately stores a mock session flag and redirects the user directly to the app.

---

## 3. Limited Seed Templates

### Description:
To minimize database seeding footprint, only a small subset of plan templates are seeded.

### Implementation:
- The database seeds exactly 3 plan templates in `AppDbContext.cs`:
  1. `habit_5k_beginner_3day_km_v1` (Habit 5K plan, 3 days/week, beginner level)
  2. `habit_5k_beginner_4day_km_v1` (Habit 5K plan, 4 days/week, beginner level)
  3. `race_5k_beginner_3day_km_v1` (Race 5K plan, 3 days/week, beginner level)
- **Effect**: If an onboarding user selects a Marathon or Half Marathon goal, or chooses an advanced running level, the backend plan generator defaults to picking `habit_5k_beginner_3day_km_v1` as a fallback. 
- *Note: While onboarding inputs work end-to-end, only 5K Beginner configurations yield a plan that matches the parameters exactly.*

---

## 4. No Push Notifications

### Description:
There are no push notification services (such as Firebase Cloud Messaging or OneSignal) integrated into the mobile app or backend.

### Implementation:
- Notification preference settings on the Settings page are stubs and do not register device push tokens or schedule push dispatches.

---

## 5. No Strava / Wearables Integration

### Description:
Automated run tracking via GPS, smartwatches (Garmin, Apple Watch), or external services (Strava, Apple Health, Google Fit) is not supported.

### Implementation:
- All completions must be logged manually by the runner through the "Log Workout" modal, typing the actual distance run and duration spent.

---

## 6. No Redis / Cache Layer

### Description:
To keep database hosting and local developer setup simple, no distributed cache (Redis) or memory cache layer is configured.

### Implementation:
- All queries from controllers call application services that query PostgreSQL directly.
- Riverpod data caching on the mobile client serves as the primary cache during user sessions.

---

## 7. No Subscription / Paywall System

### Description:
There are no stripe adapters, paywalls, or premium gating mechanics implemented in this phase.

### Implementation:
- Access to all plan types, profile settings, and calendar entries is completely free and ungated.
