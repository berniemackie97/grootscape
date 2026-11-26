package dev.grootscape

import android.app.Activity
import android.os.Bundle
import android.view.Gravity
import android.widget.TextView

/** Hosts the engine and owns the Android activity lifecycle. */
class MainActivity : Activity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(
            TextView(this).apply {
                text = getString(R.string.engine_version, Engine.version())
                textSize = 20f
                gravity = Gravity.CENTER
            },
        )
    }
}
