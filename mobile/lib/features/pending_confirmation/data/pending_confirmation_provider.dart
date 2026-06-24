import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'pending_confirmation_repository.dart';
import '../../../core/network/dtos.dart';

final pendingConfirmationsProvider = FutureProvider<List<PendingConfirmationResponse>>((ref) async {
  final repo = ref.watch(pendingConfirmationRepositoryProvider);
  return repo.fetchPendingConfirmations();
});
