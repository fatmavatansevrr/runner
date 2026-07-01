import 'dart:convert';
import 'dart:io' show Platform;
import 'dart:math';
import 'package:crypto/crypto.dart';
import 'package:firebase_auth/firebase_auth.dart';
import 'package:flutter/foundation.dart' show kIsWeb;
import 'package:sign_in_with_apple/sign_in_with_apple.dart';
import 'auth_session.dart';
import 'auth_exception.dart';

class FirebaseAuthRepository {
  FirebaseAuthRepository({FirebaseAuth? auth})
      : _auth = auth ?? FirebaseAuth.instance;

  final FirebaseAuth _auth;

  // ── Stream ──────────────────────────────────────────────────────────────────

  Stream<AuthSession?> authStateChanges() {
    return _auth.authStateChanges().asyncMap((user) async {
      if (user == null) return null;
      return AuthSession.fromUser(user, provider: _providerIdFor(user));
    });
  }

  // ── Read ────────────────────────────────────────────────────────────────────

  Future<AuthSession?> getCurrentSession() async {
    final user = _auth.currentUser;
    if (user == null) return null;
    return AuthSession.fromUser(user, provider: _providerIdFor(user));
  }

  Future<String?> getIdToken({bool forceRefresh = false}) async {
    return _auth.currentUser?.getIdToken(forceRefresh);
  }

  // ── Email / Password ────────────────────────────────────────────────────────

  Future<AuthSession> signInWithEmailAndPassword(
    String email,
    String password,
  ) async {
    try {
      final result = await _auth.signInWithEmailAndPassword(
        email: email,
        password: password,
      );
      return AuthSession.fromUser(result.user!, provider: 'password');
    } on FirebaseAuthException catch (e) {
      throw _mapFirebaseError(e);
    } catch (_) {
      throw const AuthException('Sign in failed. Please try again.');
    }
  }

  Future<AuthSession> registerWithEmailAndPassword(
    String email,
    String password, {
    String? displayName,
  }) async {
    try {
      final result = await _auth.createUserWithEmailAndPassword(
        email: email,
        password: password,
      );
      final user = result.user!;
      if (displayName != null && displayName.isNotEmpty) {
        // Best-effort — failure here doesn't block onboarding.
        await user.updateDisplayName(displayName).catchError((_) {});
      }
      return AuthSession.fromUser(user, provider: 'password');
    } on FirebaseAuthException catch (e) {
      throw _mapFirebaseError(e);
    } catch (_) {
      throw const AuthException('Registration failed. Please try again.');
    }
  }

  // ── Google ──────────────────────────────────────────────────────────────────
  //
  // Uses FirebaseAuth.signInWithProvider(GoogleAuthProvider()) which opens the
  // native Google account picker on Android/iOS without requiring the
  // google_sign_in package's direct API (which changed in v7).

  Future<AuthSession> signInWithGoogle() async {
    try {
      final provider = GoogleAuthProvider()
        ..addScope('email')
        ..addScope('profile');

      UserCredential result;
      if (kIsWeb) {
        result = await _auth.signInWithPopup(provider);
      } else {
        result = await _auth.signInWithProvider(provider);
      }
      return AuthSession.fromUser(result.user!, provider: 'google.com');
    } on FirebaseAuthException catch (e) {
      if (_isGoogleCancel(e.code)) {
        throw const AuthException('Google sign in was cancelled.',
            isCancelled: true);
      }
      throw _mapFirebaseError(e);
    } catch (e) {
      final msg = e.toString().toLowerCase();
      if (msg.contains('cancel') || msg.contains('aborted') ||
          msg.contains('sign_in_cancelled') || msg.contains('sign_in_failed') &&
              msg.contains('12501')) {
        throw const AuthException('Google sign in was cancelled.',
            isCancelled: true);
      }
      throw const AuthException('Google sign in failed. Please try again.');
    }
  }

  static bool _isGoogleCancel(String code) => const {
        'web-context-cancelled',
        'canceled',
        'user-cancelled',
        'sign_in_cancelled',
      }.contains(code);

  // ── Apple ───────────────────────────────────────────────────────────────────
  //
  // Apple Sign-In is only supported on iOS 13+ and macOS 10.15+.
  // On Android there is no native Apple sheet; the button must be hidden at
  // the UI layer before this method is ever called.
  //
  // Security: a cryptographically random nonce is generated per-request.
  // Its SHA-256 hash is sent to Apple; the raw nonce is passed to Firebase so
  // it can verify the token wasn't captured in transit (replay protection).

  static bool get isAppleSignInSupported =>
      !kIsWeb && (Platform.isIOS || Platform.isMacOS);

  Future<AuthSession> signInWithApple() async {
    if (!isAppleSignInSupported) {
      throw const AuthException(
          'Apple sign in is not supported on this platform.');
    }

    final rawNonce = _generateNonce();
    final nonce   = _sha256(rawNonce);

    try {
      final appleCredential = await SignInWithApple.getAppleIDCredential(
        scopes: [
          AppleIDAuthorizationScopes.email,
          AppleIDAuthorizationScopes.fullName,
        ],
        nonce: nonce,
      );

      final oauthCredential = OAuthProvider('apple.com').credential(
        idToken: appleCredential.identityToken,
        accessToken: appleCredential.authorizationCode,
        rawNonce: rawNonce,
      );

      final result = await _auth.signInWithCredential(oauthCredential);
      final user   = result.user!;

      // Apple only sends givenName/familyName on the very first sign-in.
      // Persist it to Firebase profile immediately so it's available on
      // subsequent sessions via user.displayName.
      final given  = appleCredential.givenName;
      final family = appleCredential.familyName;
      if (given != null || family != null) {
        final fullName = [given, family].where((n) => n != null).join(' ');
        await user.updateDisplayName(fullName).catchError((_) {});
      }

      return AuthSession.fromUser(user, provider: 'apple.com');
    } on SignInWithAppleAuthorizationException catch (e) {
      if (e.code == AuthorizationErrorCode.canceled) {
        throw const AuthException('Apple sign in was cancelled.',
            isCancelled: true);
      }
      throw const AuthException('Apple sign in failed. Please try again.');
    } on AuthException {
      rethrow;
    } on FirebaseAuthException catch (e) {
      throw _mapFirebaseError(e);
    } catch (_) {
      throw const AuthException('Apple sign in failed. Please try again.');
    }
  }

  // ── Nonce helpers ────────────────────────────────────────────────────────────

  static String _generateNonce([int length = 32]) {
    const chars =
        'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-._';
    final rng = Random.secure();
    return List.generate(length, (_) => chars[rng.nextInt(chars.length)])
        .join();
  }

  static String _sha256(String input) {
    final bytes  = utf8.encode(input);
    final digest = sha256.convert(bytes);
    return digest.toString();
  }

  // ── Sign Out ─────────────────────────────────────────────────────────────────

  Future<void> signOut() async {
    await _auth.signOut();
  }

  // ── Helpers ──────────────────────────────────────────────────────────────────

  String _providerIdFor(User user) {
    if (user.providerData.isEmpty) return 'password';
    return user.providerData.first.providerId;
  }

  AuthException _mapFirebaseError(FirebaseAuthException e) {
    final message = switch (e.code) {
      'user-not-found'         => 'No account found for this email.',
      'wrong-password'         => 'Incorrect password. Please try again.',
      'invalid-credential'     => 'Incorrect email or password. Please try again.',
      'email-already-in-use'   => 'An account already exists with this email.',
      'weak-password'          => 'Password must be at least 6 characters.',
      'invalid-email'          => 'Please enter a valid email address.',
      'network-request-failed' => 'Network error. Please check your connection.',
      'too-many-requests'      => 'Too many attempts. Please try again later.',
      'user-disabled'          => 'This account has been disabled.',
      'account-exists-with-different-credential' =>
        'An account already exists with a different sign-in method.',
      _ => 'Authentication failed. Please try again.',
    };
    return AuthException(message);
  }
}
