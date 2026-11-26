use std::{fmt, num::NonZeroU32};

/// The color the renderer clears each frame to before scene content exists.
const CLEAR_COLOR: wgpu::Color = wgpu::Color {
    r: 0.05,
    g: 0.11,
    b: 0.08,
    a: 1.0,
};

/// A drawable surface size in physical pixels. Both dimensions are non-zero.
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub struct SurfaceSize {
    width: NonZeroU32,
    height: NonZeroU32,
}

impl SurfaceSize {
    /// Returns `None` when either dimension is zero, as for a minimized window.
    pub fn new(width: u32, height: u32) -> Option<Self> {
        Some(Self {
            width: NonZeroU32::new(width)?,
            height: NonZeroU32::new(height)?,
        })
    }

    pub fn width(self) -> u32 {
        self.width.get()
    }

    pub fn height(self) -> u32 {
        self.height.get()
    }
}

/// The outcome of one [`Renderer::render`] call.
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum Frame {
    /// A frame was presented.
    Presented,
    /// The surface was not ready. The host should request another frame later.
    Skipped,
}

/// A failure that prevents the renderer from starting or continuing.
#[derive(Debug)]
pub enum RendererError {
    CreateSurface(wgpu::CreateSurfaceError),
    RequestAdapter(wgpu::RequestAdapterError),
    RequestDevice(wgpu::RequestDeviceError),
    UnsupportedSurface,
    SurfaceLost,
}

impl fmt::Display for RendererError {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        match self {
            Self::CreateSurface(error) => write!(f, "could not create a surface: {error}"),
            Self::RequestAdapter(error) => write!(f, "no suitable GPU adapter: {error}"),
            Self::RequestDevice(error) => write!(f, "could not open the GPU device: {error}"),
            Self::UnsupportedSurface => {
                f.write_str("the GPU adapter cannot present to the surface")
            }
            Self::SurfaceLost => f.write_str("the surface was lost"),
        }
    }
}

impl std::error::Error for RendererError {}

/// Owns the GPU device and the presentation surface for one host surface.
#[derive(Debug)]
pub struct Renderer {
    surface: wgpu::Surface<'static>,
    device: wgpu::Device,
    queue: wgpu::Queue,
    config: wgpu::SurfaceConfiguration,
    adapter_info: wgpu::AdapterInfo,
}

impl Renderer {
    /// Opens a GPU device that can present to `target` at `size`.
    ///
    /// Desktop hosts pass their windowing system's display handle in `instance`. In the browser the
    /// renderer uses WebGPU when the browser supports it and falls back to WebGL 2.
    pub async fn new(
        instance: wgpu::InstanceDescriptor,
        target: impl Into<wgpu::SurfaceTarget<'static>>,
        size: SurfaceSize,
    ) -> Result<Self, RendererError> {
        let instance = wgpu::util::new_instance_with_webgpu_detection(instance).await;
        let surface = instance
            .create_surface(target)
            .map_err(RendererError::CreateSurface)?;
        let adapter = instance
            .request_adapter(&wgpu::RequestAdapterOptions {
                compatible_surface: Some(&surface),
                ..Default::default()
            })
            .await
            .map_err(RendererError::RequestAdapter)?;
        let (device, queue) = adapter
            .request_device(&wgpu::DeviceDescriptor {
                label: Some("grootscape"),
                required_limits: wgpu::Limits::downlevel_webgl2_defaults()
                    .using_resolution(adapter.limits()),
                ..Default::default()
            })
            .await
            .map_err(RendererError::RequestDevice)?;
        let config = surface
            .get_default_config(&adapter, size.width(), size.height())
            .ok_or(RendererError::UnsupportedSurface)?;
        surface.configure(&device, &config);

        Ok(Self {
            surface,
            device,
            queue,
            config,
            adapter_info: adapter.get_info(),
        })
    }

    /// Describes the adapter and backend the renderer runs on.
    pub fn adapter_info(&self) -> &wgpu::AdapterInfo {
        &self.adapter_info
    }

    /// Reconfigures the surface after the host surface changes size.
    pub fn resize(&mut self, size: SurfaceSize) {
        self.config.width = size.width();
        self.config.height = size.height();
        self.surface.configure(&self.device, &self.config);
    }

    /// Draws and presents one frame.
    pub fn render(&mut self) -> Result<Frame, RendererError> {
        let texture = match self.surface.get_current_texture() {
            wgpu::CurrentSurfaceTexture::Success(texture) => texture,
            wgpu::CurrentSurfaceTexture::Suboptimal(texture) => {
                drop(texture);
                self.surface.configure(&self.device, &self.config);
                return Ok(Frame::Skipped);
            }
            wgpu::CurrentSurfaceTexture::Outdated => {
                self.surface.configure(&self.device, &self.config);
                return Ok(Frame::Skipped);
            }
            wgpu::CurrentSurfaceTexture::Timeout
            | wgpu::CurrentSurfaceTexture::Occluded
            | wgpu::CurrentSurfaceTexture::Validation => return Ok(Frame::Skipped),
            wgpu::CurrentSurfaceTexture::Lost => return Err(RendererError::SurfaceLost),
        };

        let view = texture
            .texture
            .create_view(&wgpu::TextureViewDescriptor::default());
        let mut encoder = self
            .device
            .create_command_encoder(&wgpu::CommandEncoderDescriptor {
                label: Some("frame"),
            });
        encoder.begin_render_pass(&wgpu::RenderPassDescriptor {
            label: Some("clear"),
            color_attachments: &[Some(wgpu::RenderPassColorAttachment {
                view: &view,
                depth_slice: None,
                resolve_target: None,
                ops: wgpu::Operations {
                    load: wgpu::LoadOp::Clear(CLEAR_COLOR),
                    store: wgpu::StoreOp::Store,
                },
            })],
            ..Default::default()
        });
        self.queue.submit([encoder.finish()]);
        self.queue.present(texture);
        Ok(Frame::Presented)
    }
}

#[cfg(test)]
mod tests {
    use super::SurfaceSize;

    #[test]
    fn surface_size_rejects_zero_dimensions() {
        assert_eq!(SurfaceSize::new(0, 600), None);
        assert_eq!(SurfaceSize::new(800, 0), None);
    }

    #[test]
    fn surface_size_keeps_dimensions() {
        let size = SurfaceSize::new(800, 600).unwrap();
        assert_eq!((size.width(), size.height()), (800, 600));
    }
}
