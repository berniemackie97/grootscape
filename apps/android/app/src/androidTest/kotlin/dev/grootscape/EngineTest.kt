package dev.grootscape

import androidx.test.core.app.ActivityScenario
import androidx.test.ext.junit.runners.AndroidJUnit4
import android.widget.TextView
import org.junit.Assert.assertEquals
import org.junit.Test
import org.junit.runner.RunWith

/** Runs on a device or emulator and proves the packaged C++ engine loads through JNI. */
@RunWith(AndroidJUnit4::class)
class EngineTest {
    @Test
    fun nativeEngineReportsTheAppVersion() {
        assertEquals(BuildConfig.VERSION_NAME, Engine.version())
    }

    @Test
    fun launchActivityShowsTheEngineVersion() {
        ActivityScenario.launch(MainActivity::class.java).use { scenario ->
            scenario.onActivity { activity ->
                val label = activity.window.decorView.findViewById<android.view.ViewGroup>(android.R.id.content)
                    .getChildAt(0) as TextView
                assertEquals("Grootscape ${Engine.version()}", label.text.toString())
            }
        }
    }
}
