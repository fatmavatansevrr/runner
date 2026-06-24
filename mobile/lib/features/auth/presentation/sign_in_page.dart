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

class SignInPage extends ConsumerStatefulWidget {
  const SignInPage({super.key});

  @override
  ConsumerState<SignInPage> createState() => _SignInPageState();
}

class _SignInPageState extends ConsumerState<SignInPage> {
  final _emailController = TextEditingController();
  final _passwordController = TextEditingController();
  bool _isLoading = false;

  @override
  void dispose() {
    _emailController.dispose();
    _passwordController.dispose();
    super.dispose();
  }

  void _onSignIn() async {
    setState(() => _isLoading = true);
    ref.read(authProvider.notifier).loginMock();
    try {
      final bootstrap = await ref.refresh(bootstrapDataProvider.future);
      if (mounted) {
        if (bootstrap.nextScreen == 'Home') {
          context.go(AppRoutes.home);
        } else if (bootstrap.nextScreen == 'PendingConfirmation') {
          context.go(AppRoutes.pendingConfirmation);
        } else {
          context.go(AppRoutes.introCarousel);
        }
      }
    } catch (_) {
      if (mounted) {
        context.go(AppRoutes.introCarousel);
      }
    } finally {
      if (mounted) {
        setState(() => _isLoading = false);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.surface,
      appBar: AppBar(
        backgroundColor: Colors.transparent,
        elevation: 0,
        leading: IconButton(
          icon: const Icon(Icons.arrow_back_ios_new_rounded, color: AppColors.textPrimary),
          onPressed: () => context.pop(),
        ),
      ),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(AppSpacing.lg),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text('Welcome back', style: AppTextStyles.h1),
              const SizedBox(height: AppSpacing.xs),
              Text(
                'Sign in to access your custom running plan.',
                style: AppTextStyles.bodyLarge.copyWith(color: AppColors.textSecondary),
              ),
              const SizedBox(height: AppSpacing.xl),

              // Email field
              TextField(
                controller: _emailController,
                keyboardType: TextInputType.emailAddress,
                decoration: const InputDecoration(
                  labelText: 'Email',
                  hintText: 'Enter your email address',
                  border: OutlineInputBorder(),
                ),
              ),
              const SizedBox(height: AppSpacing.md),

              // Password field
              TextField(
                controller: _passwordController,
                obscureText: true,
                decoration: const InputDecoration(
                  labelText: 'Password',
                  hintText: 'Enter your password',
                  border: OutlineInputBorder(),
                  suffixIcon: Icon(Icons.visibility_outlined, color: AppColors.textMuted),
                ),
              ),
              const SizedBox(height: AppSpacing.xl),

              if (_isLoading)
                const Center(child: CircularProgressIndicator(color: AppColors.primary))
              else
                AppPrimaryButton(
                  label: 'Sign In',
                  onPressed: _onSignIn,
                ),
              const SizedBox(height: AppSpacing.lg),

              Center(
                child: TextButton(
                  onPressed: () => context.pushReplacement(AppRoutes.signUp),
                  child: RichText(
                    text: TextSpan(
                      text: 'New to antigravity? ',
                      style: AppTextStyles.bodyMedium.copyWith(color: AppColors.textSecondary),
                      children: [
                        TextSpan(
                          text: 'Sign up',
                          style: AppTextStyles.bodyMedium.copyWith(
                            color: AppColors.primary,
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
