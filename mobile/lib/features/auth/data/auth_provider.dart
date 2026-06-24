import 'package:flutter_riverpod/flutter_riverpod.dart';

class AuthState {
  AuthState({required this.isAuthenticated, this.name, this.email});
  final bool isAuthenticated;
  final String? name;
  final String? email;

  AuthState copyWith({bool? isAuthenticated, String? name, String? email}) {
    return AuthState(
      isAuthenticated: isAuthenticated ?? this.isAuthenticated,
      name: name ?? this.name,
      email: email ?? this.email,
    );
  }
}

class AuthNotifier extends StateNotifier<AuthState> {
  AuthNotifier() : super(AuthState(isAuthenticated: false));

  void loginMock() {
    state = state.copyWith(isAuthenticated: true, name: 'Runner', email: 'runner@antigravity.run');
  }

  void logout() {
    state = AuthState(isAuthenticated: false);
  }
}

final authProvider = StateNotifierProvider<AuthNotifier, AuthState>((ref) {
  return AuthNotifier();
});
