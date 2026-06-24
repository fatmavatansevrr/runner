# Adaptive Running App — Antigravity MVP Skeleton Build Brief

**Document purpose:** This is the single source-of-truth brief for Antigravity to build the first connected MVP skeleton of the adaptive running mobile app.

**Primary goal of this phase:** Build the complete app skeleton end-to-end: Flutter pages, navigation, .NET backend API skeleton, PostgreSQL data model, endpoint contracts, and basic working flows. Do **not** build the real Adaptive Engine yet.

**Build style:** small, readable, maintainable, boring architecture. Prefer clear working flows over over-engineered abstractions.

---

# 1. Product Summary

This is a calm adaptive running planner mobile app for adults who want to start running, return to running, or train toward a race without pressure, guilt, or competitive comparison.

The app should feel like a supportive running companion, not a performance dashboard. It should help the user understand today’s run, follow a weekly plan, mark runs as completed or missed/not completed, and continue their plan without feeling punished.

## 1.1 Product Positioning

Working positioning:

> A gentle adaptive running planner that adjusts with real life.

Alternative internal wording:

> A calm running companion that helps users build consistency without guilt.

## 1.2 Target Users

Target users are adults who:
- Are new to running or returning after a break.
- Want a structured plan but do not want pressure-heavy coaching.
- May feel guilty when they miss workouts.
- Need flexibility because of work, fatigue, weather, travel, or life interruptions.
- Want clarity on what to do today.
- Want a plan that feels safe, sustainable, and realistic.

## 1.3 Product Philosophy

The app should prioritize:
- Consistency over perfection.
- Sustainable progress over aggressive performance.
- Clarity over analytics overload.
- Supportive language over guilt.
- Gentle accountability over streak pressure.
- A single clear daily focus over complex dashboards.

The app should avoid:
- Competitive leaderboards.
- Social comparison.
- Streak shaming.
- “You failed” messaging.
- Aggressive notifications.
- Dense performance analytics in the first version.
- Overly childish mascot behavior.

---

# 2. Phase 1 Decision: Connected MVP Skeleton Only

This first implementation phase is not the final product. It is the connected skeleton.

## 2.1 Phase 1 Goal

Build a functioning app where:
- All core pages exist.
- Navigation works.
- Main frontend states are represented.
- Backend endpoints exist.
- Database entities exist.
- Plan preview and plan confirm flow works with seed/static data.
- Home, Calendar, Profile read from backend data.
- Complete, Not Today, and Pending Confirmation flows update backend state.
- Adaptive Engine is only a placeholder.

## 2.2 Phase 1 Must Not Implement

Do not implement the real Adaptive Engine.

Do not implement:
- Real adaptive plan generation algorithm.
- RuleEvaluator.
- SafetyValidator.
- Missed score calculation.
- Recovery week logic.
- Real interval/long-run rescheduling.
- Weekly load calculation.
- Fitness-based progression.
- Real plan optimization.
- Strava / Apple Health / Garmin integrations.
- Real push notification scheduling.
- Redis caching.
- Subscription/paywall.
- Founder access logic.
- Pause/resume plan.
- Advanced analytics.
- Admin panel.
- Final mascot animations.
- Production legal/account deletion flow unless explicitly requested later.

## 2.3 Important Placeholder Decision

Adaptive behavior must be represented structurally, but not implemented.

Create these types/interfaces so the real engine can be added later:
- `IAdaptationEngine`
- `PlaceholderAdaptationEngine`
- `AdaptationDecision`
- `AdaptationAction`
- `TriggerSource`

For now, the placeholder engine returns safe fixed responses and does not mutate future training days.

## 2.4 Design Reference Policy for Screens

The app already has intended screen designs, wireframes, or screenshots. Antigravity must use those provided screen references as the visual source of truth.

The goal of the MVP Skeleton phase is not final pixel-perfect UI, but the screens must clearly follow the provided design references. Do not invent a new visual design direction.

### 2.4.1 Non-Negotiable Design Rules

Antigravity must not:
- Replace the app with generic Material Design screens.
- Create its own visual design language.
- Redesign page hierarchy.
- Move primary CTA buttons unless technically necessary.
- Change bottom navigation behavior.
- Change modal/bottom sheet behavior without instruction.
- Introduce new colors, card styles, or typography that do not match the references.

Antigravity must:
- Follow the provided screenshots/wireframes as the primary reference.
- Match layout hierarchy as closely as possible.
- Preserve card structure and visual priority.
- Preserve primary CTA placement.
- Preserve bottom navigation placement and behavior.
- Preserve modal/bottom sheet interaction patterns.
- Reuse existing components instead of creating one-off UI per screen.
- Use placeholder mascot/illustration assets only when final assets are missing.

### 2.4.2 Design References Input Format

Screen references should be provided with clear file names. Recommended naming:

```text
/design-references
  01_auth_welcome.png
  02_intro_carousel.png
  03_goal_selection.png
  04_race_details.png
  05_running_background.png
  06_habit_goal_selection.png
  07_custom_goal.png
  08_weekly_frequency.png
  09_running_days_selection.png
  10_long_run_day_preference.png
  11_start_date_selection.png
  12_plan_generation_loading.png
  13_plan_preview.png
  14_home_planned.png
  15_home_completed.png
  16_home_missed_not_completed.png
  17_home_rest_day.png
  18_home_no_active_plan.png
  19_home_plan_completed.png
  20_calendar_month.png
  21_training_day_detail_modal.png
  22_completion_modal.png
  23_not_today_modal.png
  24_pending_confirmation.png
  25_profile.png
  26_plan_details.png
  27_stop_plan_modal.png
  28_settings_placeholder.png
```

If a screen has multiple states, each state should be treated as a separate reference image. For example, Home Planned, Home Completed, Home Missed/Not Completed, Home Rest Day, and Home No Active Plan should not be treated as the same screen.

### 2.4.3 Screen-Level Implementation Rule

For every screen implementation, Antigravity should identify:
- Reference image name.
- Screen purpose.
- Main layout sections.
- Primary CTA placement.
- Secondary actions.
- Modal/bottom sheet behavior.
- Empty/loading/error state behavior.
- Backend endpoint used, if any.

If a screen has a provided reference:
- Follow the reference image as closely as possible.
- Match spacing, card hierarchy, visual weight, and CTA placement.
- Keep the same emotional tone.

If a screen does not have a provided reference:
- Use the closest existing component pattern.
- Keep the screen simple.
- Use the shared design system tokens.
- Do not invent a new UI style.

### 2.4.4 Shared Flutter Design System Requirement

Before implementing page screens, first create a shared Flutter design system based on the provided references.

Create reusable tokens/components for:
- Color tokens.
- Typography tokens.
- Spacing tokens.
- Border radius values.
- Button styles.
- Card styles.
- Input styles.
- Modal/bottom sheet styles.
- Bottom navigation style.
- Calendar cell style.
- Workout status indicators.
- Empty state component.
- Loading state component.

Suggested Flutter structure:

```text
lib/
  core/
    theme/
      app_colors.dart
      app_text_styles.dart
      app_spacing.dart
      app_radius.dart
      app_theme.dart
    widgets/
      app_button.dart
      app_card.dart
      app_bottom_sheet.dart
      app_progress_dots.dart
      selectable_card.dart
      workout_type_badge.dart
      status_indicator.dart
      empty_state.dart
      loading_state.dart
```

### 2.4.5 MVP Skeleton Design Fidelity

The MVP Skeleton does not need to be final pixel-perfect. However, it must not look like a generic Flutter starter template.

Priority order for this phase:
1. Correct screen hierarchy.
2. Correct navigation and state transitions.
3. Correct shared component usage.
4. Correct visual direction from the references.
5. Backend integration.
6. Later page-by-page visual refinement.

### 2.4.6 Implementation Order for UI

When building the Flutter side, follow this order:
1. Read the design references.
2. Create shared design tokens and reusable components.
3. Build page shells using the shared design system.
4. Connect navigation between pages.
5. Connect pages to backend endpoints.
6. Add loading, empty, and error states.
7. Refine individual screens one by one after the connected skeleton works.


---

# 3. Tech Stack

## 3.1 Mobile Frontend

Use Flutter.

Recommended Flutter principles:
- Feature-based folder structure.
- Clean, readable state management.
- API client layer separated from UI.
- DTO/model classes separated from widgets.
- Simple reusable UI components for cards, buttons, modals, calendar cells.
- Mobile-first design.

Suggested packages may include:
- `go_router` for navigation.
- `dio` or `http` for API calls.
- `flutter_riverpod` or a similarly clean state approach.
- `intl` for date formatting.
- `table_calendar` or custom lightweight calendar widget.

If a package decision is uncertain, prefer the simplest stable option and document it.

## 3.2 Backend

Use .NET 8 or .NET 9 Web API.

Recommended backend structure:
- `RunningApp.Api`
- `RunningApp.Application`
- `RunningApp.Domain`
- `RunningApp.Infrastructure`
- `RunningApp.Persistence`

Use:
- EF Core.
- PostgreSQL.
- Swagger/OpenAPI.
- DTO request/response models.
- Service layer for business flows.
- Placeholder auth/JWT validation structure.

## 3.3 Database

Use PostgreSQL.

For Phase 1:
- PostgreSQL is the source of truth.
- Do not implement Redis.
- Do not implement cache invalidation infrastructure beyond comments or placeholder interfaces.
- Use seed/static plan templates for preview and confirm.

## 3.4 Auth

Authentication UI must exist in Flutter:
- Google Sign-In button.
- Apple Sign-In button.
- Email/password sign-up screen.

For Phase 1:
- Real Firebase/Supabase Auth integration can be left as a placeholder unless explicitly requested.
- Backend should be structured as if JWT auth will be added later.
- API ownership validation should be represented in service method structure, but can use a mock/current user ID during skeleton development.

---

# 4. MVP Scope

## 4.1 Build in Phase 1

Pages and flows to build:
- Authentication / Welcome entry.
- Intro carousel.
- Goal selection.
- Race details.
- Running background.
- Habit goal selection.
- Custom goal basic screen.
- Weekly frequency.
- Running days selection.
- Long run day preference for race flow.
- Start date selection.
- Plan generation/loading screen.
- Plan preview.
- First Home entry.
- Home states.
- Calendar screen.
- Training day detail modal/page.
- Complete flow.
- Not Today flow.
- Pending confirmation flow.
- Profile screen.
- Plan details screen.
- Stop plan modal.
- No active plan state.
- Basic plan completed state.
- Basic settings placeholder.

Backend flows to build:
- Bootstrap routing.
- Plan preview.
- Plan confirm.
- Home read.
- Calendar read.
- Training day detail read.
- Complete workout.
- Not Today decision create.
- Not Today decision confirm.
- Pending confirmations read.
- Pending confirmations resolve.
- Profile overview.
- Active plan details.
- Cancel/stop plan.

## 4.2 Future Scope — Do Not Build Now

Do not build in Phase 1:
- Real adaptive engine.
- Real notifications.
- Strava/Health detected run flows.
- Completion candidates.
- Pause/resume.
- Subscription/founder logic.
- Full settings edit system.
- Full achievements/badges system.
- Advanced workout stats.
- Mood-based recommendations.
- Social/community features.
- Weather-aware logic.
- Admin panel.
- Dark mode.
- Multi-language support.

---

# 5. Core Navigation Map

## 5.1 App Bootstrap Routing

On app start:

```text
Splash
→ GET /api/v1/me/bootstrap
→ route based on nextScreen
```

Possible `nextScreen` values:
- `Welcome`
- `Onboarding`
- `PlanSetup`
- `PendingConfirmation`
- `Home`
- `NoActivePlan`

Routing rules:
- If user is not authenticated: show Auth / Welcome.
- If profile/onboarding is incomplete: show onboarding.
- If there are unresolved past runs: show Pending Confirmation.
- If active plan exists: show Home.
- If no active plan exists: show No Active Plan state.

## 5.2 Bottom Navigation

Main app tabs:
- Calendar
- Home
- Profile

Default destination after active plan creation:
- Home

## 5.3 Onboarding Navigation

```text
Auth / Welcome
→ Intro Carousel
→ Goal Selection
→ Conditional Goal Details
→ Running Background
→ Goal-Based Preferences
→ Weekly Frequency
→ Running Days Selection
→ Start Date Selection
→ Plan Generation Screen
→ Plan Preview
→ Home
```

Race path:

```text
Goal Selection: Train for a Race
→ Race Name / Race Date / Race Distance
→ Running Background
→ Optional Goal Time
→ Weekly Frequency
→ Running Days Selection
→ Long Run Day Preference
→ Start Date Selection
→ Plan Generation
→ Plan Preview
→ Home
```

Habit path:

```text
Goal Selection: Build a Running Habit
→ Running Background
→ Habit Goal Selection
→ Optional Custom Goal
→ Weekly Frequency
→ Preferred Run Duration
→ Running Days Selection
→ Start Date Selection
→ Plan Generation
→ Plan Preview
→ Home
```

---

# 6. Frontend Page Requirements

## 6.1 Authentication / Welcome

Purpose:
- Allow users to enter the app with low friction.

UI elements:
- App logo/name placeholder.
- Calm introductory text.
- Continue with Google.
- Continue with Apple.
- Continue with Email.

Phase 1 behavior:
- Auth can use mock login or placeholder auth service.
- After mock login, continue to Intro Carousel or Onboarding.

## 6.2 Intro Carousel

3 pages:

Page 1 — Progress & Clarity
- Message: the app helps users clearly track running plans, progress, and milestones.

Page 2 — Low Pressure Running
- Message: running plans should fit real life without pressure or guilt.

Page 3 — Adaptive Planning
- Message: the app can adjust when life gets in the way.

UI behavior:
- Horizontal swipe.
- Continue button.
- Skip button.
- Progress indicators.

## 6.3 Goal Selection

Options:
- Build a running habit.
- Train for a race.

Behavior:
- Single selection.
- Continue disabled until selected.
- Routes to race details if race is selected.
- Routes to running background if habit is selected.

## 6.4 Race Details

Fields:
- Race name: optional.
- Race date: required, future date.
- Race distance: required.
- Unit: km/mi.

Supported distance examples:
- 5K.
- 10K.
- Half Marathon.
- Marathon.
- Custom.

Phase 1 behavior:
- Collect values in frontend state.
- No real training intelligence yet.

## 6.5 Running Background

Options:
- New to running.
- Used to run.
- Running regularly.

Behavior:
- Single selection.
- Continue disabled until selected.

## 6.6 Habit Goal Selection

Options:
- Run 5 km comfortably.
- Run 10 km nonstop.
- Run 5 km under 30 minutes.
- Create my own goal.

Behavior:
- Single selection.
- Custom goal unlocks custom goal screen.

## 6.7 Custom Goal Screen

Fields:
- Target distance.
- Goal preference:
  - Just finish comfortably.
  - Maintain a steady pace.
  - Finish under a time.
- Target time only if user chooses finish under a time.

Phase 1:
- Collect data only.
- Do not implement advanced plan logic.

## 6.8 Weekly Frequency

MVP options:
- 3 days per week.
- 4 days per week.

Do not show or implement 5/6 day plans in Phase 1.

## 6.9 Preferred Run Duration

Options/examples:
- 15 minutes.
- 20 minutes.
- 30 minutes.
- 45 minutes.
- 60 minutes.

Phase 1:
- Store in onboarding state.
- It can be included in preview request.
- It does not need to drive real plan generation yet.

## 6.10 Running Days Selection

Behavior:
- Multi-select weekdays.
- Number of selected days must match weekly frequency.
- Continue disabled until valid.

## 6.11 Long Run Day Preference

Shown for race flow.

Behavior:
- User selects one preferred long-run day.
- It should be one of selected running days if possible.

Phase 1:
- Store preference.
- Use it if seed schedule supports it; otherwise keep simple.

## 6.12 Start Date Selection

Options:
- Today.
- Next Monday.
- Custom date.

Behavior:
- Custom date opens date picker.
- Start date must be today or a future date.

## 6.13 Plan Generation Screen

Purpose:
- Transition from onboarding to preview.

Important Phase 1 decision:
- Do not implement real intelligent generation.
- Show a lightweight loading screen.
- Call `POST /api/v1/plans/generate-preview`.
- Backend returns a static/seed preview.

Suggested staged UI text:
- Understanding your goal.
- Checking your schedule.
- Building your first plan.
- Finalizing your preview.

These are UI messages only. They should not imply a real adaptive engine is implemented.

## 6.14 Plan Preview

Display:
- Plan name.
- Goal.
- Duration weeks.
- Days per week.
- Preferred run days.
- Weekly summary preview.

Primary CTA:
- Start Plan.

Action:
- Calls `POST /api/v1/plans/confirm`.
- On success route to Home.

## 6.15 Home Screen

Main sections:
- Header.
- Today’s Plan Card.
- Weekly Mini Calendar.
- Secondary Insight Widgets.
- Bottom Navigation.

Today’s Plan Card displays:
- Workout type.
- Distance.
- Duration.
- Optional pace range placeholder.
- Status.
- Actions.

Primary states:
- Planned Run State.
- Completed Run State.
- Rest Day State.
- Missed / Not Completed Run State.
- Pending Confirmation State.
- Adapted Plan State placeholder.
- No Active Plan State.
- Plan Completed State.

## 6.16 Planned Run State

Actions:
- Complete.
- Not Today.
- Tap card to open training day detail.

Complete action:
- Opens completion modal.
- Saves activity.
- Calls complete endpoint.
- Home updates to completed state.

Not Today action:
- Opens Not Today modal.
- User may select reason.
- User confirms with Got it.
- Home transitions to Missed / Not Completed State.

## 6.17 Completed Run State

Display:
- Completed indicator.
- Supportive message.
- Updated weekly calendar marker.
- Recovery tip widget.

Do not make the state overly celebratory or gamified.

## 6.18 Rest Day State

Display:
- Recovery-focused Today card.
- No Complete / Not Today buttons.
- Recovery tips.
- Rest should feel intentional, not empty.

## 6.19 Missed / Not Completed Run State

This state is important.

It appears when:
- User taps Not Today for today’s run and confirms.
- User marks a past pending confirmation as missed.

UX principles:
- The state is still missed/not completed.
- It must not feel punitive.
- It should communicate continuity, flexibility, and support.
- Calendar should show a missed/not completed indicator.
- The missed indicator should be visually muted, not aggressive.

Possible messages:
- “One missed run doesn’t define your progress.”
- “Your plan can continue from here.”
- “See you on your next run.”

Actions:
- View Missed Workout.
- Acknowledge Missed Run, if implemented locally.
- Navigate to Calendar.
- Navigate to Profile.

Phase 1 backend:
- Mark the day missed or skipped.
- Do not perform real adaptive mutation.

## 6.20 Calendar Screen

Display:
- Monthly grid.
- Workout indicators.
- Completed markers.
- Missed markers.
- Rest days.

Interaction:
- Tap a day.
- Open day detail modal/page.

Day detail variants:
- Future workout.
- Current day workout.
- Completed workout.
- Missed workout.
- Rest day.

Frontend decides visual indicators based on backend type/status. Backend should not return UI-specific icons.

## 6.21 Training Day Detail

Opened from:
- Home Today card.
- Calendar day tap.

Display:
- Workout title.
- Type.
- Date.
- Status.
- Planned distance.
- Planned duration.
- Intensity.
- Description.
- Allowed actions.

Allowed actions:
- Complete if current/planned.
- Not Today if current/planned.
- Close/back.

## 6.22 Completion Modal

Triggered by Complete.

Options:
- As planned.
- Shorter.
- Exceeded.

Phase 1:
- Store feedback if simple.
- Actual stats are optional.
- Complete endpoint can accept empty request or simple feedback.

## 6.23 Not Today Modal

Triggered by Not Today.

Reason chips:
- Need rest.
- No time.
- Feeling tired.
- Other.

Behavior:
- Reason is optional.
- Got it confirms.
- Result transitions to Missed / Not Completed State.
- Do not show real adaptive reschedule options in Phase 1.

## 6.24 Pending Confirmation Screen

Shown when backend indicates unresolved past planned workouts.

Single item version:
- Title: “Did you get this run in?”
- Buttons: Yes, I ran / No, I missed it.

Multiple item version:
- List cards.
- Each card has Completed / Missed choice.
- CTA: Update my plan.

Phase 1:
- Completed marks completed.
- Missed marks missed.
- No real adaptation.

## 6.25 Profile Screen

Main sections:
- Profile header.
- User statistics.
- Active plan card.
- Recent badges placeholder.
- Bottom navigation.

Actions:
- View Plan.
- Stop Plan.
- Settings.

## 6.26 Plan Details Screen

Display:
- Plan name.
- Goal type.
- Level/background.
- Duration.
- Days per week.
- Preferred run days.
- Long run day.
- Start date.
- Estimated end date.
- Workout types.

## 6.27 Stop Plan Modal

Triggered from Profile / Active Plan Card.

Optional reasons:
- Too hard.
- Injury.
- No time.
- Other.

Actions:
- Keep Going.
- Stop Plan.

Backend:
- Calls `POST /api/v1/plans/{planId}/cancel`.
- Backend status becomes `cancelled`.
- Frontend label can be “Stop Plan”.

## 6.28 No Active Plan State

Shown when no active plan exists.

Actions:
- Create New Plan.
- Explore App placeholder.
- Navigate to Profile.
- Navigate to Calendar empty state.

## 6.29 Plan Completed State

Basic Phase 1 only.

Trigger:
- If all training days are completed or the plan reaches completion criteria.

Display:
- Calm completion message.
- Start New Plan.
- View Plan Summary placeholder.

Do not build a full achievement system yet.

## 6.30 Settings Placeholder

Build a basic settings screen with placeholder sections:
- Active Plan Settings.
- Notification Settings.
- App Preferences.
- Account Settings.
- Legal & Support.

Phase 1:
- These can be mostly static.
- Do not build full edit behavior unless explicitly requested.

---

# 7. Backend API Contracts

Base path:

```text
/api/v1
```

## 7.1 Bootstrap

```http
GET /api/v1/me/bootstrap
```

Purpose:
- Decide where the app should route on startup.

Response:

```json
{
  "isAuthenticated": true,
  "hasProfile": true,
  "hasActivePlan": true,
  "hasPendingConfirmations": false,
  "nextScreen": "Home"
}
```

Possible `nextScreen` values:
- `Welcome`
- `Onboarding`
- `PlanSetup`
- `PendingConfirmation`
- `Home`
- `NoActivePlan`

## 7.2 Generate Plan Preview

```http
POST /api/v1/plans/generate-preview
```

Purpose:
- Create a preview before permanently creating a training plan.

Important Phase 1 decision:
- Do not run a real plan generation engine.
- Select a simple seed/static template.
- Return a valid preview.
- Store preview payload if implementing `plan_previews`.

Request:

```json
{
  "goalType": "habit",
  "goalDistance": "5k",
  "customDistanceKm": null,
  "targetTimeSeconds": null,
  "raceName": null,
  "raceDate": null,
  "unit": "km",
  "level": "beginner",
  "runningBackground": "new_to_running",
  "daysPerWeek": 3,
  "preferredRunDays": ["Tuesday", "Thursday", "Sunday"],
  "longRunDay": "Sunday",
  "preferredRunDurationMin": 30,
  "startDate": "2026-05-01"
}
```

Response:

```json
{
  "previewId": "uuid",
  "templateId": "habit_5k_beginner_3day_km_v1",
  "planName": "5K Beginner Plan",
  "durationWeeks": 8,
  "daysPerWeek": 3,
  "startDate": "2026-05-01",
  "estimatedEndDate": "2026-06-26",
  "weeklySummary": [
    {
      "weekNumber": 1,
      "totalDistanceKm": 7.5,
      "workoutCount": 3
    }
  ]
}
```

## 7.3 Confirm Plan

```http
POST /api/v1/plans/confirm
```

Purpose:
- Persist previewed plan.

Request:

```json
{
  "previewId": "uuid"
}
```

Backend behavior:
- Read preview.
- Create or update user profile/onboarding snapshot if needed.
- Create TrainingPlan.
- Create TrainingWeeks.
- Create TrainingDays.
- Write PlanGenerated event.

Response:

```json
{
  "planId": "uuid",
  "status": "active",
  "messageKey": "plan_generated"
}
```

## 7.4 Home

```http
GET /api/v1/plans/active/home
```

Purpose:
- Return everything needed for Home in one response.

Response:

```json
{
  "activePlan": {
    "planId": "uuid",
    "name": "5K Beginner Plan",
    "currentWeek": 1,
    "totalWeeks": 8,
    "progressPercent": 12,
    "status": "active"
  },
  "today": {
    "trainingDayId": "uuid",
    "date": "2026-05-01",
    "type": "easy",
    "status": "planned",
    "title": "Easy Run",
    "plannedDistanceKm": 2.5,
    "plannedDurationMin": 25,
    "intensity": "z2",
    "canMarkComplete": true,
    "canMarkNotToday": true
  },
  "currentWeek": [
    {
      "trainingDayId": "uuid",
      "date": "2026-05-01",
      "type": "easy",
      "status": "planned"
    }
  ],
  "pendingConfirmations": [],
  "homeTips": [
    {
      "tipKey": "easy_run_tip_01",
      "title": "Keep it comfortable",
      "message": "Today is about showing up, not pushing hard."
    }
  ],
  "adaptationCards": []
}
```

If no active plan:

```json
{
  "activePlan": null,
  "today": null,
  "currentWeek": [],
  "pendingConfirmations": [],
  "homeTips": [],
  "adaptationCards": []
}
```

## 7.5 Calendar

```http
GET /api/v1/plans/active/calendar?month=YYYY-MM
```

Purpose:
- Return flat monthly training day data.

Response:

```json
{
  "month": "2026-05",
  "days": [
    {
      "trainingDayId": "uuid-1",
      "date": "2026-05-01",
      "type": "easy",
      "status": "completed"
    },
    {
      "trainingDayId": "uuid-2",
      "date": "2026-05-03",
      "type": "long_run",
      "status": "planned"
    },
    {
      "trainingDayId": "uuid-3",
      "date": "2026-05-04",
      "type": "rest",
      "status": "planned"
    }
  ]
}
```

Frontend decides icons/colors/indicators.

## 7.6 Training Day Detail

```http
GET /api/v1/training-days/{trainingDayId}
```

Response:

```json
{
  "trainingDayId": "uuid",
  "date": "2026-05-01",
  "type": "easy",
  "status": "planned",
  "title": "Easy Run",
  "description": "Comfortable aerobic run.",
  "plannedDistanceKm": 2.5,
  "plannedDurationMin": 25,
  "intensity": "z2",
  "isRestDay": false,
  "canMarkComplete": true,
  "canMarkNotToday": true
}
```

## 7.7 Complete Training Day

```http
POST /api/v1/training-days/{trainingDayId}/complete
```

Request:

```json
{
  "result": "as_planned",
  "actualDistanceKm": null,
  "actualDurationMin": null,
  "userNote": null
}
```

Phase 1 backend behavior:
- Validate ownership/mock current user.
- Set TrainingDay.status = completed.
- Create WorkoutLog.
- Write WorkoutCompleted event.
- Do not call Adaptive Engine.
- Check if plan is now completed. If yes, set TrainingPlan.status = completed and write PlanCompleted event.

Response:

```json
{
  "trainingDayId": "uuid",
  "status": "completed",
  "planAdapted": false,
  "planCompleted": false,
  "popup": {
    "title": "Nice work!",
    "message": "Easy run completed.",
    "workoutSummary": {
      "type": "easy",
      "plannedDistanceKm": 2.5,
      "plannedDurationMin": 25
    },
    "recoveryTip": {
      "key": "easy_recovery_01",
      "text": "Take a few minutes to cool down and drink some water."
    }
  }
}
```

## 7.8 Not Today Decision Create

```http
POST /api/v1/training-days/{trainingDayId}/not-today-decisions
```

Purpose:
- Create a pending Not Today decision for today’s planned run.

Important UX decision:
- Not Today transitions the user to Missed / Not Completed Run State.
- It is still a missed/not completed state.
- It must not feel punitive.

Phase 1 backend behavior:
- Do not perform real reschedule/shorten/recovery logic.
- Do not generate a real adaptive recommendation.
- Create NotTodayDecision.
- Return a fixed supportive response.
- Keep response shape future-compatible.

Request:

```json
{
  "reason": "no_time"
}
```

Reason values:
- `need_rest`
- `no_time`
- `feeling_tired`
- `other`

Response:

```json
{
  "decisionId": "uuid",
  "trainingDayId": "uuid",
  "triggerSource": "not_today",
  "reason": "no_time",
  "resultingStatus": "missed",
  "action": "no_change",
  "planAdapted": false,
  "title": "No problem",
  "message": "Taking a rest today won’t define your progress.",
  "supportiveText": "Your plan will continue from here.",
  "affectedDays": []
}
```

## 7.9 Not Today Decision Confirm

```http
POST /api/v1/not-today-decisions/{decisionId}/confirm
```

Purpose:
- Finalize Not Today and update today’s training day.

Request:

```json
{
  "accepted": true
}
```

Phase 1 backend behavior:
- Set not_today_decision.status = confirmed.
- Set current TrainingDay.status = missed or skipped. Prefer `missed` if the UI calls the state Missed / Not Completed.
- Write NotTodayDecisionConfirmed event.
- Write WorkoutMissed or WorkoutSkippedBySystem event.
- Do not mutate future training days.
- Do not call real adaptive engine.

Response:

```json
{
  "status": "confirmed",
  "messageKey": "not_today_confirmed",
  "triggerSource": "not_today",
  "resultingStatus": "missed",
  "action": "no_change",
  "planAdapted": false,
  "adaptationSummary": null,
  "affectedDays": []
}
```

## 7.10 Pending Confirmations Read

```http
GET /api/v1/pending-confirmations
```

Purpose:
- Return past planned workouts that need completed/missed resolution.

Response:

```json
{
  "items": [
    {
      "trainingDayId": "uuid-1",
      "date": "2026-05-01",
      "type": "easy",
      "title": "Easy Run",
      "plannedDistanceKm": 2.5,
      "plannedDurationMin": 25,
      "status": "pending"
    }
  ],
  "maxVisibleItems": 3
}
```

## 7.11 Pending Confirmations Resolve

```http
POST /api/v1/pending-confirmations/resolve
```

Request:

```json
{
  "items": [
    {
      "trainingDayId": "uuid-1",
      "answer": "completed"
    },
    {
      "trainingDayId": "uuid-2",
      "answer": "missed"
    }
  ]
}
```

Phase 1 backend behavior:
- Process answers chronologically.
- completed → TrainingDay.status = completed.
- completed → WorkoutCompleted event.
- missed → TrainingDay.status = missed.
- missed → WorkoutMissed event.
- Do not run real adaptation.
- Do not mutate future training days.
- Return planAdapted = false.

Response:

```json
{
  "resolvedItems": [
    {
      "trainingDayId": "uuid-1",
      "status": "completed"
    },
    {
      "trainingDayId": "uuid-2",
      "status": "missed"
    }
  ],
  "triggerSource": "pending_confirmation",
  "planAdapted": false,
  "adaptationSummary": null
}
```

## 7.12 Profile Overview

```http
GET /api/v1/profile/overview
```

Response:

```json
{
  "user": {
    "name": "Fatma",
    "email": "fatma@example.com",
    "level": "beginner"
  },
  "activePlan": {
    "planId": "uuid",
    "name": "5K Beginner Plan",
    "goalType": "habit",
    "goalDistance": "5k",
    "currentWeek": 1,
    "totalWeeks": 8,
    "progressPercent": 12,
    "completedRuns": 1,
    "totalRuns": 24,
    "missedRuns": 0,
    "startDate": "2026-05-01",
    "estimatedEndDate": "2026-06-26",
    "status": "active"
  },
  "completedPlans": [],
  "badges": [],
  "cumulativeStats": {
    "totalRuns": 1,
    "totalDistanceKm": 2.5,
    "completedPlans": 0
  }
}
```

## 7.13 Active Plan Details

```http
GET /api/v1/plans/active/details
```

Response:

```json
{
  "planId": "uuid",
  "name": "5K Beginner Plan",
  "goalType": "habit",
  "goalDistance": "5k",
  "level": "beginner",
  "durationWeeks": 8,
  "daysPerWeek": 3,
  "preferredRunDays": ["Tuesday", "Thursday", "Sunday"],
  "longRunDay": "Sunday",
  "startDate": "2026-05-01",
  "estimatedEndDate": "2026-06-26",
  "workoutTypes": [
    {
      "type": "easy",
      "description": "Comfortable aerobic run."
    },
    {
      "type": "interval",
      "description": "Short faster efforts with recovery."
    },
    {
      "type": "long_run",
      "description": "Longer steady run for endurance."
    },
    {
      "type": "rest",
      "description": "Recovery day."
    }
  ]
}
```

## 7.14 Cancel / Stop Plan

```http
POST /api/v1/plans/{planId}/cancel
```

Frontend label:
- Stop Plan.

Backend status:
- cancelled.

Request:

```json
{
  "reason": "optional_user_reason"
}
```

Backend behavior:
- Set TrainingPlan.status = cancelled.
- Write PlanCancelled event.
- Do not delete historical data.

Response:

```json
{
  "planId": "uuid",
  "status": "cancelled",
  "messageKey": "plan_cancelled"
}
```

## 7.15 Settings Placeholder

Optional Phase 1 endpoints:

```http
GET /api/v1/settings
PATCH /api/v1/settings/preferences
```

If implemented, keep simple.

Settings response:

```json
{
  "unit": "km",
  "language": "en",
  "theme": "system",
  "notifications": {
    "reminderStyle": "balanced",
    "workoutRemindersEnabled": false,
    "eveningReminderEnabled": false,
    "reminderTime": null
  }
}
```

---

# 8. Domain Enums

Use consistent enum values across backend and frontend.

## 8.1 GoalType

```text
habit
race
```

## 8.2 GoalDistance

```text
five_k
ten_k
half_marathon
marathon
custom
```

## 8.3 UserLevel / RunningBackground

```text
beginner
returning
regular
```

or:

```text
new_to_running
used_to_run
running_regularly
```

Choose one naming convention and use it everywhere. Prefer API-friendly snake_case values.

## 8.4 TrainingPlanStatus

```text
active
completed
cancelled
```

Do not implement paused in Phase 1.

## 8.5 TrainingWeekType

```text
base
build
recovery
peak
taper
race_week
```

Phase 1 can mostly use `build` and `recovery` placeholders.

## 8.6 TrainingDayType

```text
easy
interval
tempo
long_run
rest
recovery_easy
```

## 8.7 TrainingDayStatus

```text
planned
completed
missed
skipped
pending
```

Recommendation for Phase 1:
- Not Today confirm can set status to `missed` to match Missed / Not Completed UI.
- If using `skipped`, ensure UI still maps it to Missed / Not Completed State.

## 8.8 NotTodayDecisionStatus

```text
pending
confirmed
expired
cancelled
```

## 8.9 AdaptationAction

```text
no_change
skipped
rescheduled
shortened
recovery_week
```

Phase 1 placeholder should usually return:

```text
no_change
```

## 8.10 TriggerSource

```text
not_today
pending_confirmation
system
manual_override
```

---

# 9. Database Entities

Create EF Core entities for Phase 1. Keep them simple but future-compatible.

## 9.1 UserProfile

Fields:
- `id`
- `userId`
- `name`
- `email`
- `unit`
- `runningBackground`
- `createdAt`
- `updatedAt`

## 9.2 PlanTemplate

Fields:
- `id`
- `version`
- `goalType`
- `goalDistance`
- `level`
- `daysPerWeek`
- `unit`
- `dataJson`
- `createdAt`
- `deprecatedAt`

Phase 1:
- Seed a small number of templates.
- Template JSON can be simple.

## 9.3 PlanPreview

Fields:
- `id`
- `userId`
- `templateId`
- `requestPayloadJson`
- `previewPayloadJson`
- `expiresAt`
- `createdAt`

## 9.4 TrainingPlan

Fields:
- `id`
- `userId`
- `templateId`
- `status`
- `goalType`
- `goalDistance`
- `goalDistanceKm`
- `level`
- `daysPerWeek`
- `unit`
- `raceName`
- `raceDate`
- `targetFinishTimeSeconds`
- `startedAt`
- `estimatedEndDate`
- `completedAt`
- `cancelledAt`
- `createdAt`

Rule:
- Only one active plan per user.

## 9.5 TrainingWeek

Fields:
- `id`
- `planId`
- `weekNumber`
- `weekType`
- `plannedVolumeKm`
- `actualVolumeKm`
- `isRecoveryWeek`
- `startDate`
- `createdAt`

## 9.6 TrainingDay

Fields:
- `id`
- `planId`
- `weekId`
- `date`
- `dayType`
- `status`
- `title`
- `description`
- `plannedDistanceKm`
- `plannedDurationMin`
- `plannedPaceMinKm`
- `intensity`
- `actualDistanceKm`
- `actualDurationMin`
- `isLongRun`
- `originalDate`
- `originalType`
- `canMarkComplete`
- `canMarkNotToday`
- `completedAt`
- `createdAt`
- `updatedAt`

## 9.7 WorkoutLog

Fields:
- `id`
- `userId`
- `planId`
- `trainingDayId`
- `result`
- `actualDistanceKm`
- `actualDurationMin`
- `userNote`
- `createdAt`

## 9.8 NotTodayDecision

Fields:
- `id`
- `userId`
- `planId`
- `trainingDayId`
- `reason`
- `status`
- `triggerSource`
- `action`
- `resultingStatus`
- `decisionPayloadJson`
- `createdAt`
- `confirmedAt`

## 9.9 PendingConfirmation

Fields:
- `id`
- `userId`
- `planId`
- `trainingDayId`
- `status`
- `createdAt`
- `resolvedAt`

## 9.10 PlanEvent

Fields:
- `id`
- `userId`
- `planId`
- `trainingDayId`
- `eventType`
- `payloadJson`
- `createdAt`

## 9.11 AdaptationEvent

Fields:
- `id`
- `userId`
- `planId`
- `eventType`
- `triggerSource`
- `triggeredByTrainingDayId`
- `action`
- `affectedDaysJson`
- `explanationKey`
- `createdAt`
- `dismissedAt`

Phase 1:
- Can be created but rarely used.
- Placeholder engine can write no adaptation events unless simple logging is desired.

## 9.12 DailyTipSet

Fields:
- `id`
- `tipKey`
- `title`
- `message`
- `workoutType`
- `level`
- `goalType`
- `language`
- `createdAt`

Phase 1:
- Seed a few tips.
- Return tips inside Home response.

## 9.13 NotificationPreference

Optional Phase 1.

Fields:
- `id`
- `userId`
- `reminderStyle`
- `workoutRemindersEnabled`
- `eveningReminderEnabled`
- `reminderTime`
- `createdAt`
- `updatedAt`

---

# 10. Seed Data

## 10.1 Seed Plan Templates

Create at least these seed templates:
- `habit_5k_beginner_3day_km_v1`
- `habit_5k_beginner_4day_km_v1`
- `race_5k_beginner_3day_km_v1`

Template format can be simple.

Example:

```json
{
  "templateId": "habit_5k_beginner_3day_km_v1",
  "version": 1,
  "goalType": "habit",
  "goalDistance": "five_k",
  "level": "beginner",
  "daysPerWeek": 3,
  "unit": "km",
  "weeks": [
    {
      "weekNumber": 1,
      "weekType": "build",
      "days": [
        {
          "slotIndex": 1,
          "dayType": "easy",
          "distanceKm": 2.0,
          "durationMin": 20,
          "intensity": "z2"
        },
        {
          "slotIndex": 2,
          "dayType": "easy",
          "distanceKm": 2.5,
          "durationMin": 25,
          "intensity": "z2"
        },
        {
          "slotIndex": 3,
          "dayType": "long_run",
          "distanceKm": 3.0,
          "durationMin": 30,
          "intensity": "z2"
        }
      ]
    }
  ]
}
```

## 10.2 Seed Tips

Seed simple home tips:
- Easy run tip.
- Long run tip.
- Rest day tip.
- Missed day supportive tip.
- Completed run recovery tip.

---

# 11. Backend Service Structure

Suggested services:

- `BootstrapService`
- `PlanPreviewService`
- `PlanConfirmationService`
- `HomeQueryService`
- `CalendarQueryService`
- `TrainingDayService`
- `WorkoutCompletionService`
- `NotTodayService`
- `PendingConfirmationService`
- `ProfileService`
- `PlanManagementService`
- `PlaceholderAdaptationEngine`

Important:
- Keep read services separate from mutation services if possible.
- Keep controllers thin.
- Put business logic in application services.
- Use DTOs for all API requests/responses.

---

# 12. Flutter Project Structure

Suggested structure:

```text
lib/
  main.dart
  app.dart
  core/
    config/
    routing/
    theme/
    network/
    widgets/
    utils/
  features/
    auth/
      presentation/
      data/
    onboarding/
      presentation/
      application/
      data/
    plan/
      presentation/
      application/
      data/
    home/
      presentation/
      application/
      data/
    calendar/
      presentation/
      application/
      data/
    training_day/
      presentation/
      application/
      data/
    pending_confirmation/
      presentation/
      application/
      data/
    profile/
      presentation/
      application/
      data/
    settings/
      presentation/
      data/
```

## 12.1 Frontend Data Layer

Create:
- `ApiClient`
- DTO classes matching backend contracts.
- Repositories per feature.
- Loading/error states.
- Simple retry or error message handling.

## 12.2 Frontend State Management

State should support:
- Bootstrap loading state.
- Onboarding local state.
- Active plan/home state.
- Calendar month state.
- Training day detail state.
- Mutation success/failure states.

## 12.3 UI Components

Reusable components:
- Primary button.
- Secondary button.
- Selectable card.
- Workout card.
- Today plan card.
- Week mini calendar item.
- Month calendar cell.
- Bottom sheet/modal wrapper.
- Empty state component.
- Loading/generation screen component.

---

# 13. UI Tone and Design Rules

Visual style:
- Calm.
- Minimal.
- Mature.
- Warm.
- Supportive.
- Doodle-inspired but not childish.

Use placeholder mascot assets for Phase 1.

Do not block development on final illustrations.

Language examples:
- “No problem.”
- “Your plan can continue from here.”
- “Recovery is part of progress.”
- “Nice work today.”
- “One missed run doesn’t define your progress.”

Avoid:
- “You failed.”
- “Streak lost.”
- “You are behind.”
- “Bad job.”
- “You must make up this workout.”

---

# 14. Important Flow Decisions

## 14.1 Complete

Complete is fast and simple.

Complete should:
- Mark the workout completed.
- Create workout log.
- Update Home/Calendar.
- Not call adaptive engine.

## 14.2 Not Today

Not Today means:
- The user is intentionally not completing today’s run.
- The app transitions to Missed / Not Completed State.
- Calendar gets a missed/not completed marker.
- The UX stays supportive and non-punitive.

Phase 1:
- No real reschedule.
- No real adaptive mutation.
- Just mark day missed/skipped after confirm.

## 14.3 Pending Confirmation

Pending Confirmation means:
- A past planned workout was not resolved.
- User must say completed or missed.

Phase 1:
- Completed marks completed.
- Missed marks missed.
- No real adaptation.

## 14.4 Plan Generation

Plan generation in Phase 1 is not intelligent.

It should:
- Use a seed template.
- Map template slots to selected run days if simple.
- Persist TrainingPlan/Weeks/Days after confirm.
- Keep the interface ready for a real generator later.

---

# 15. Acceptance Criteria for Phase 1

Phase 1 is successful when:

1. App opens and routes through bootstrap.
2. User can go through onboarding.
3. User can generate a preview using seed/static template data.
4. User can confirm a plan.
5. Backend creates plan/weeks/days.
6. Home reads active plan data from backend.
7. Calendar reads plan days from backend.
8. User can open training day details.
9. User can mark a workout complete.
10. User can press Not Today and confirm it.
11. Not Today marks today as missed/not completed.
12. Pending Confirmation can resolve past runs as completed or missed.
13. Profile shows active plan summary.
14. User can stop/cancel active plan.
15. No active plan state works.
16. Basic plan completed state exists or is at least structurally supported.
17. Adaptive Engine is represented as placeholder only.
18. No future-scope features are accidentally implemented.

---

# 16. Development Order for Antigravity

Build in this exact order.

## Step 1 — Repository and Project Setup

Create:
- Flutter app.
- .NET Web API backend.
- PostgreSQL connection.
- Swagger.
- Basic README.

## Step 2 — Backend Domain and DB

Create:
- Entities.
- Enums.
- DbContext.
- Migrations.
- Seed templates.
- Seed tips.

## Step 3 — Backend Controllers and DTOs

Implement endpoints:
- Bootstrap.
- Plan preview.
- Plan confirm.
- Home.
- Calendar.
- Training day detail.
- Complete.
- Not Today create/confirm.
- Pending confirmations read/resolve.
- Profile overview.
- Plan details.
- Cancel plan.

## Step 4 — Placeholder Adaptation Engine

Create:
- Interface.
- Placeholder implementation.
- DTOs/enums.

Do not implement real logic.

## Step 5 — Flutter Navigation Skeleton

Before building page shells, apply the Design Reference Policy: create the shared Flutter design system from the provided screen references and do not use generic Material Design screens.

Create all pages and navigation:
- Auth.
- Intro.
- Onboarding.
- Plan preview.
- Home.
- Calendar.
- Profile.
- Settings placeholder.

## Step 6 — Flutter API Integration

Connect:
- Bootstrap.
- Generate preview.
- Confirm plan.
- Home.
- Calendar.
- Training day detail.
- Complete.
- Not Today.
- Pending confirmation.
- Profile.
- Cancel plan.

## Step 7 — State and UI Polishing

Make sure:
- Loading states work.
- Empty states work.
- Error states are present.
- Navigation does not break.
- Home updates after mutations.
- Calendar updates after mutations.

## Step 8 — Documentation

Add README sections:
- How to run backend.
- How to run Flutter app.
- Environment variables.
- Known placeholders.
- Future work.

---

# 17. Explicit Do Not Implement List

Do not implement these unless explicitly requested after Phase 1:

```text
Real Adaptive Engine
Real plan optimization
Real rescheduling logic
Recovery week logic
Missed score calculation
Strava integration
Apple Health integration
Garmin integration
Completion candidates
Push notification scheduler
Redis
Subscription
Founder access
Pause/resume
Advanced Settings mutation
Admin panel
AI recommendations
Weather-aware suggestions
Social/community features
Dark mode
Full localization
Production account deletion
```

---

# 18. Final Instruction to Antigravity

Build the connected MVP skeleton first.

Do not attempt to make the product fully intelligent in this phase.

The most important goal is to create a clean, maintainable foundation where all pages, endpoints, entities, and navigation paths exist and work together with simple placeholder behavior.

After this skeleton works, each page and each backend flow will be improved one by one.
