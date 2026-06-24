import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_text_styles.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../core/widgets/app_button.dart';
import '../../../core/widgets/app_shared_widgets.dart';
import '../../../core/routing/app_router.dart';

/// Intro carousel (ref: 02_intro_carousel.png — the flow diagram)
/// 3 slides: Progress & Clarity, Low Pressure Running, Adaptive Planning.
class IntroCarouselPage extends StatefulWidget {
  const IntroCarouselPage({super.key});

  @override
  State<IntroCarouselPage> createState() => _IntroCarouselPageState();
}

class _IntroCarouselPageState extends State<IntroCarouselPage> {
  final _controller = PageController();
  int _currentPage = 0;

  static const _slides = [
    _SlideData(
      title: 'Track your progress clearly',
      subtitle: 'Know exactly what to run today. No guesswork.',
      icon: Icons.track_changes_rounded,
    ),
    _SlideData(
      title: 'Running without the pressure',
      subtitle: 'Plans that fit real life. No guilt when life gets in the way.',
      icon: Icons.favorite_rounded,
    ),
    _SlideData(
      title: 'Your adaptive running partner',
      subtitle: 'The plan adjusts with you — automatically.',
      icon: Icons.auto_fix_high_rounded,
    ),
  ];

  void _next() {
    if (_currentPage < _slides.length - 1) {
      _controller.nextPage(
        duration: const Duration(milliseconds: 300),
        curve: Curves.easeInOut,
      );
    } else {
      context.go(AppRoutes.goalSelection);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.surface,
      body: SafeArea(
        child: Column(
          children: [
            // Skip button
            Align(
              alignment: Alignment.centerRight,
              child: TextButton(
                onPressed: () => context.go(AppRoutes.goalSelection),
                child: Text('Skip', style: AppTextStyles.bodyMedium),
              ),
            ),

            // Slides
            Expanded(
              child: PageView.builder(
                controller: _controller,
                onPageChanged: (i) => setState(() => _currentPage = i),
                itemCount: _slides.length,
                itemBuilder: (_, i) => _SlideView(slide: _slides[i]),
              ),
            ),

            // Progress dots
            AppProgressDots(
              count: _slides.length,
              currentIndex: _currentPage,
            ),
            const SizedBox(height: AppSpacing.lg),

            // Continue CTA
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: AppSpacing.md),
              child: AppPrimaryButton(
                label: _currentPage < _slides.length - 1 ? 'Continue' : 'Get started',
                icon: Icons.arrow_forward_rounded,
                onPressed: _next,
              ),
            ),
            const SizedBox(height: AppSpacing.lg),
          ],
        ),
      ),
    );
  }
}

class _SlideData {
  const _SlideData({required this.title, required this.subtitle, required this.icon});
  final String title;
  final String subtitle;
  final IconData icon;
}

class _SlideView extends StatelessWidget {
  const _SlideView({required this.slide});
  final _SlideData slide;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: AppSpacing.xl),
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Container(
            width: 96,
            height: 96,
            decoration: BoxDecoration(
              color: AppColors.primaryLight,
              shape: BoxShape.circle,
            ),
            child: Icon(slide.icon, size: 48, color: AppColors.primary),
          ),
          const SizedBox(height: AppSpacing.xl),
          Text(slide.title, style: AppTextStyles.h2, textAlign: TextAlign.center),
          const SizedBox(height: AppSpacing.sm),
          Text(slide.subtitle, style: AppTextStyles.bodyLarge.copyWith(color: AppColors.textSecondary), textAlign: TextAlign.center),
        ],
      ),
    );
  }
}
