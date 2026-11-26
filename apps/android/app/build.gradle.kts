plugins {
    alias(libs.plugins.android.application)
}

val minSdkVersion = 28
val engineNdkVersion = "29.0.14206865"
val rustAbis = listOf("arm64-v8a", "x86_64")
val repositoryRoot = rootProject.layout.projectDirectory.dir("../..")
val rustJniLibs = layout.buildDirectory.dir("rustJniLibs")

// The Cargo workspace version is the single source of the client version on every platform.
val engineVersion = providers.fileContents(repositoryRoot.file("Cargo.toml")).asText.map { manifest ->
    Regex("""(?m)^version = "([^"]+)"""").find(manifest)?.groupValues?.get(1)
        ?: error("Cargo.toml has no workspace version")
}

android {
    namespace = "dev.grootscape"
    compileSdk = 36
    // AGP installs this NDK when it is missing; cargo-ndk links the engine with it.
    ndkVersion = engineNdkVersion

    defaultConfig {
        applicationId = "dev.grootscape"
        minSdk = minSdkVersion
        targetSdk = 36
        versionCode = 1
        versionName = engineVersion.get()
        ndk { abiFilters += rustAbis }
        testInstrumentationRunner = "androidx.test.runner.AndroidJUnitRunner"
    }

    buildFeatures { buildConfig = true }

    sourceSets.getByName("main").jniLibs.directories.add(rustJniLibs.get().asFile.path)
}

kotlin {
    jvmToolchain(17)
}

dependencies {
    androidTestImplementation(libs.androidx.test.junit)
    androidTestImplementation(libs.androidx.test.runner)
    androidTestImplementation(libs.junit)
}

val buildRustLibraries = tasks.register<CargoNdkBuild>("buildRustLibraries") {
    description = "Builds libgrootscape_native.so for each Android ABI."
    workspaceDirectory = repositoryRoot
    sources = repositoryRoot.dir("src/client")
    lockfile = repositoryRoot.file("Cargo.lock")
    ndkDirectory = androidComponents.sdkComponents.ndkDirectory
    ndkVersion = engineNdkVersion
    packageName = "grootscape-native"
    abis = rustAbis
    platform = minSdkVersion
    outputDirectory = rustJniLibs
}

tasks.named("preBuild") { dependsOn(buildRustLibraries) }
