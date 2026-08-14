plugins {
    id("com.android.application")
    // START: FlutterFire Configuration
    id("com.google.gms.google-services")
    // END: FlutterFire Configuration
    id("kotlin-android")
    // The Flutter Gradle Plugin must be applied after the Android and Kotlin Gradle plugins.
    id("dev.flutter.flutter-gradle-plugin")
}

// Phase 4L.6A release authorities. The checked-in Firebase client is still
// registered for the legacy example ID, so a production ID must be supplied
// only after the matching Firebase Android client has been provisioned.
val firebaseRegisteredApplicationId = "com.example.antigravity_app"
val releaseApplicationId = providers.gradleProperty("APPSEL_ANDROID_APPLICATION_ID")
    .orElse(providers.environmentVariable("APPSEL_ANDROID_APPLICATION_ID"))
    .getOrElse(firebaseRegisteredApplicationId)
val releaseTaskRequested = gradle.startParameter.taskNames.any {
    it.contains("release", ignoreCase = true)
}
fun releaseSecret(name: String): String? = providers.gradleProperty(name)
    .orElse(providers.environmentVariable(name))
    .orNull

val releaseStoreFile = releaseSecret("APPSEL_RELEASE_STORE_FILE")
val releaseStorePassword = releaseSecret("APPSEL_RELEASE_STORE_PASSWORD")
val releaseKeyAlias = releaseSecret("APPSEL_RELEASE_KEY_ALIAS")
val releaseKeyPassword = releaseSecret("APPSEL_RELEASE_KEY_PASSWORD")

android {
    namespace = "com.example.antigravity_app"
    compileSdk = flutter.compileSdkVersion
    ndkVersion = "27.0.12077973"

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_11
        targetCompatibility = JavaVersion.VERSION_11
    }

    kotlinOptions {
        jvmTarget = JavaVersion.VERSION_11.toString()
    }

    defaultConfig {
        applicationId = releaseApplicationId
        // You can update the following values to match your application needs.
        // For more information, see: https://flutter.dev/to/review-gradle-config.
        // Firebase Auth 6.5.4 declares Android API 23 as its minimum. API
        // 21-22 devices are therefore intentionally outside the supported
        // platform set; never bypass this with manifest-merger overrides.
        minSdk = 23
        targetSdk = flutter.targetSdkVersion
        versionCode = flutter.versionCode
        versionName = flutter.versionName
    }

    signingConfigs {
        create("release") {
            if (releaseTaskRequested) {
                require(!releaseStoreFile.isNullOrBlank()) {
                    "Release signing requires APPSEL_RELEASE_STORE_FILE."
                }
                require(!releaseStorePassword.isNullOrBlank()) {
                    "Release signing requires APPSEL_RELEASE_STORE_PASSWORD."
                }
                require(!releaseKeyAlias.isNullOrBlank()) {
                    "Release signing requires APPSEL_RELEASE_KEY_ALIAS."
                }
                require(!releaseKeyPassword.isNullOrBlank()) {
                    "Release signing requires APPSEL_RELEASE_KEY_PASSWORD."
                }
                storeFile = file(releaseStoreFile)
                storePassword = releaseStorePassword
                keyAlias = releaseKeyAlias
                keyPassword = releaseKeyPassword
            }
        }
    }

    buildTypes {
        release {
            signingConfig = signingConfigs.getByName("release")
        }
    }
}

flutter {
    source = "../.."
}
