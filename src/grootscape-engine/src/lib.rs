pub const VERSION: &str = env!("CARGO_PKG_VERSION");

#[cfg(target_os = "macos")]
mod macos;
