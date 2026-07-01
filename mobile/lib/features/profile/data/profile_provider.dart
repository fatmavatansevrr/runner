import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'profile_repository.dart';
import '../../../core/network/dtos.dart';

final profileOverviewProvider = FutureProvider<ProfileOverviewResponse>((ref) async {
  final repo = ref.watch(profileRepositoryProvider);
  return repo.fetchProfileOverview();
});

final activePlanDetailsProvider = FutureProvider<PlanDetailsResponse>((ref) async {
  final repo = ref.watch(profileRepositoryProvider);
  return repo.fetchActivePlanDetails();
});
