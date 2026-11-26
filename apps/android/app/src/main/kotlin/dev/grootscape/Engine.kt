package dev.grootscape

/** JNI bindings to the Rust engine, implemented by the grootscape-native crate. */
object Engine {
    init {
        System.loadLibrary("grootscape_native")
    }

    /** The engine build version. */
    @JvmStatic
    external fun version(): String
}
