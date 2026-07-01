import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:shared_preferences/shared_preferences.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/routing/app_router.dart';

class IntroCarouselPage extends StatefulWidget {
  const IntroCarouselPage({super.key});

  @override
  State<IntroCarouselPage> createState() => _IntroCarouselPageState();
}

class _IntroCarouselPageState extends State<IntroCarouselPage> {
  final PageController _controller = PageController(viewportFraction: 1.0);

  int _currentPage = 0;
  double _pageOffset = 0.0;
  bool _initialized = false;

  static const List<_SlideData> _slides = [
    _SlideData(
      title: 'Yes, you can\nsing while you run.',
      subtitle: 'Your easy runs are for\nenjoying the moment.',
      asset: 'assets/images/easy-illustration.png',
      bgColor: Color(0xFFC9E0F9),
      accentColor: Color(0xFF2F6BFF),
      illustrationScale: 1.00,
      illustrationWidthFactor: 0.88,
    ),
    _SlideData(
      title: 'You can run from\nKültürpark to Göztepe.',
      subtitle: 'Long runs take you\nfurther than you think.',
      asset: 'assets/images/long-illustration.png',
      bgColor: Color(0xFFE8DFFD),
      accentColor: Color(0xFF8A5CFF),
      illustrationScale: 1.00,
      illustrationWidthFactor: 0.92,
    ),
    _SlideData(
      title: 'You might even race\nan ostrich.',
      subtitle: 'Intervals make you faster,\nstride by stride.',
      asset: 'assets/images/interval-illustration.png',
      bgColor: Color(0xFFFFD6D6),
      accentColor: Color(0xFFFF3F56),
      illustrationScale: 1.00,
      illustrationWidthFactor: 0.94,
    ),
    _SlideData(
      title: 'And on rest days,\nwe sunbathe together.',
      subtitle: 'Rest is part of the plan.\nYou\'ve earned it.',
      asset: 'assets/images/rest-illustration.png',
      bgColor: Color(0xFFFEF4DD),
      accentColor: Color(0xFFD9A400),
      illustrationScale: 1.35,
      illustrationWidthFactor: 1.05,
    ),
    _SlideData(
      title: 'Ready to do\nthis together?',
      subtitle:
          'Whether you\'re building a habit or training for your next race, we\'ll be with you every step of the way.',
      asset: 'assets/images/team-illustration.png',
      bgColor: Color(0xFFF5F5F7),
      accentColor: Color(0xFF0F1426),
      illustrationScale: 1.10,
      illustrationWidthFactor: 1.22,
    ),
  ];

  @override
  void initState() {
    super.initState();
    _initialized = true;

    _controller.addListener(() {
      if (!_controller.hasClients) return;
      setState(() {
        _pageOffset = _controller.page ?? 0.0;
      });
    });
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  Future<void> _onFinish() async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setBool('hasSeenWelcomeCarousel', true);

    if (mounted) {
      context.go(AppRoutes.authEntry);
    }
  }

  void _onNext() {
    if (_currentPage < _slides.length - 1) {
      _controller.nextPage(
        duration: const Duration(milliseconds: 300),
        curve: Curves.easeInOut,
      );
    } else {
      _onFinish();
    }
  }

  Color _resolveBackgroundColor() {
    final floorIndex = _pageOffset.floor().clamp(0, _slides.length - 1);
    final ceilIndex = _pageOffset.ceil().clamp(0, _slides.length - 1);
    final ratio = _pageOffset - floorIndex;

    return Color.lerp(
          _slides[floorIndex].bgColor,
          _slides[ceilIndex].bgColor,
          ratio,
        ) ??
        _slides[_currentPage].bgColor;
  }

  @override
  Widget build(BuildContext context) {
    if (!_initialized) {
      return const Scaffold(
        backgroundColor: Color(0xFFF5F5F7),
        body: Center(
          child: CircularProgressIndicator(color: AppColors.primary),
        ),
      );
    }

    return Scaffold(
      backgroundColor: _resolveBackgroundColor(),
      body: SafeArea(
        child: Stack(
          children: [
            Column(
              children: [
                _TopProgressBar(
                  currentPage: _currentPage,
                  slides: _slides,
                  showSkip: _currentPage < _slides.length - 1,
                  onSkip: () {
                    _controller.animateToPage(
                      _slides.length - 1,
                      duration: const Duration(milliseconds: 400),
                      curve: Curves.easeInOut,
                    );
                  },
                ),
                Expanded(
                  child: PageView.builder(
                    controller: _controller,
                    itemCount: _slides.length,
                    onPageChanged: (index) {
                      setState(() => _currentPage = index);
                    },
                    itemBuilder: (context, index) {
                      return _SlideView(
                        slide: _slides[index],
                        index: index,
                        isLast: index == _slides.length - 1,
                        onFinish: _onFinish,
                      );
                    },
                  ),
                ),
              ],
            ),
            if (_currentPage < _slides.length - 1)
              Positioned(
                right: 24,
                bottom: 32,
                child: _NextArrowButton(
                  color: _slides[_currentPage].accentColor,
                  onTap: _onNext,
                ),
              ),
          ],
        ),
      ),
    );
  }
}

class _SlideData {
  const _SlideData({
    required this.title,
    required this.subtitle,
    required this.asset,
    required this.bgColor,
    required this.accentColor,
    required this.illustrationScale,
    required this.illustrationWidthFactor,
    this.illustrationDx = 0,
  });

  final String title;
  final String subtitle;
  final String asset;
  final Color bgColor;
  final Color accentColor;
  final double illustrationScale;
  final double illustrationWidthFactor;
  final double illustrationDx;
}

class _TopProgressBar extends StatelessWidget {
  const _TopProgressBar({
    required this.currentPage,
    required this.slides,
    required this.showSkip,
    required this.onSkip,
  });

  final int currentPage;
  final List<_SlideData> slides;
  final bool showSkip;
  final VoidCallback onSkip;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(24, 18, 24, 10),
      child: SizedBox(
        // Fixed height so the bar sits at the same vertical position
        // whether or not the "Skip" label is shown next to it.
        height: 20,
        child: Row(
          children: [
            Expanded(
              child: Row(
                children: List.generate(slides.length, (index) {
                  final isLastPage = currentPage == slides.length - 1;
                  final isActive = index == currentPage;

                  return Expanded(
                    child: AnimatedContainer(
                      duration: const Duration(milliseconds: 250),
                      curve: Curves.easeInOut,
                      height: 4,
                      margin: const EdgeInsets.symmetric(horizontal: 4),
                      decoration: BoxDecoration(
                        // Last page: reveal every segment in its own color.
                        // Other pages: only the current page's segment is
                        // colored, at the matching position; the rest stay
                        // colorless/faded.
                        color: isLastPage || isActive
                            ? slides[index].accentColor
                            : Colors.white.withOpacity(0.65),
                        borderRadius: BorderRadius.circular(999),
                      ),
                    ),
                  );
                }),
              ),
            ),
            if (showSkip) ...[
              const SizedBox(width: 16),
              GestureDetector(
                onTap: onSkip,
                child: const Text(
                  'Skip',
                  style: TextStyle(
                    fontFamily: 'GeneralSans',
                    fontSize: 14,
                    fontWeight: FontWeight.w400,
                    color: Color(0xFFA3A8B3),
                  ),
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }
}

class _SlideView extends StatelessWidget {
  const _SlideView({
    required this.slide,
    required this.index,
    required this.isLast,
    required this.onFinish,
  });

  final _SlideData slide;
  final int index;
  final bool isLast;
  final VoidCallback onFinish;

  @override
  Widget build(BuildContext context) {
    final size = MediaQuery.sizeOf(context);
    final screenHeight = size.height;
    final screenWidth = size.width;

    if (isLast) {
      return _buildLastSlide(context, screenWidth);
    }
    return _buildGenericSlide(context, screenHeight, screenWidth);
  }

  Widget _buildTextBlock(double screenWidth) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const SizedBox(height: 34),
        Text(
          slide.title,
          style: const TextStyle(
            fontFamily: 'ClashGrotesk',
            fontSize: 33,
            fontWeight: FontWeight.w700,
            height: 1.13,
            letterSpacing: -0.6,
            color: Color(0xFF0F172A),
          ),
        ),
        const SizedBox(height: 14),
        ConstrainedBox(
          constraints: BoxConstraints(maxWidth: screenWidth * 0.82),
          child: Text(
            slide.subtitle,
            style: const TextStyle(
              fontFamily: 'GeneralSans',
              fontSize: 15,
              fontWeight: FontWeight.w400,
              height: 1.4,
              color: Color(0xFF475569),
            ),
          ),
        ),
      ],
    );
  }

  Widget _buildGenericSlide(
    BuildContext context,
    double screenHeight,
    double screenWidth,
  ) {
    final topTextAreaHeight = screenHeight * 0.27;
    final illustrationBoxHeight = screenHeight * 0.40;

    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 24),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(
            height: topTextAreaHeight,
            child: _buildTextBlock(screenWidth),
          ),
          SizedBox(height: screenHeight * 0.04),
          Center(
            child: SizedBox(
              height: illustrationBoxHeight,
              width: double.infinity,
              child: OverflowBox(
                minWidth: 0,
                maxWidth: screenWidth * 1.35,
                minHeight: 0,
                maxHeight: illustrationBoxHeight * 1.25,
                alignment: Alignment.center,
                child: Transform.translate(
                  offset: Offset(slide.illustrationDx, 0),
                  child: Transform.scale(
                    scale: slide.illustrationScale,
                    alignment: Alignment.center,
                    child: Image.asset(
                      slide.asset,
                      width: screenWidth * slide.illustrationWidthFactor,
                      fit: BoxFit.contain,
                      alignment: Alignment.center,
                      filterQuality: FilterQuality.high,
                    ),
                  ),
                ),
              ),
            ),
          ),
          const Spacer(),
          const SizedBox(height: 86),
        ],
      ),
    );
  }

  Widget _buildLastSlide(BuildContext context, double screenWidth) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Padding(
          padding: const EdgeInsets.symmetric(horizontal: 24),
          child: _buildTextBlock(screenWidth),
        ),
        Expanded(
          child: Align(
            alignment: Alignment.bottomCenter,
            child: Transform.scale(
              scale: slide.illustrationScale,
              alignment: Alignment.bottomCenter,
              child: Image.asset(
                slide.asset,
                width: screenWidth * slide.illustrationWidthFactor,
                fit: BoxFit.fitWidth,
                alignment: Alignment.bottomCenter,
                filterQuality: FilterQuality.high,
              ),
            ),
          ),
        ),
        const SizedBox(height: 24),
        Padding(
          padding: const EdgeInsets.fromLTRB(24, 0, 24, 26),
          child: SizedBox(
            width: double.infinity,
            height: 56,
            child: ElevatedButton(
              onPressed: onFinish,
              style: ElevatedButton.styleFrom(
                backgroundColor: const Color(0xFF0F1426),
                elevation: 0,
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(16),
                ),
              ),
              child: const Text(
                "Let's Start",
                style: TextStyle(
                  fontFamily: 'GeneralSans',
                  fontSize: 16,
                  fontWeight: FontWeight.w600,
                  color: Colors.white,
                ),
              ),
            ),
          ),
        ),
      ],
    );
  }
}

class _NextArrowButton extends StatelessWidget {
  const _NextArrowButton({
    required this.color,
    required this.onTap,
  });

  final Color color;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        width: 44,
        height: 44,
        decoration: BoxDecoration(
          color: Colors.white,
          shape: BoxShape.circle,
          boxShadow: [
            BoxShadow(
              color: Colors.black.withOpacity(0.07),
              blurRadius: 14,
              offset: const Offset(0, 5),
            ),
          ],
        ),
        child: Icon(
          Icons.arrow_forward_rounded,
          size: 22,
          color: color,
        ),
      ),
    );
  }
}
