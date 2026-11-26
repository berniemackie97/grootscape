use std::ffi::{CStr, c_char};

const C_VERSION: &CStr =
    match CStr::from_bytes_with_nul(concat!(env!("CARGO_PKG_VERSION"), "\0").as_bytes()) {
        Ok(version) => version,
        Err(_) => panic!("Cargo package version must be a C string"),
    };

// This symbol is reserved by the Grootscape engine header. The string has static lifetime.
#[unsafe(no_mangle)]
pub extern "C" fn grootscape_engine_version() -> *const c_char {
    C_VERSION.as_ptr()
}
