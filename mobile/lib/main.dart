import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'core/network/api_client.dart';
import 'app.dart';

void main() {
  print('API Base URL: ${ApiClient.resolveBaseUrl()}');
  runApp(
    const ProviderScope(
      child: AntigravityApp(),
    ),
  );
}
