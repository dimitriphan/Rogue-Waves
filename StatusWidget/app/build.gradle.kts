plugins {
    id("com.android.application")
    id("org.jetbrains.kotlin.android")
}

android {
    namespace = "com.roguewaves.status"
    compileSdk = 35

    defaultConfig {
        applicationId = "com.roguewaves.status"
        minSdk = 31
        targetSdk = 35
        versionCode = (System.getenv("GITHUB_RUN_NUMBER") ?: "1").toInt()
        versionName = "0.1.${System.getenv("GITHUB_RUN_NUMBER") ?: "0"}"
    }

    // One fixed key, kept beside the code, so each new APK installs over the last one (a sideloaded personal app;
    // anyone who can read the repository could sign an update, which is everyone who can already change the code).
    signingConfigs {
        create("sideload") {
            storeFile = file("sideload.keystore")
            storePassword = "roguewaves"
            keyAlias = "roguewaves"
            keyPassword = "roguewaves"
        }
    }
    buildTypes {
        release {
            isMinifyEnabled = false
            signingConfig = signingConfigs.getByName("sideload")
        }
        debug {
            signingConfig = signingConfigs.getByName("sideload")
        }
    }
    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
    }
    kotlinOptions { jvmTarget = "17" }
    lint { abortOnError = false }
}

dependencies {
    implementation("androidx.work:work-runtime-ktx:2.9.1")
    testImplementation("junit:junit:4.13.2")
}
