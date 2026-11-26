plugins {
    alias(libs.plugins.android.application)
}

val minSdkVersion = 28
val repositoryRoot: File = rootDir.resolve("../..").canonicalFile
val rustJniLibs = layout.buildDirectory.dir("rustJniLibs")
val rustAbis = listOf("arm64-v8a", "x86_64")

android {
    namespace = "dev.grootscape"
    compileSdk = 36

    defaultConfig {
        applicationId = "dev.grootscape"
        minSdk = minSdkVersion
        targetSdk = 36
        versionCode = 1
        versionName = "0.1.0"
        ndk { abiFilters += rustAbis }
    }

    sourceSets.getByName("main").jniLibs.directories.add(rustJniLibs.get().asFile.path)
}

kotlin {
    jvmToolchain(17)
}

// Builds the Rust engine for each Android ABI with cargo-ndk before the app is packaged.
val buildRustLibraries = tasks.register<Exec>("buildRustLibraries") {
    description = "Builds libgrootscape_native.so for each Android ABI."
    workingDir = repositoryRoot
    inputs.dir(repositoryRoot.resolve("src/client"))
    inputs.file(repositoryRoot.resolve("Cargo.lock"))
    outputs.dir(rustJniLibs)
    commandLine(
        listOf("cargo", "ndk", "--platform", minSdkVersion.toString()) +
            rustAbis.flatMap { listOf("--target", it) } +
            listOf("--output-dir", rustJniLibs.get().asFile.path) +
            listOf("build", "--locked", "--release", "--package", "grootscape-native"),
    )
}

tasks.named("preBuild") { dependsOn(buildRustLibraries) }
