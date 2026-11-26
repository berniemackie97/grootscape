plugins { alias(libs.plugins.android.application) }
val repositoryRoot = rootProject.layout.projectDirectory.dir("../..")
val engineVersion = providers.fileContents(repositoryRoot.file("VERSION")).asText.map { it.trim() }
android {
    namespace = "dev.grootscape"
    compileSdk = 36
    ndkVersion = "29.0.14206865"
    defaultConfig {
        applicationId = "dev.grootscape"
        minSdk = 28
        targetSdk = 36
        versionCode = 1
        versionName = engineVersion.get()
        ndk { abiFilters += listOf("arm64-v8a", "x86_64") }
        testInstrumentationRunner = "androidx.test.runner.AndroidJUnitRunner"
        externalNativeBuild {
            cmake {
                arguments += listOf("-DGROOTSCAPE_RENDERER=OFF", "-DBUILD_TESTING=OFF")
                targets += "grootscape_native"
            }
        }
    }
    externalNativeBuild {
        cmake {
            path = repositoryRoot.file("CMakeLists.txt").asFile
            version = "3.31.6"
        }
    }
    buildFeatures { buildConfig = true }
}
kotlin { jvmToolchain(17) }
dependencies {
    androidTestImplementation(libs.androidx.test.junit)
    androidTestImplementation(libs.androidx.test.runner)
    androidTestImplementation(libs.junit)
}
