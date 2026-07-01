import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_text_styles.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../core/widgets/app_button.dart';
import '../../../core/routing/app_router.dart';
import '../data/auth_providers.dart';
import '../data/auth_exception.dart';
import '../data/firebase_auth_repository.dart';
import '../../../core/network/bootstrap_provider.dart';

class AuthWelcomePage extends ConsumerStatefulWidget {
  const AuthWelcomePage({super.key});

  @override
  ConsumerState<AuthWelcomePage> createState() => _AuthWelcomePageState();
}

class _AuthWelcomePageState extends ConsumerState<AuthWelcomePage> {
  bool _isGoogleLoading = false;
  bool _isAppleLoading = false;

  Future<void> _onGoogleSignIn() async {
    setState(() => _isGoogleLoading = true);
    try {
      final repo = ref.read(firebaseAuthRepositoryProvider);
      await repo.signInWithGoogle();
      final bootstrap = await ref.refresh(bootstrapDataProvider.future);
      if (mounted) {
        context.go(AppRoutes.routeForNextScreen(bootstrap.nextScreen));
      }
    } catch (e) {
      // User tapped "Cancel" in the Google account picker — exit silently.
      if (e is AuthException && e.isCancelled) return;
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(e.toString()),
            behavior: SnackBarBehavior.floating,
          ),
        );
      }
    } finally {
      if (mounted) setState(() => _isGoogleLoading = false);
    }
  }

  Future<void> _onAppleSignIn() async {
    setState(() => _isAppleLoading = true);
    try {
      final repo = ref.read(firebaseAuthRepositoryProvider);
      await repo.signInWithApple();
      final bootstrap = await ref.refresh(bootstrapDataProvider.future);
      if (mounted) {
        context.go(AppRoutes.routeForNextScreen(bootstrap.nextScreen));
      }
    } catch (e) {
      // User dismissed the Apple sign-in sheet — exit silently.
      if (e is AuthException && e.isCancelled) return;
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(e.toString()),
            behavior: SnackBarBehavior.floating,
          ),
        );
      }
    } finally {
      if (mounted) setState(() => _isAppleLoading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final isSocialLoading = _isGoogleLoading || _isAppleLoading;

    return Scaffold(
      backgroundColor: AppColors.surface,
      body: SafeArea(
        child: CustomScrollView(
          slivers: [
            SliverFillRemaining(
              hasScrollBody: false,
              child: Padding(
                padding: const EdgeInsets.symmetric(horizontal: AppSpacing.lg, vertical: AppSpacing.xl),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.center,
                  children: [
                    const Spacer(flex: 3),

                    // Logo container: white card with shadow and blue teardrop leaf logo
                    Container(
                      width: 90,
                      height: 90,
                      decoration: BoxDecoration(
                        color: Colors.white,
                        borderRadius: BorderRadius.circular(24),
                        boxShadow: [
                          BoxShadow(
                            color: Colors.black.withOpacity(0.06),
                            blurRadius: 16,
                            offset: const Offset(0, 8),
                          ),
                        ],
                      ),
                      child: Center(
                        child: Stack(
                          alignment: Alignment.center,
                          children: [
                            // Teardrop / Leaf outline
                            Transform.rotate(
                              angle: -0.785398,
                              child: Container(
                                width: 40,
                                height: 40,
                                decoration: BoxDecoration(
                                  color: Colors.transparent,
                                  border: Border.all(
                                    color: AppColors.primary,
                                    width: 3.5,
                                  ),
                                  borderRadius: const BorderRadius.only(
                                    topLeft: Radius.circular(20),
                                    bottomLeft: Radius.circular(20),
                                    bottomRight: Radius.circular(20),
                                  ),
                                ),
                              ),
                            ),
                            // Inner smaller filled teardrop / leaf seed
                            Transform.rotate(
                              angle: -0.785398,
                              child: Container(
                                width: 16,
                                height: 16,
                                decoration: const BoxDecoration(
                                  color: AppColors.primary,
                                  borderRadius: BorderRadius.only(
                                    topLeft: Radius.circular(8),
                                    bottomLeft: Radius.circular(8),
                                    bottomRight: Radius.circular(8),
                                  ),
                                ),
                              ),
                            ),
                          ],
                        ),
                      ),
                    ),
                    const SizedBox(height: AppSpacing.xl),

                    // Title
                    Text(
                      'Your running\nplan, made simple.',
                      textAlign: TextAlign.center,
                      style: AppTextStyles.h1.copyWith(
                        fontSize: 32,
                        fontWeight: FontWeight.w800,
                        height: 1.2,
                        letterSpacing: -0.8,
                      ),
                    ),
                    const SizedBox(height: AppSpacing.md),

                    // Subtitle
                    Text(
                      'Personalized plans that adapt\nto you and your progress.',
                      textAlign: TextAlign.center,
                      style: AppTextStyles.bodyLarge.copyWith(
                        color: AppColors.textSecondary,
                        height: 1.4,
                      ),
                    ),

                    const Spacer(flex: 3),

                    // Primary Actions
                    AppPrimaryButton(
                      label: 'Sign up',
                      onPressed: isSocialLoading ? null : () => context.push(AppRoutes.signUp),
                    ),
                    const SizedBox(height: AppSpacing.md),

                    AppSecondaryButton(
                      label: 'Sign in',
                      onPressed: isSocialLoading ? null : () => context.push(AppRoutes.signIn),
                    ),
                    const SizedBox(height: AppSpacing.xl),

                    // Social logins divider
                    Row(
                      children: [
                        const Expanded(child: Divider(color: AppColors.border)),
                        Padding(
                          padding: const EdgeInsets.symmetric(horizontal: AppSpacing.md),
                          child: Text(
                            'or continue with',
                            style: AppTextStyles.bodySmall.copyWith(
                              color: AppColors.textSecondary,
                              fontSize: 12,
                            ),
                          ),
                        ),
                        const Expanded(child: Divider(color: AppColors.border)),
                      ],
                    ),
                    const SizedBox(height: AppSpacing.lg),

                    // Social login buttons.
                    // Apple Sign-In is only available on iOS/macOS — the button
                    // is hidden on Android to avoid offering an unsupported flow.
                    _SocialButtons(
                      isGoogleLoading: _isGoogleLoading,
                      isAppleLoading: _isAppleLoading,
                      isSocialLoading: isSocialLoading,
                      onGoogle: _onGoogleSignIn,
                      onApple: _onAppleSignIn,
                      showApple: FirebaseAuthRepository.isAppleSignInSupported,
                    ),
                    const SizedBox(height: AppSpacing.xl),

                    // Footer
                    Text(
                      'By continuing, you agree to our',
                      textAlign: TextAlign.center,
                      style: AppTextStyles.bodySmall.copyWith(color: AppColors.textSecondary, fontSize: 11),
                    ),
                    Row(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        GestureDetector(
                          onTap: () {},
                          child: Text(
                            'Terms of Service',
                            style: AppTextStyles.bodySmall.copyWith(
                              color: AppColors.primary,
                              fontWeight: FontWeight.w600,
                              fontSize: 11,
                              decoration: TextDecoration.underline,
                              decorationColor: AppColors.primary,
                            ),
                          ),
                        ),
                        Text(
                          ' and ',
                          style: AppTextStyles.bodySmall.copyWith(color: AppColors.textSecondary, fontSize: 11),
                        ),
                        GestureDetector(
                          onTap: () {},
                          child: Text(
                            'Privacy Policy',
                            style: AppTextStyles.bodySmall.copyWith(
                              color: AppColors.primary,
                              fontWeight: FontWeight.w600,
                              fontSize: 11,
                              decoration: TextDecoration.underline,
                              decorationColor: AppColors.primary,
                            ),
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: AppSpacing.sm),
                  ],
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Social buttons row — adapts layout based on whether Apple is supported.
// ─────────────────────────────────────────────────────────────────────────────

class _SocialButtons extends StatelessWidget {
  const _SocialButtons({
    required this.isGoogleLoading,
    required this.isAppleLoading,
    required this.isSocialLoading,
    required this.onGoogle,
    required this.onApple,
    required this.showApple,
  });

  final bool isGoogleLoading;
  final bool isAppleLoading;
  final bool isSocialLoading;
  final VoidCallback onGoogle;
  final VoidCallback onApple;
  final bool showApple;

  @override
  Widget build(BuildContext context) {
    final googleBtn = _socialButton(
      onPressed: isSocialLoading ? null : onGoogle,
      isLoading: isGoogleLoading,
      icon: Icons.g_mobiledata_rounded,
      iconSize: 24,
      label: 'Google',
    );

    if (!showApple) {
      // Android: only Google, full width.
      return googleBtn;
    }

    // iOS/macOS: two-column row with both providers.
    return Row(
      children: [
        Expanded(child: googleBtn),
        const SizedBox(width: AppSpacing.md),
        Expanded(
          child: _socialButton(
            onPressed: isSocialLoading ? null : onApple,
            isLoading: isAppleLoading,
            icon: Icons.apple,
            iconSize: 20,
            label: 'Apple',
          ),
        ),
      ],
    );
  }

  Widget _socialButton({
    required VoidCallback? onPressed,
    required bool isLoading,
    required IconData icon,
    required double iconSize,
    required String label,
  }) {
    return OutlinedButton.icon(
      onPressed: onPressed,
      icon: isLoading
          ? const SizedBox(
              width: 18,
              height: 18,
              child: CircularProgressIndicator(
                strokeWidth: 2,
                color: AppColors.primary,
              ),
            )
          : Icon(icon, size: iconSize, color: AppColors.textPrimary),
      label: Text(label,
          style: const TextStyle(fontWeight: FontWeight.bold)),
      style: OutlinedButton.styleFrom(
        side: const BorderSide(color: AppColors.border),
        foregroundColor: AppColors.textPrimary,
        padding: const EdgeInsets.symmetric(vertical: 12),
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(16),
        ),
      ),
    );
  }
}
