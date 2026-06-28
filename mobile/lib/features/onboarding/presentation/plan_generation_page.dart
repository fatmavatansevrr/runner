import 'dart:math' as math;
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/routing/app_router.dart';
import '../data/onboarding_provider.dart';
import '../../../core/widgets/app_button.dart';

class PlanGenerationPage extends ConsumerStatefulWidget {
  const PlanGenerationPage({super.key});

  @override
  ConsumerState<PlanGenerationPage> createState() => _PlanGenerationPageState();
}

class _PlanGenerationPageState extends ConsumerState<PlanGenerationPage>
    with TickerProviderStateMixin {
  String? _error;

  // Step progression — advances as generation proceeds
  int _completedSteps = 0;

  late final AnimationController _progressController;
  late final Animation<double> _progressAnimation;

  // Step delays (ms) — simulate progression during the real API call
  static const _steps = [
    'Understanding your goal',
    'Analyzing the data',
    'Building your plan',
    'Finalizing your plan',
  ];

  @override
  void initState() {
    super.initState();

    _progressController = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 2500),
    );
    _progressAnimation = CurvedAnimation(
      parent: _progressController,
      curve: Curves.easeInOut,
    );

    _startGeneration();
  }

  @override
  void dispose() {
    _progressController.dispose();
    super.dispose();
  }

  /// Advances the visual step + progress independently of the API call,
  /// so the animation always looks alive while waiting.
  void _tickStep(int step, int delayMs) {
    Future.delayed(Duration(milliseconds: delayMs), () {
      if (!mounted) return;
      setState(() => _completedSteps = step);
    });
  }

  void _startGeneration() async {
    setState(() {
      _error = null;
      _completedSteps = 0;
    });

    // Start the arc animation
    _progressController.forward(from: 0.0);

    // Tick the step checklist visually while loading
    _tickStep(1, 400);
    _tickStep(2, 900);
    _tickStep(3, 1400);

    try {
      // Bypassed API call for development visual testing
      await Future.delayed(const Duration(seconds: 2));

      // Ensure final step is marked complete before navigating
      if (mounted) {
        setState(() => _completedSteps = 4);
        await Future.delayed(const Duration(milliseconds: 300));
        if (mounted) context.pushReplacement(AppRoutes.planPreview);
      }
    } catch (e) {
      if (mounted) {
        setState(() => _error = e.toString());
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.navyBackground,
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 24.0),
          child: _error != null ? _buildError() : _buildLoading(),
        ),
      ),
    );
  }

  // ── Error state ──────────────────────────────────────────────────────────
  Widget _buildError() {
    return Center(
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          const Icon(Icons.error_outline_rounded, size: 64, color: Colors.redAccent),
          const SizedBox(height: 24),
          const Text(
            'Generation Failed',
            style: TextStyle(fontSize: 22, fontWeight: FontWeight.bold, color: Colors.white),
            textAlign: TextAlign.center,
          ),
          const SizedBox(height: 12),
          Text(
            _error!,
            style: const TextStyle(fontSize: 14, color: Color(0xFF8E9AB8)),
            textAlign: TextAlign.center,
          ),
          const SizedBox(height: 32),
          AppPrimaryButton(
            label: 'Try Again',
            onPressed: _startGeneration,
          ),
        ],
      ),
    );
  }

  // ── Loading state ─────────────────────────────────────────────────────────
  Widget _buildLoading() {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const SizedBox(height: 16),

        // ── Top progress bar (onboarding step indicator) ─────────────────
        _OnboardingProgressBar(filledFraction: 1.0),
        const SizedBox(height: 40),

        // ── Heading ───────────────────────────────────────────────────────
        const Text(
          'Your plan is cooking...',
          style: TextStyle(
            fontSize: 28,
            fontWeight: FontWeight.w800,
            color: Colors.white,
            height: 1.2,
          ),
        ),
        const SizedBox(height: 12),
        const Text(
          "We're building a plan that fits your goal,\nyour time, and your life.",
          style: TextStyle(fontSize: 15, color: Color(0xFF8E9AB8), height: 1.5),
        ),
        const SizedBox(height: 48),

        // ── Circular progress indicator ───────────────────────────────────
        Center(
          child: AnimatedBuilder(
            animation: _progressAnimation,
            builder: (context, child) {
              final progress = (_completedSteps / _steps.length)
                  .clamp(0.0, 0.72 + 0.28 * _progressAnimation.value);
              return _CircularProgressWidget(progress: progress.clamp(0.0, 1.0));
            },
          ),
        ),
        const SizedBox(height: 48),

        // ── Step checklist ────────────────────────────────────────────────
        ...List.generate(_steps.length, (i) {
          final done = i < _completedSteps;
          final active = i == _completedSteps;
          return Padding(
            padding: const EdgeInsets.only(bottom: 16),
            child: _StepItem(
              label: _steps[i],
              isDone: done,
              isActive: active,
            ),
          );
        }),

        const Spacer(),

        // ── Footnote ───────────────────────────────────────────────────────
        const Center(
          child: Text(
            'This usually takes less than a minute.',
            style: TextStyle(fontSize: 13, color: Color(0xFF5A6580)),
          ),
        ),
        const SizedBox(height: 24),
      ],
    );
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Thin blue progress bar at top (onboarding step position indicator)
// ─────────────────────────────────────────────────────────────────────────────

class _OnboardingProgressBar extends StatelessWidget {
  const _OnboardingProgressBar({required this.filledFraction});
  final double filledFraction;

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(builder: (context, constraints) {
      return ClipRRect(
        borderRadius: BorderRadius.circular(4),
        child: SizedBox(
          height: 4,
          width: constraints.maxWidth,
          child: LinearProgressIndicator(
            value: filledFraction,
            backgroundColor: const Color(0xFF1E2A3F),
            color: AppColors.primary,
          ),
        ),
      );
    });
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Circular arc progress widget
// ─────────────────────────────────────────────────────────────────────────────

class _CircularProgressWidget extends StatelessWidget {
  const _CircularProgressWidget({required this.progress});
  final double progress;

  @override
  Widget build(BuildContext context) {
    final pct = (progress * 100).round();
    return SizedBox(
      width: 180,
      height: 180,
      child: CustomPaint(
        painter: _ArcPainter(progress: progress),
        child: Center(
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Text(
                '$pct%',
                style: const TextStyle(
                  fontSize: 40,
                  fontWeight: FontWeight.w800,
                  color: Colors.white,
                ),
              ),
              const SizedBox(height: 4),
              const Text(
                'Almost there!',
                style: TextStyle(fontSize: 13, color: Color(0xFF8E9AB8)),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _ArcPainter extends CustomPainter {
  const _ArcPainter({required this.progress});
  final double progress;

  @override
  void paint(Canvas canvas, Size size) {
    final center = Offset(size.width / 2, size.height / 2);
    final radius = size.width / 2 - 10;

    // Track
    canvas.drawCircle(
      center,
      radius,
      Paint()
        ..color = const Color(0xFF1E2A3F)
        ..style = PaintingStyle.stroke
        ..strokeWidth = 14,
    );

    // Arc
    final rect = Rect.fromCircle(center: center, radius: radius);
    canvas.drawArc(
      rect,
      -math.pi / 2,          // start from top
      2 * math.pi * progress, // sweep
      false,
      Paint()
        ..color = AppColors.primary
        ..style = PaintingStyle.stroke
        ..strokeWidth = 14
        ..strokeCap = StrokeCap.round,
    );
  }

  @override
  bool shouldRepaint(_ArcPainter old) => old.progress != progress;
}

// ─────────────────────────────────────────────────────────────────────────────
// Step item row
// ─────────────────────────────────────────────────────────────────────────────

class _StepItem extends StatelessWidget {
  const _StepItem({required this.label, required this.isDone, required this.isActive});
  final String label;
  final bool isDone;
  final bool isActive;

  @override
  Widget build(BuildContext context) {
    final iconColor = isDone
        ? AppColors.stepCompleted
        : isActive
            ? AppColors.primary
            : const Color(0xFF2E3A50);

    final textColor = isDone || isActive ? Colors.white : const Color(0xFF5A6580);

    return Row(
      children: [
        AnimatedSwitcher(
          duration: const Duration(milliseconds: 300),
          child: isDone
              ? Icon(Icons.check_circle_rounded, color: iconColor, size: 24, key: const ValueKey('done'))
              : isActive
                  ? SizedBox(
                      width: 24,
                      height: 24,
                      key: const ValueKey('active'),
                      child: CircularProgressIndicator(
                        strokeWidth: 2.5,
                        color: AppColors.primary,
                      ),
                    )
                  : Container(
                      key: const ValueKey('pending'),
                      width: 24,
                      height: 24,
                      decoration: BoxDecoration(
                        shape: BoxShape.circle,
                        border: Border.all(color: iconColor, width: 2),
                      ),
                    ),
        ),
        const SizedBox(width: 14),
        Text(
          label,
          style: TextStyle(fontSize: 15, color: textColor, fontWeight: FontWeight.w500),
        ),
      ],
    );
  }
}
