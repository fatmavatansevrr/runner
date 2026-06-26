import 'package:flutter_test/flutter_test.dart';
import 'package:antigravity_app/core/network/api_client.dart';

void main() {
  group('ApiClient Base URL Tests', () {
    test('resolveBaseUrl returns valid api base url', () {
      final baseUrl = ApiClient.resolveBaseUrl();
      expect(baseUrl, isNotEmpty);
      expect(baseUrl.endsWith('/api/v1'), isTrue);
      expect(baseUrl.startsWith('http://'), isTrue);
    });
  });
}
