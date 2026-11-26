//! Native interfaces that app hosts written in other languages link against.
//!
//! Apple hosts use the C interface declared in `include/grootscape.h`. The Android host uses the
//! JNI methods of `dev.grootscape.Engine`.

#[cfg(target_os = "android")]
pub mod android;
#[cfg(not(target_os = "android"))]
pub mod c_abi;
