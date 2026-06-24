import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'training_day_repository.dart';
import '../../../core/network/dtos.dart';

final trainingDayDetailProvider = FutureProvider.family<TrainingDayDetailResponse, String>((ref, dayId) async {
  final repo = ref.watch(trainingDayRepositoryProvider);
  return repo.fetchTrainingDayDetail(dayId);
});
