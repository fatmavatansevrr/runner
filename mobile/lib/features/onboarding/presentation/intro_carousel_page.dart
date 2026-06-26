import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_text_styles.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../core/widgets/app_button.dart';
import '../../../core/widgets/app_shared_widgets.dart';
import '../../../core/routing/app_router.dart';

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
      title: 'See your progress\nclearly',
      subtitle: 'Weekly plans, rest days, and\nmilestones.',
    ),
    _SlideData(
      title: 'Running without\nthe pressure',
      subtitle: 'Personalized plans that fit your life.\nNo guilt when life gets in the way.',
    ),
    _SlideData(
      title: 'Your adaptive\nrunning partner',
      subtitle: 'The plan adjusts with you —\nautomatically.',
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
      backgroundColor: AppColors.background, // Light background matching screens
      body: SafeArea(
        child: Column(
          children: [
            // Top segment progress indicators
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: AppSpacing.lg, vertical: AppSpacing.sm),
              child: Row(
                children: List.generate(_slides.length, (index) {
                  final isActive = index == _currentPage;
                  return Expanded(
                    child: Container(
                      height: 4,
                      margin: const EdgeInsets.symmetric(horizontal: 4),
                      decoration: BoxDecoration(
                        color: isActive ? AppColors.primary : AppColors.border,
                        borderRadius: BorderRadius.circular(2),
                      ),
                    ),
                  );
                }),
              ),
            ),

            // Top Navigation (Back & Skip)
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: AppSpacing.md, vertical: AppSpacing.xs),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  IconButton(
                    icon: const Icon(Icons.arrow_back_rounded, color: AppColors.textPrimary),
                    onPressed: () {
                      if (_currentPage == 0) {
                        context.go(AppRoutes.welcome);
                      } else {
                        _controller.previousPage(
                          duration: const Duration(milliseconds: 300),
                          curve: Curves.easeInOut,
                        );
                      }
                    },
                  ),
                  TextButton(
                    onPressed: () => context.go(AppRoutes.goalSelection),
                    child: Text(
                      'Skip',
                      style: AppTextStyles.bodyMedium.copyWith(
                        color: AppColors.textSecondary,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                  ),
                ],
              ),
            ),

            // Slides
            Expanded(
              child: PageView.builder(
                controller: _controller,
                onPageChanged: (i) => setState(() => _currentPage = i),
                itemCount: _slides.length,
                itemBuilder: (_, i) => _SlideView(slide: _slides[i], index: i),
              ),
            ),

            // Progress dots (visual secondary indicator or removed if segment represents it, but let's keep it clean as spacing)
            const SizedBox(height: AppSpacing.lg),

            // Continue CTA
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: AppSpacing.lg),
              child: AppPrimaryButton(
                label: _currentPage < _slides.length - 1 ? 'Continue' : 'Get started',
                icon: Icons.arrow_forward_rounded,
                onPressed: _next,
              ),
            ),
            const SizedBox(height: AppSpacing.xl),
          ],
        ),
      ),
    );
  }
}

class _SlideData {
  const _SlideData({required this.title, required this.subtitle});
  final String title;
  final String subtitle;
}

class _SlideView extends StatelessWidget {
  const _SlideView({required this.slide, required this.index});
  final _SlideData slide;
  final int index;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: AppSpacing.xl),
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          // Graphic illustrations representing the screens in onboarding-placeholder
          _buildIllustration(index),
          const SizedBox(height: AppSpacing.xxl),

          // Slide Title (colored blue/primary)
          Text(
            slide.title,
            style: AppTextStyles.h1.copyWith(
              color: AppColors.primary,
              fontSize: 28,
              fontWeight: FontWeight.w800,
              height: 1.2,
              letterSpacing: -0.5,
            ),
            textAlign: TextAlign.center,
          ),
          const SizedBox(height: AppSpacing.md),

          // Slide Subtitle
          Text(
            slide.subtitle,
            style: AppTextStyles.bodyLarge.copyWith(
              color: AppColors.textSecondary,
              height: 1.4,
            ),
            textAlign: TextAlign.center,
          ),
        ],
      ),
    );
  }

  Widget _buildIllustration(int idx) {
    if (idx == 0) {
      // Calendar view illustration matching onboarding-placeholder.png
      return Container(
        width: 190,
        height: 140,
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(16),
          boxShadow: [
            BoxShadow(
              color: Colors.black.withOpacity(0.06),
              blurRadius: 20,
              offset: const Offset(0, 10),
            ),
          ],
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            // Blue Header block
            Container(
              height: 32,
              decoration: const BoxDecoration(
                color: AppColors.primary,
                borderRadius: BorderRadius.only(
                  topLeft: Radius.circular(16),
                  topRight: Radius.circular(16),
                ),
              ),
              child: Padding(
                padding: const EdgeInsets.symmetric(horizontal: 12),
                child: Row(
                  children: [
                    Container(width: 8, height: 8, decoration: const BoxDecoration(color: Colors.white24, shape: BoxShape.circle)),
                    const SizedBox(width: 6),
                    Container(width: 8, height: 8, decoration: const BoxDecoration(color: Colors.white24, shape: BoxShape.circle)),
                  ],
                ),
              ),
            ),
            // Calendar cells mockup
            Expanded(
              child: Padding(
                padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
                child: Column(
                  mainAxisAlignment: MainAxisAlignment.spaceEvenly,
                  children: [
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: List.generate(7, (i) {
                        final isCompleted = i == 3;
                        final isRest = i == 5;
                        return Container(
                          width: 18,
                          height: 18,
                          decoration: BoxDecoration(
                            color: isCompleted
                                ? AppColors.easyRunTint
                                : isRest
                                    ? AppColors.restTint
                                    : AppColors.border.withOpacity(0.4),
                            borderRadius: BorderRadius.circular(4),
                          ),
                          child: isCompleted
                              ? const Center(child: Icon(Icons.check, size: 10, color: AppColors.primary))
                              : null,
                        );
                      }),
                    ),
                    Row(
                      mainAxisAlignment: MainAxisAlignment.start,
                      children: [
                        Container(
                          width: 18,
                          height: 18,
                          decoration: BoxDecoration(
                            color: AppColors.primary,
                            borderRadius: BorderRadius.circular(4),
                          ),
                          child: const Center(
                            child: Text(
                              '15',
                              style: TextStyle(color: Colors.white, fontSize: 8, fontWeight: FontWeight.bold),
                            ),
                          ),
                        ),
                        const SizedBox(width: 6),
                        Container(
                          width: 18,
                          height: 18,
                          decoration: BoxDecoration(
                            color: AppColors.border.withOpacity(0.4),
                            borderRadius: BorderRadius.circular(4),
                          ),
                        ),
                        const Spacer(),
                        // Rest Day indicator
                        Container(
                          padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                          decoration: BoxDecoration(
                            color: AppColors.restTint,
                            borderRadius: BorderRadius.circular(4),
                          ),
                          child: Text(
                            'Rest Day',
                            style: AppTextStyles.bodySmall.copyWith(
                              fontSize: 8,
                              fontWeight: FontWeight.bold,
                              color: Colors.orange.shade700,
                            ),
                          ),
                        ),
                      ],
                    ),
                  ],
                ),
              ),
            ),
          ],
        ),
      );
    } else if (idx == 1) {
      // Heart/Relaxed running illustration
      return Container(
        width: 190,
        height: 140,
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(16),
          boxShadow: [
            BoxShadow(
              color: Colors.black.withOpacity(0.06),
              blurRadius: 20,
              offset: const Offset(0, 10),
            ),
          ],
        ),
        child: Center(
          child: Container(
            width: 72,
            height: 72,
            decoration: const BoxDecoration(
              color: AppColors.intervalTint,
              shape: BoxShape.circle,
            ),
            child: const Center(
              child: Icon(
                Icons.favorite_rounded,
                size: 36,
                color: Colors.pinkAccent,
              ),
            ),
          ),
        ),
      );
    } else {
      // Adaptive planner circular flow illustration
      return Container(
        width: 190,
        height: 140,
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(16),
          boxShadow: [
            BoxShadow(
              color: Colors.black.withOpacity(0.06),
              blurRadius: 20,
              offset: const Offset(0, 10),
            ),
          ],
        ),
        child: Center(
          child: Container(
            width: 72,
            height: 72,
            decoration: const BoxDecoration(
              color: AppColors.primaryLight,
              shape: BoxShape.circle,
            ),
            child: const Center(
              child: Icon(
                Icons.auto_fix_high_rounded,
                size: 36,
                color: AppColors.primary,
              ),
            ),
          ),
        ),
      );
    }
  }
}
