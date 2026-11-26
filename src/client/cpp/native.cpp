#include "grootscape/native.h"
#include "grootscape/version.h"
#if defined(__ANDROID__)
#include <jni.h>
extern "C" JNIEXPORT jstring JNICALL Java_dev_grootscape_Engine_version(JNIEnv *env, jclass) {
    return env->NewStringUTF(GROOTSCAPE_VERSION);
}
#endif
const char *grootscape_engine_version() { return GROOTSCAPE_VERSION; }
