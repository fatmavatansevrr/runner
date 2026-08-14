import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../core/network/long_horizon_dtos.dart';
import '../../plan/data/long_horizon_provider.dart';
import '../data/home_provider.dart';
import 'home_page.dart';
import 'long_horizon_home_page.dart';

/// Route-level dispatcher for `AppRoutes.home`. Decides which Home screen
/// to render based on the backend's own `schedule_strategy` field from
/// `GET /plans/active/home` — never inferred any other way. The existing
/// static [HomePage] is completely unmodified; this widget only chooses
/// between it and [LongHorizonHomePage].
class ActiveHomeDispatcherPage extends ConsumerWidget {
  const ActiveHomeDispatcherPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    // TEST SHORTCUT bypass (see useMockHomeDataProvider in home_provider.dart):
    // mock data has no real backend strategy to check, so it always renders
    // the static Home screen it was built for.
    if (ref.watch(useMockHomeDataProvider)) {
      return const HomePage();
    }

    final asyncResult = ref.watch(activeHomeResultProvider);
    return asyncResult.when(
      loading: () => const Scaffold(body: Center(child: CircularProgressIndicator())),
      error: (err, _) {
        // Falls through to the static HomePage's own error handling (it
        // has its own retry UI wired to homeDataProvider) rather than
        // duplicating an error screen here.
        return const HomePage();
      },
      data: (result) {
        if (result.strategy == PlanScheduleStrategy.rollingLongHorizon) {
          return const LongHorizonHomePage();
        }
        return const HomePage();
      },
    );
  }
}
