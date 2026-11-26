//! Shared Grootscape client engine.
//!
//! Platform hosts own windows, surfaces, input, and app lifecycle. They hand the engine a surface
//! target and forward lifecycle events to it.

#![forbid(unsafe_code)]

mod renderer;

pub use renderer::{Frame, Renderer, RendererError, SurfaceSize};

/// The engine build version, shared by every client.
pub const VERSION: &str = env!("CARGO_PKG_VERSION");
