#ifndef GROOTSCAPE_H
#define GROOTSCAPE_H

// C interface to the Grootscape engine, implemented by the grootscape-native crate.

// Returns the engine version as a nul-terminated UTF-8 string with static lifetime.
const char * _Nonnull grootscape_engine_version(void);

#endif
