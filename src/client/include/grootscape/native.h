#pragma once
#ifdef __cplusplus
extern "C" {
#endif
// Returns a nul-terminated UTF-8 string with static lifetime. Never free it.
const char *grootscape_engine_version(void);
#ifdef __cplusplus
}
#endif
