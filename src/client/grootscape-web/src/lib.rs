//! WebAssembly bindings that let the browser host run the Grootscape engine.
//!
//! The browser host in `apps/web` owns the page, the canvas, and the frame and resize callbacks.
//! It forwards them to a [`WebClient`]. The crate is empty on targets other than WebAssembly.

#![cfg(target_family = "wasm")]
#![forbid(unsafe_code)]

use grootscape_engine::{Frame, Renderer, SurfaceSize};
use wasm_bindgen::prelude::*;
use web_sys::HtmlCanvasElement;

/// Installs a panic hook that reports Rust panics to the browser console.
#[wasm_bindgen(start)]
fn init() {
    console_error_panic_hook::set_once();
}

/// The engine version, for display and diagnostics.
#[wasm_bindgen]
pub fn version() -> String {
    grootscape_engine::VERSION.to_owned()
}

/// One engine instance bound to a canvas.
#[wasm_bindgen]
#[derive(Debug)]
pub struct WebClient {
    renderer: Renderer,
}

#[wasm_bindgen]
impl WebClient {
    /// Opens a renderer on `canvas`, preferring WebGPU and falling back to WebGL 2.
    pub async fn start(canvas: HtmlCanvasElement) -> Result<WebClient, JsError> {
        let size = surface_size(canvas.width(), canvas.height())?;
        let renderer = Renderer::new(
            wgpu::InstanceDescriptor::new_without_display_handle(),
            wgpu::SurfaceTarget::Canvas(canvas),
            size,
        )
        .await
        .map_err(|error| JsError::new(&error.to_string()))?;
        Ok(Self { renderer })
    }

    /// The GPU backend in use, such as `BrowserWebGpu` or `Gl`.
    #[wasm_bindgen(getter)]
    pub fn backend(&self) -> String {
        format!("{:?}", self.renderer.adapter_info().backend)
    }

    /// Resizes the drawing surface to the canvas size in device pixels.
    pub fn resize(&mut self, width: u32, height: u32) -> Result<(), JsError> {
        self.renderer.resize(surface_size(width, height)?);
        Ok(())
    }

    /// Draws one frame. Returns `false` when the surface was not ready.
    pub fn render(&mut self) -> Result<bool, JsError> {
        self.renderer
            .render()
            .map(|frame| frame == Frame::Presented)
            .map_err(|error| JsError::new(&error.to_string()))
    }
}

fn surface_size(width: u32, height: u32) -> Result<SurfaceSize, JsError> {
    SurfaceSize::new(width, height).ok_or_else(|| JsError::new("canvas size must be non-zero"))
}
