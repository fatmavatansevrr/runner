import 'dart:convert';
import 'dart:io';

import 'package:flutter_test/flutter_test.dart';

void main() {
  final gradleFile = File('android/app/build.gradle.kts');
  final googleServicesFile = File('android/app/google-services.json');
  final androidIgnoreFile = File('android/.gitignore');

  test('Android minimum SDK cannot regress below Firebase Auth API 23', () {
    final gradle = gradleFile.readAsStringSync();
    expect(gradle, contains('minSdk = 23'));
    expect(gradle, isNot(contains('minSdk = flutter.minSdkVersion')));
  });

  test('release signing is external and never falls back to debug signing', () {
    final gradle = gradleFile.readAsStringSync();
    expect(gradle, contains('APPSEL_RELEASE_STORE_FILE'));
    expect(gradle, contains('APPSEL_RELEASE_STORE_PASSWORD'));
    expect(gradle, contains('APPSEL_RELEASE_KEY_ALIAS'));
    expect(gradle, contains('APPSEL_RELEASE_KEY_PASSWORD'));
    expect(
      gradle,
      isNot(contains('signingConfig = signingConfigs.getByName("debug")')),
    );

    final ignores = androidIgnoreFile.readAsStringSync();
    expect(ignores, contains('key.properties'));
    expect(ignores, contains('**/*.keystore'));
    expect(ignores, contains('**/*.jks'));
  });

  test('checked-in Android identity exactly matches Firebase registration', () {
    final gradle = gradleFile.readAsStringSync();
    final configured = RegExp(
      r'firebaseRegisteredApplicationId = "([^"]+)"',
    ).firstMatch(gradle)!.group(1);
    final json = jsonDecode(googleServicesFile.readAsStringSync())
        as Map<String, dynamic>;
    final clients = json['client'] as List<dynamic>;
    final packages = clients
        .map(
          (client) => ((client as Map<String, dynamic>)['client_info']
                  as Map<String, dynamic>)['android_client_info']
              as Map<String, dynamic>,
        )
        .map((android) => android['package_name'] as String)
        .toSet();

    expect(packages, contains(configured));
  });
}
