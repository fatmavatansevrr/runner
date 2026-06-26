import 'dart:io' show Platform;
import 'package:flutter/foundation.dart' show kIsWeb;
import 'package:dio/dio.dart';

/// HTTP client wrapper using Dio.
/// Automatically selects the correct base URL for the current platform:
///   - Android emulator: 10.0.2.2 (routes to host machine)
///   - iOS simulator / desktop / web: localhost
class ApiClient {
  ApiClient({String? baseUrl})
      : _dio = Dio(
          BaseOptions(
            baseUrl: baseUrl ?? resolveBaseUrl(),
            connectTimeout: const Duration(seconds: 10),
            receiveTimeout: const Duration(seconds: 15),
            headers: {
              'Content-Type': 'application/json',
              'Accept': 'application/json',
              // TODO (Step 6): Add Authorization: Bearer <token> header
            },
          ),
        );

  final Dio _dio;

  /// Returns the correct base URL for the current runtime environment.
  ///
  /// Uses API_BASE_URL from the environment (defaulting to http://10.0.2.2:40118).
  /// Dynamically switches between 10.0.2.2 for Android emulator and localhost for others.
  static String resolveBaseUrl() {
    const apiBaseUrl = String.fromEnvironment(
      'API_BASE_URL',
      defaultValue: 'http://10.0.2.2:40118',
    );

    String url = apiBaseUrl;

    // If using default and on non-Android platform, switch to localhost
    if (url == 'http://10.0.2.2:40118' && (kIsWeb || !Platform.isAndroid)) {
      url = 'http://localhost:40118';
    }

    // Translate localhost/127.0.0.1 to 10.0.2.2 on Android emulator
    if (!kIsWeb && Platform.isAndroid) {
      url = url
          .replaceAll('localhost', '10.0.2.2')
          .replaceAll('127.0.0.1', '10.0.2.2');
    }

    // Ensure it ends with /api/v1
    if (url.isNotEmpty && !url.endsWith('/api/v1')) {
      if (url.endsWith('/')) {
        url = '${url}api/v1';
      } else {
        url = '$url/api/v1';
      }
    }

    return url;
  }

  Future<Response<T>> get<T>(String path, {Map<String, dynamic>? queryParameters}) =>
      _dio.get(path, queryParameters: queryParameters);

  Future<Response<T>> post<T>(String path, {Object? data}) =>
      _dio.post(path, data: data);

  Future<Response<T>> patch<T>(String path, {Object? data}) =>
      _dio.patch(path, data: data);

  Future<Response<T>> delete<T>(String path) => _dio.delete(path);
}
