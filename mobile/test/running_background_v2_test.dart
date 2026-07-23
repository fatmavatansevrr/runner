import 'package:flutter_test/flutter_test.dart';
import 'package:antigravity_app/core/models/running_background.dart';
import 'package:antigravity_app/core/models/recent_race_result.dart';
import 'package:antigravity_app/core/network/dtos.dart';

void main() {
  group('RunningBackground', () {
    test('wireValue matches backend canonical snake_case contract', () {
      expect(RunningBackground.beginner.wireValue, 'beginner');
      expect(RunningBackground.intermediate.wireValue, 'intermediate');
      expect(RunningBackground.advanced.wireValue, 'advanced');
      expect(RunningBackground.experienced.wireValue, 'experienced');
    });

    test('parse round-trips every canonical wire value', () {
      for (final level in RunningBackground.values) {
        expect(RunningBackground.parse(level.wireValue), level);
      }
    });

    test('parse rejects the removed legacy aliases — V2.1 has no frontend legacy compatibility', () {
      // Running Background V2.1: legacy aliases were removed from the
      // active contract entirely. Unlike the backend (which retains a
      // narrowly-scoped historical-compat reader for pre-existing persisted
      // server-side data), the frontend has no equivalent legacy local
      // store to stay compatible with, so parse() rejects these outright.
      expect(() => RunningBackground.parse('new_to_running'), throwsFormatException);
      expect(() => RunningBackground.parse('used_to_run'), throwsFormatException);
      expect(() => RunningBackground.parse('running_regularly'), throwsFormatException);
      expect(RunningBackground.tryParse('new_to_running'), isNull);
      expect(RunningBackground.tryParse('used_to_run'), isNull);
      expect(RunningBackground.tryParse('running_regularly'), isNull);
    });

    test('parse throws FormatException for an unknown value', () {
      expect(() => RunningBackground.parse('sprinter'), throwsFormatException);
    });

    test('tryParse returns null instead of throwing, and null for null input', () {
      expect(RunningBackground.tryParse('sprinter'), isNull);
      expect(RunningBackground.tryParse(null), isNull);
      expect(RunningBackground.tryParse('advanced'), RunningBackground.advanced);
    });

    test('only beginner skips the runner-background-details screen', () {
      expect(RunningBackground.beginner.skipsRunnerBackgroundDetails, isTrue);
      expect(RunningBackground.intermediate.skipsRunnerBackgroundDetails, isFalse);
      expect(RunningBackground.advanced.skipsRunnerBackgroundDetails, isFalse);
      expect(RunningBackground.experienced.skipsRunnerBackgroundDetails, isFalse);
    });

    test('label/description match the approved §2 copy exactly', () {
      expect(RunningBackground.beginner.description,
          "I'm new to running or getting back into it.");
      expect(RunningBackground.intermediate.description,
          'I run regularly and can comfortably complete 5–10 km.');
      expect(RunningBackground.advanced.description,
          'I train consistently and regularly run 10 km or more.');
      expect(RunningBackground.experienced.description,
          'I have a strong running base and regularly train for longer distances.');
    });
  });

  group('RecentRaceResult', () {
    test('summary formats preset distances with the conventional short label', () {
      final result = RecentRaceResult(
        distanceKm: 10.0,
        finishTimeSeconds: 58 * 60 + 30,
        raceDate: DateTime(2026, 6, 14),
      );
      expect(result.summary(useKm: true), '10K · 58:30 · 14 Jun 2026');
    });

    test('summary falls back to a raw numeric distance for non-preset values', () {
      final result = RecentRaceResult(
        distanceKm: 15.0,
        finishTimeSeconds: 70 * 60 + 5,
        raceDate: DateTime(2026, 1, 2),
      );
      expect(result.summary(useKm: true), '15.0km · 70:05 · 2 Jan 2026');
    });

    test('summary converts to miles when useKm is false', () {
      final result = RecentRaceResult(
        distanceKm: 5.0,
        finishTimeSeconds: 25 * 60,
        raceDate: DateTime(2026, 3, 1),
      );
      // 5km ≈ 3.1mi, not a recognized km preset once converted to miles.
      expect(result.summary(useKm: false), contains('mi'));
    });
  });

  group('GenerateRacePlanPreviewRequestDto — recent-running field suppression', () {
    Map<String, dynamic> jsonFor({
      double? weeklyVolumeKm,
      double? longestRunKm,
    }) {
      return GenerateRacePlanPreviewRequestDto(
        goalDistance: 'ten_k',
        level: RunningBackground.intermediate.wireValue,
        daysPerWeek: 4,
        unit: 'km',
        startDate: '2026-07-20',
        preferredDays: const ['mon', 'wed', 'fri', 'sun'],
        longRunDay: 'sun',
        raceDate: '2026-10-12',
        targetFinishTimeSeconds: 3480,
        targetFinishTimeSource: TargetFinishTimeSourceWire.productAverage,
        recentWeeklyVolumeKm: weeklyVolumeKm,
        recentLongestRunKm: longestRunKm,
      ).toJson();
    }

    test('omits recent-running keys entirely when all values are null', () {
      final json = jsonFor();
      expect(json.containsKey('recent_weekly_volume_km'), isFalse);
      expect(json.containsKey('recent_longest_run_km'), isFalse);
      // recent_race is always present as an explicit key, even when absent.
      expect(json['recent_race'], isNull);
    });

    test('an explicit 0 is sent, distinct from an omitted/null value', () {
      final json = jsonFor(weeklyVolumeKm: 0, longestRunKm: 0);
      expect(json['recent_weekly_volume_km'], 0);
      expect(json['recent_longest_run_km'], 0);
    });
  });
}
