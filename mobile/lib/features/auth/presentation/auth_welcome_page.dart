import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_text_styles.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../core/widgets/app_button.dart';
import '../../../core/routing/app_router.dart';
import '../data/auth_provider.dart';
import '../../../core/network/bootstrap_provider.dart';

class AuthWelcomePage extends ConsumerWidget {
  const AuthWelcomePage({super.key});

  void _onLoginSuccess(BuildContext context, WidgetRef ref) async {
    ref.read(authProvider.notifier).loginMock();
    final bootstrap = await ref.refresh(bootstrapDataProvider.future);
    if (context.mounted) {
      if (bootstrap.nextScreen == 'Home') {
        context.go(AppRoutes.home);
      } else if (bootstrap.nextScreen == 'PendingConfirmation') {
        context.go(AppRoutes.pendingConfirmation);
      } else {
        context.go(AppRoutes.introCarousel);
      }
    }
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return Scaffold(
      backgroundColor: AppColors.background,
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: AppSpacing.lg, vertical: AppSpacing.xl),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.center,
            children: [
              const Spacer(),

              // Logo / Icon placeholder
              Container(
                width: 90,
                height: 90,
                decoration: const BoxDecoration(
                  color: AppColors.primaryLight,
                  shape: BoxShape.circle,
                ),
                child: const Center(
                  child: Icon(
                    Icons.directions_run_rounded,
                    size: 48,
                    color: AppColors.primary,
                  ),
                ),
              ),
              const SizedBox(height: AppSpacing.lg),

              // Title
              Text(
                'antigravity',
                style: AppTextStyles.displayLarge.copyWith(
                  color: AppColors.primary,
                  letterSpacing: -1,
                ),
              ),
              const SizedBox(height: AppSpacing.xs),

              // Subtitle
              Text(
                'A gentle adaptive running planner\nthat adjusts with real life.',
                textAlign: TextAlign.center,
                style: AppTextStyles.bodyLarge.copyWith(
                  color: AppColors.textSecondary,
                  height: 1.4,
                ),
              ),

              const Spacer(flex: 2),

              // Primary Actions
              AppPrimaryButton(
                label: 'Get Started',
                onPressed: () => context.push(AppRoutes.signUp),
              ),
              const SizedBox(height: AppSpacing.md),

              AppSecondaryButton(
                label: 'I already have an account',
                onPressed: () => context.push(AppRoutes.signIn),
              ),
              const SizedBox(height: AppSpacing.xl),

              // Social logins
              Row(
                children: [
                  const Expanded(child: Divider(color: AppColors.border)),
                  Padding(
                    padding: const EdgeInsets.symmetric(horizontal: AppSpacing.md),
                    child: Text('OR JOIN WITH', style: AppTextStyles.label.copyWith(fontSize: 11)),
                  ),
                  const Expanded(child: Divider(color: AppColors.border)),
                ],
              ),
              const SizedBox(height: AppSpacing.lg),

              Row(
                children: [
                  Expanded(
                    child: OutlinedButton.icon(
                      onPressed: () => _onLoginSuccess(context, ref),
                      icon: const Icon(Icons.g_mobiledata_rounded, size: 24, color: AppColors.textPrimary),
                      label: const Text('Google'),
                      style: OutlinedButton.styleFrom(
                        side: const BorderSide(color: AppColors.border),
                        foregroundColor: AppColors.textPrimary,
                        padding: const EdgeInsets.symmetric(vertical: 12),
                      ),
                    ),
                  ),
                  const SizedBox(width: AppSpacing.md),
                  Expanded(
                    child: OutlinedButton.icon(
                      onPressed: () => _onLoginSuccess(context, ref),
                      icon: const Icon(Icons.apple, size: 20, color: AppColors.textPrimary),
                      label: const Text('Apple'),
                      style: OutlinedButton.styleFrom(
                        side: const BorderSide(color: AppColors.border),
                        foregroundColor: AppColors.textPrimary,
                        padding: const EdgeInsets.symmetric(vertical: 12),
                      ),
                    ),
                  ),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }
}
