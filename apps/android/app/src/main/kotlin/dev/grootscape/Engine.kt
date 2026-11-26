package dev.grootscape

/** JNI bindings to the C++ engine, implemented by grootscape_native. */
object Engine {
    init {
        System.loadLibrary("grootscape_native")
    }

    /** The engine build version. */
    @JvmStatic
    external fun version(): String
}
