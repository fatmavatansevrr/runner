import 'dart:io' show Platform;
import 'dart:math';
import 'package:flutter/foundation.dart' show kIsWeb;
import 'package:dio/dio.dart';
import 'api_exception.dart';

/// HTTP client wrapper using Dio.
///
/// Token lifecycle:
/// - [tokenProvider]      → called on every request; returns the cached Firebase
///                          ID token (fast, no network).
/// - [tokenRefresher]     → called once on 401; calls getIdToken(forceRefresh:true)
///                          to get a fresh token from Google, then retries the
///                          original request exactly once.
/// - [onAuthInvalidated]  → called when the retry after forceRefresh still returns
///                          401, meaning the account is deactivated / session truly
///                          revoked. Signs the user out.
///
/// 401 retry flow:
///   request → 401 → forceRefresh token → retry (once) → resolved or propagated
///
/// 403 → safe "permission denied" message, no sign-out.
class ApiClient {
  ApiClient({
    String? baseUrl,
    Future<String?> Function()? tokenProvider,
    Future<String?> Function()? tokenRefresher,
    Future<void> Function()? onAuthInvalidated,
  })  : _tokenProvider = tokenProvider,
        _tokenRefresher = tokenRefresher,
        _onAuthInvalidated = onAuthInvalidated,
        _dio = Dio(
          BaseOptions(
            baseUrl: baseUrl ?? resolveBaseUrl(),
            connectTimeout: const Duration(seconds: 10),
            receiveTimeout: const Duration(seconds: 15),
            headers: {
              'Content-Type': 'application/json',
              'Accept': 'application/json',
            },
          ),
        ) {
    _dio.interceptors.add(
      InterceptorsWrapper(
        onRequest: (options, handler) async {
          options.headers['X-Correlation-Id'] = _newCorrelationId();
          if (_tokenProvider != null) {
            try {
              final token = await _tokenProvider();
              if (token != null) {
                options.headers['Authorization'] = 'Bearer $token';
              }
            } catch (_) {
              // Token fetch failure must not block the request.
            }
          }
          handler.next(options);
        },
        onError: (error, handler) async {
          final statusCode = error.response?.statusCode;
          // Only retry 401s once. The extra flag prevents infinite loops when
          // _dio.fetch() re-enters this interceptor on the retry pass.
          final alreadyRetried =
              error.requestOptions.extra['_tokenRetried'] == true;

          if (statusCode == 401 && !alreadyRetried && _tokenRefresher != null) {
            try {
              final freshToken = await _tokenRefresher();
              if (freshToken != null) {
                // Stamp the request so this interceptor won't retry again.
                error.requestOptions.extra['_tokenRetried'] = true;
                error.requestOptions.headers['Authorization'] =
                    'Bearer $freshToken';
                // Re-execute the original request with the new token.
                final retryResponse = await _dio.fetch(error.requestOptions);
                handler.resolve(retryResponse);
                return;
              }
            } catch (_) {
              // forceRefresh failed — fall through to sign-out + propagate error.
            }
            // Token refresh didn't help → session is truly invalid.
            await _onAuthInvalidated?.call();
          }

          handler.next(error);
        },
      ),
    );
  }

  final Dio _dio;
  final Future<String?> Function()? _tokenProvider;
  final Future<String?> Function()? _tokenRefresher;
  final Future<void> Function()? _onAuthInvalidated;

  static final Random _random = Random();

  static String _newCorrelationId() {
    final bytes = List<int>.generate(16, (_) => _random.nextInt(256));
    return bytes.map((b) => b.toRadixString(16).padLeft(2, '0')).join();
  }

  /// Selects the correct base URL for the current runtime environment.
  ///
  /// Priority: API_BASE_URL env var → 10.0.2.2 on Android emulator →
  /// localhost elsewhere.
  static String resolveBaseUrl() {
    const apiBaseUrl = String.fromEnvironment(
      'API_BASE_URL',
      defaultValue: 'http://10.0.2.2:40118',
    );

    String url = apiBaseUrl;

    if (url == 'http://10.0.2.2:40118' && (kIsWeb || !Platform.isAndroid)) {
      url = 'http://localhost:40118';
    }

    if (!kIsWeb && Platform.isAndroid) {
      url = url
          .replaceAll('localhost', '10.0.2.2')
          .replaceAll('127.0.0.1', '10.0.2.2');
    }

    if (url.isNotEmpty && !url.endsWith('/api/v1')) {
      url = url.endsWith('/') ? '${url}api/v1' : '$url/api/v1';
    }

    return url;
  }

  // ── Public HTTP methods ────────────────────────────────────────────────────

  Future<Response<T>> get<T>(String path,
          {Map<String, dynamic>? queryParameters}) =>
      _run(() => _dio.get(path, queryParameters: queryParameters));

  Future<Response<T>> post<T>(String path, {Object? data}) =>
      _run(() => _dio.post(path, data: data));

  Future<Response<T>> patch<T>(String path, {Object? data}) =>
      _run(() => _dio.patch(path, data: data));

  Future<Response<T>> delete<T>(String path) =>
      _run(() => _dio.delete(path));

  // ── Error mapping ──────────────────────────────────────────────────────────

  Future<Response<T>> _run<T>(Future<Response<T>> Function() request) async {
    try {
      return await request();
    } on DioException catch (e) {
      throw _mapError(e);
    }
  }

  /// Maps a raw [DioException] to a clean [ApiException].
  ///
  /// Checks for the backend's standardised error envelope first:
  ///   `{ "errorCode", "message", "correlationId" }`
  /// Falls back to status-code-derived messages so the UI never sees a
  /// raw Dio error or a stack trace.
  ApiException _mapError(DioException e) {
    final statusCode = e.response?.statusCode;

    // Prefer structured backend error envelope.
    final data = e.response?.data;
    if (data is Map<String, dynamic> && data['message'] is String) {
      return ApiException(
        message: data['message'] as String,
        errorCode: data['errorCode'] as String?,
        correlationId: data['correlationId'] as String?,
        statusCode: statusCode,
      );
    }

    // Derive a safe message from status code / Dio error type.
    final message = switch (e.type) {
      DioExceptionType.connectionTimeout ||
      DioExceptionType.sendTimeout ||
      DioExceptionType.receiveTimeout =>
        'The connection timed out. Please check your network and try again.',
      DioExceptionType.connectionError =>
        "Couldn't reach the server. Please check your connection and try again.",
      DioExceptionType.badResponse => switch (statusCode) {
          401 => 'Your session has expired. Please sign in again.',
          403 => "You don't have permission to perform this action.",
          404 => 'The requested resource was not found.',
          500 || 503 => 'Server error. Please try again later.',
          _ => 'Something went wrong. Please try again.',
        },
      _ => 'Something went wrong. Please try again.',
    };

    return ApiException(message: message, statusCode: statusCode);
  }
}
