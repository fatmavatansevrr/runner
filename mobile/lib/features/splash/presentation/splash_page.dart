import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:shared_preferences/shared_preferences.dart';
import '../../../core/network/bootstrap_provider.dart';
import '../../../core/routing/app_router.dart';
import '../../../core/theme/app_colors.dart';

/// First screen on every app launch. Shows briefly, then routes to the
/// intro carousel, auth entry, plan-generation questions, or home —
/// whichever is appropriate for the current user state.
class SplashPage extends ConsumerStatefulWidget {
  const SplashPage({super.key});

  @override
  ConsumerState<SplashPage> createState() => _SplashPageState();
}

class _SplashPageState extends ConsumerState<SplashPage> {
  @override
  void initState() {
    super.initState();
    _resolveNextRoute();
  }

  Future<void> _resolveNextRoute() async {
    await Future.delayed(const Duration(seconds: 2));
    if (!mounted) return;

    final prefs = await SharedPreferences.getInstance();
    final hasSeenIntro = prefs.getBool('hasSeenWelcomeCarousel') ?? false;
    if (!mounted) return;

    if (!hasSeenIntro) {
      context.go(AppRoutes.introCarousel);
      return;
    }

    try {
      final bootstrap = await ref.read(bootstrapDataProvider.future);
      if (!mounted) return;
      context.go(AppRoutes.routeForNextScreen(bootstrap.nextScreen));
    } catch (_) {
      if (mounted) context.go(AppRoutes.authEntry);
    }
  }

  @override
  Widget build(BuildContext context) {
    return const Scaffold(
      backgroundColor: AppColors.surface,
      body: Center(
        child: CircularProgressIndicator(color: AppColors.primary),
      ),
    );
  }
}
