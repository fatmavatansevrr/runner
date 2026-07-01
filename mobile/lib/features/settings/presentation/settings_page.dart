import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_text_styles.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../core/routing/app_router.dart';
import '../../../core/widgets/app_shared_widgets.dart';
import '../../auth/data/auth_providers.dart';
import '../data/settings_provider.dart';
import '../../../core/network/api_provider.dart';
import '../../../core/network/bootstrap_provider.dart';
import '../../home/data/home_provider.dart';
import '../../calendar/data/calendar_provider.dart';
import '../../profile/data/profile_provider.dart';
import '../../pending_confirmation/data/pending_confirmation_provider.dart';

class SettingsPage extends ConsumerStatefulWidget {
  const SettingsPage({super.key});

  @override
  ConsumerState<SettingsPage> createState() => _SettingsPageState();
}

class _SettingsPageState extends ConsumerState<SettingsPage> {
  bool _isResetting = false;

  Future<void> _onSignOut() async {
    // Invalidate data caches first so no stale data flashes on re-login.
    // app.dart's auth-state listener also does this, but calling it here
    // makes the logout feel instant for the current session.
    ref.invalidate(bootstrapDataProvider);
    ref.invalidate(homeDataProvider);
    ref.invalidate(calendarDataProvider);
    ref.invalidate(profileOverviewProvider);
    ref.invalidate(activePlanDetailsProvider);
    ref.invalidate(pendingConfirmationsProvider);

    await ref.read(firebaseAuthRepositoryProvider).signOut();

    // GoRouter's _AuthChangeNotifier will detect the auth state change and
    // trigger the redirect guard automatically. We also navigate explicitly
    // as a belt-and-suspenders fallback.
    if (mounted) context.go(AppRoutes.welcome);
  }

  void _onResetDatabase() async {
    setState(() => _isResetting = true);
    try {
      final client = ref.read(apiClientProvider);
      await client.post('/testing/reset', data: {});
      
      // Invalidate everything to refresh local states
      ref.invalidate(settingsPreferencesProvider);
      ref.invalidate(bootstrapDataProvider);
      ref.invalidate(homeDataProvider);
      ref.invalidate(profileOverviewProvider);
      ref.invalidate(activePlanDetailsProvider);

      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Database reset successful.')),
        );
        context.go(AppRoutes.welcome);
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Failed to reset: ${e.toString()}')),
        );
      }
    } finally {
      if (mounted) {
        setState(() => _isResetting = false);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final preferencesState = ref.watch(settingsPreferencesProvider);

    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        title: const Text('Settings'),
        backgroundColor: Colors.transparent,
        elevation: 0,
        leading: IconButton(
          icon: const Icon(Icons.arrow_back_ios_new_rounded, color: AppColors.textPrimary),
          onPressed: () => context.pop(),
        ),
      ),
      body: preferencesState.when(
        loading: () => const LoadingState(message: 'Loading preferences...'),
        error: (err, _) => Center(
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              const Icon(Icons.error_outline_rounded, size: 48, color: AppColors.textMuted),
              const SizedBox(height: AppSpacing.md),
              const Text('Error loading settings'),
              Text(err.toString(), style: AppTextStyles.bodySmall),
            ],
          ),
        ),
        data: (prefs) {
          return ListView(
            children: [
              const _SectionHeader('Active Plan Settings'),
              _SettingsItem(
                title: 'Plan preferences',
                subtitle: 'Update preferred running days',
                onTap: () {},
              ),
              const _SectionHeader('Notification Settings'),
              _SettingsItem(
                title: 'Workout reminders',
                subtitle: prefs.workoutRemindersEnabled ? 'On' : 'Off',
                onTap: () {},
              ),
              _SettingsItem(
                title: 'Evening check-in',
                subtitle: prefs.eveningReminderEnabled ? 'On' : 'Off',
                onTap: () {},
              ),
              _SectionHeader('App Preferences'),
              _SettingsItem(
                title: 'Distance unit',
                subtitle: 'KM',
                onTap: () {},
              ),
              _SettingsItem(
                title: 'Language',
                subtitle: 'English',
                onTap: () {},
              ),
              const _SectionHeader('Account Settings'),
              _SettingsItem(
                title: 'Sign out',
                subtitle: 'Disconnect from your account',
                textColor: AppColors.missed,
                onTap: _onSignOut,
              ),
              const _SectionHeader('Developer Options'),
              if (_isResetting)
                const Padding(
                  padding: EdgeInsets.all(AppSpacing.md),
                  child: Center(child: CircularProgressIndicator(color: AppColors.primary)),
                )
              else
                _SettingsItem(
                  title: 'Reset database',
                  subtitle: 'Clear all plans & workouts for current user',
                  textColor: Colors.orange,
                  onTap: _onResetDatabase,
                ),
              const _SectionHeader('Legal & Support'),
              _SettingsItem(
                title: 'Privacy policy',
                subtitle: '',
                onTap: () {},
              ),
              _SettingsItem(
                title: 'Terms of service',
                subtitle: '',
                onTap: () {},
              ),
              const SizedBox(height: AppSpacing.xl),
            ],
          );
        },
      ),
    );
  }
}

class _SectionHeader extends StatelessWidget {
  const _SectionHeader(this.title);
  final String title;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(AppSpacing.md, AppSpacing.lg, AppSpacing.md, AppSpacing.sm),
      child: Text(title, style: AppTextStyles.label),
    );
  }
}

class _SettingsItem extends StatelessWidget {
  const _SettingsItem({
    required this.title,
    required this.subtitle,
    required this.onTap,
    this.textColor,
  });

  final String title;
  final String subtitle;
  final VoidCallback onTap;
  final Color? textColor;

  @override
  Widget build(BuildContext context) {
    return ListTile(
      tileColor: AppColors.surface,
      title: Text(
        title,
        style: AppTextStyles.bodyLarge.copyWith(color: textColor ?? AppColors.textPrimary),
      ),
      subtitle: subtitle.isNotEmpty ? Text(subtitle, style: AppTextStyles.bodySmall) : null,
      trailing: const Icon(Icons.chevron_right, color: AppColors.textMuted),
      onTap: onTap,
    );
  }
}
