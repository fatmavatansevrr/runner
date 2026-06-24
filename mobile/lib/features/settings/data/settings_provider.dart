import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'settings_repository.dart';
import '../../../core/network/dtos.dart';

final settingsPreferencesProvider = FutureProvider<SettingsPreferencesResponse>((ref) async {
  final repo = ref.watch(settingsRepositoryProvider);
  return repo.fetchPreferences();
});
