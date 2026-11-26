use std::ffi::{CStr, c_char};

// Every client crate inherits the workspace version, so this matches `grootscape_engine::VERSION`.
const C_VERSION: &CStr =
    match CStr::from_bytes_with_nul(concat!(env!("CARGO_PKG_VERSION"), "\0").as_bytes()) {
        Ok(version) => version,
        Err(_) => panic!("Cargo package version must be a C string"),
    };

/// Declared in `include/grootscape.h`. The string has static lifetime.
#[unsafe(no_mangle)]
pub extern "C" fn grootscape_engine_version() -> *const c_char {
    C_VERSION.as_ptr()
}

#[cfg(test)]
mod tests {
    use std::ffi::CStr;

    #[test]
    fn version_matches_engine() {
        // SAFETY: the function returns a pointer to a static, nul-terminated string.
        let version = unsafe { CStr::from_ptr(super::grootscape_engine_version()) };
        assert_eq!(version.to_str(), Ok(grootscape_engine::VERSION));
    }
}
