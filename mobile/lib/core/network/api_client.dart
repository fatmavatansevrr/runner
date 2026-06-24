import 'package:dio/dio.dart';

/// HTTP client wrapper using Dio.
/// Phase 1: baseUrl points to localhost backend.
/// Step 6 will add request/response interceptors and error handling.
class ApiClient {
  ApiClient({String? baseUrl})
      : _dio = Dio(
          BaseOptions(
            baseUrl: baseUrl ?? 'http://localhost:5001/api/v1',
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

  Future<Response<T>> get<T>(String path, {Map<String, dynamic>? queryParameters}) =>
      _dio.get(path, queryParameters: queryParameters);

  Future<Response<T>> post<T>(String path, {Object? data}) =>
      _dio.post(path, data: data);

  Future<Response<T>> patch<T>(String path, {Object? data}) =>
      _dio.patch(path, data: data);

  Future<Response<T>> delete<T>(String path) => _dio.delete(path);
}
