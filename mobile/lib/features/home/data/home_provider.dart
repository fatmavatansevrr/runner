import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'home_repository.dart';
import '../../../core/network/dtos.dart';

final homeDataProvider = FutureProvider<HomeResponse>((ref) async {
  final repo = ref.watch(homeRepositoryProvider);
  return repo.fetchHomeData();
});
