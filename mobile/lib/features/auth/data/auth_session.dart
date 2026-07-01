import 'package:firebase_auth/firebase_auth.dart';

class AuthSession {
  const AuthSession({
    required this.uid,
    this.email,
    this.displayName,
    this.photoUrl,
    required this.idToken,
    required this.provider,
    required this.isEmailVerified,
  });

  final String uid;
  final String? email;
  final String? displayName;
  final String? photoUrl;
  final String idToken;

  /// One of: 'password', 'google.com', 'apple.com'
  final String provider;
  final bool isEmailVerified;

  static Future<AuthSession> fromUser(User user, {required String provider}) async {
    final idToken = await user.getIdToken() ?? '';
    return AuthSession(
      uid: user.uid,
      email: user.email,
      displayName: user.displayName,
      photoUrl: user.photoURL,
      idToken: idToken,
      provider: provider,
      isEmailVerified: user.emailVerified,
    );
  }
}
