import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_text_styles.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../core/widgets/app_button.dart';
import '../../../core/routing/app_router.dart';
import '../data/auth_providers.dart';
import '../../../core/network/bootstrap_provider.dart';

class SignInPage extends ConsumerStatefulWidget {
  const SignInPage({super.key});

  @override
  ConsumerState<SignInPage> createState() => _SignInPageState();
}

class _SignInPageState extends ConsumerState<SignInPage> {
  final _emailController    = TextEditingController();
  final _passwordController = TextEditingController();

  bool    _isLoading       = false;
  bool    _obscurePassword = true;
  String? _emailError;
  String? _passwordError;

  @override
  void dispose() {
    _emailController.dispose();
    _passwordController.dispose();
    super.dispose();
  }

  // ── Validation ─────────────────────────────────────────────────────────────

  /// Returns true if all fields pass local validation.
  bool _validate() {
    String? emailErr;
    String? passErr;

    final email    = _emailController.text.trim();
    final password = _passwordController.text;

    if (email.isEmpty) {
      emailErr = 'Email is required.';
    }
    if (password.isEmpty) {
      passErr = 'Password is required.';
    } else if (password.length < 6) {
      passErr = 'Password must be at least 6 characters.';
    }

    setState(() {
      _emailError    = emailErr;
      _passwordError = passErr;
    });

    return emailErr == null && passErr == null;
  }

  // ── Sign In ────────────────────────────────────────────────────────────────

  Future<void> _onSignIn() async {
    if (!_validate()) return;

    setState(() => _isLoading = true);
    try {
      final repo = ref.read(firebaseAuthRepositoryProvider);
      await repo.signInWithEmailAndPassword(
        _emailController.text.trim(),
        _passwordController.text,
      );
      // Force a fresh bootstrap call so the route reflects the user's actual
      // plan state (Home vs plan-generation questions).
      final bootstrap = await ref.refresh(bootstrapDataProvider.future);
      if (mounted) {
        context.go(AppRoutes.routeForNextScreen(bootstrap.nextScreen));
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(e.toString()),
            behavior: SnackBarBehavior.floating,
          ),
        );
      }
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  // ── Build ──────────────────────────────────────────────────────────────────

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.symmetric(
            horizontal: AppSpacing.lg,
            vertical: AppSpacing.md,
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                'Sign in',
                style: AppTextStyles.label.copyWith(fontWeight: FontWeight.bold),
              ),
              const SizedBox(height: AppSpacing.xs),

              // Back button
              Row(
                children: [
                  IconButton(
                    icon: const Icon(Icons.arrow_back_rounded,
                        color: AppColors.textPrimary),
                    padding: EdgeInsets.zero,
                    constraints: const BoxConstraints(),
                    onPressed: () => context.go(AppRoutes.welcome),
                  ),
                ],
              ),
              const SizedBox(height: AppSpacing.xl),

              Text(
                'Welcome back',
                style: AppTextStyles.h1.copyWith(
                  fontSize: 28,
                  fontWeight: FontWeight.w800,
                  letterSpacing: -0.5,
                ),
              ),
              const SizedBox(height: AppSpacing.xs),
              Text(
                'Sign in to access your custom running plan.',
                style: AppTextStyles.bodyLarge
                    .copyWith(color: AppColors.textSecondary),
              ),
              const SizedBox(height: AppSpacing.xl),

              // ── Email ────────────────────────────────────────────────────
              Text(
                'Email',
                style: AppTextStyles.bodyMedium.copyWith(
                  color: AppColors.textPrimary,
                  fontWeight: FontWeight.w600,
                ),
              ),
              const SizedBox(height: AppSpacing.xs),
              TextField(
                controller: _emailController,
                keyboardType: TextInputType.emailAddress,
                autocorrect: false,
                textInputAction: TextInputAction.next,
                onChanged: (_) {
                  if (_emailError != null) {
                    setState(() => _emailError = null);
                  }
                },
                decoration: InputDecoration(
                  hintText: 'Enter your email',
                  errorText: _emailError,
                ),
              ),
              const SizedBox(height: AppSpacing.md),

              // ── Password ─────────────────────────────────────────────────
              Text(
                'Password',
                style: AppTextStyles.bodyMedium.copyWith(
                  color: AppColors.textPrimary,
                  fontWeight: FontWeight.w600,
                ),
              ),
              const SizedBox(height: AppSpacing.xs),
              TextField(
                controller: _passwordController,
                obscureText: _obscurePassword,
                textInputAction: TextInputAction.done,
                onSubmitted: (_) => _onSignIn(),
                onChanged: (_) {
                  if (_passwordError != null) {
                    setState(() => _passwordError = null);
                  }
                },
                decoration: InputDecoration(
                  hintText: 'Enter your password',
                  errorText: _passwordError,
                  suffixIcon: IconButton(
                    icon: Icon(
                      _obscurePassword
                          ? Icons.visibility_outlined
                          : Icons.visibility_off_outlined,
                      color: AppColors.textMuted,
                    ),
                    onPressed: () =>
                        setState(() => _obscurePassword = !_obscurePassword),
                  ),
                ),
              ),
              const SizedBox(height: AppSpacing.xxl),

              // ── Submit ────────────────────────────────────────────────────
              if (_isLoading)
                const Center(
                  child: CircularProgressIndicator(color: AppColors.primary),
                )
              else
                AppPrimaryButton(
                  label: 'Sign in',
                  onPressed: _onSignIn,
                ),
              const SizedBox(height: AppSpacing.lg),

              Center(
                child: TextButton(
                  onPressed: _isLoading
                      ? null
                      : () => context.pushReplacement(AppRoutes.signUp),
                  child: RichText(
                    text: TextSpan(
                      text: "Don't have an account? ",
                      style: AppTextStyles.bodyMedium
                          .copyWith(color: AppColors.textSecondary),
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
