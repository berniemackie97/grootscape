//! Window and event-loop lifecycle for the desktop host.

use std::{fmt, sync::Arc};

use grootscape_engine::{Frame, Renderer, RendererError, SurfaceSize};
use winit::{
    application::ApplicationHandler,
    dpi::LogicalSize,
    error::{EventLoopError, OsError},
    event::WindowEvent,
    event_loop::{ActiveEventLoop, EventLoop},
    window::{Window, WindowId},
};

use crate::cli::RunOptions;

const INITIAL_SIZE: LogicalSize<u32> = LogicalSize::new(1280, 720);

#[derive(Debug)]
pub(crate) enum AppError {
    EventLoop(EventLoopError),
    Window(OsError),
    Renderer(RendererError),
}

impl fmt::Display for AppError {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        match self {
            Self::EventLoop(error) => write!(f, "event loop failed: {error}"),
            Self::Window(error) => write!(f, "could not open the window: {error}"),
            Self::Renderer(error) => write!(f, "renderer failed: {error}"),
        }
    }
}

/// Runs the client until the window closes or the frame limit is reached.
pub(crate) fn run(options: RunOptions) -> Result<(), AppError> {
    let event_loop = EventLoop::new().map_err(AppError::EventLoop)?;
    let mut app = App {
        options,
        session: None,
        frames_presented: 0,
        error: None,
    };
    event_loop.run_app(&mut app).map_err(AppError::EventLoop)?;
    app.error.map_or(Ok(()), Err)
}

struct App {
    options: RunOptions,
    session: Option<Session>,
    frames_presented: u32,
    error: Option<AppError>,
}

struct Session {
    window: Arc<Window>,
    renderer: Renderer,
}

impl App {
    fn fail(&mut self, event_loop: &ActiveEventLoop, error: AppError) {
        self.error = Some(error);
        event_loop.exit();
    }

    fn open(event_loop: &ActiveEventLoop) -> Result<Session, AppError> {
        let window = Arc::new(
            event_loop
                .create_window(
                    Window::default_attributes()
                        .with_title("Grootscape")
                        .with_inner_size(INITIAL_SIZE),
                )
                .map_err(AppError::Window)?,
        );
        let physical = window.inner_size();
        let size = SurfaceSize::new(physical.width, physical.height)
            .unwrap_or_else(|| SurfaceSize::new(1, 1).expect("1x1 is non-zero"));
        let instance = wgpu::InstanceDescriptor::new_with_display_handle(Box::new(
            event_loop.owned_display_handle(),
        ));
        let renderer = pollster::block_on(Renderer::new(instance, window.clone(), size))
            .map_err(AppError::Renderer)?;
        let info = renderer.adapter_info();
        log::info!(
            "rendering with {} on {:?} ({:?})",
            info.name,
            info.backend,
            info.device_type
        );
        Ok(Session { window, renderer })
    }
}

impl ApplicationHandler for App {
    fn resumed(&mut self, event_loop: &ActiveEventLoop) {
        if self.session.is_some() {
            return;
        }
        match Self::open(event_loop) {
            Ok(session) => {
                session.window.request_redraw();
                self.session = Some(session);
            }
            Err(error) => self.fail(event_loop, error),
        }
    }

    fn window_event(&mut self, event_loop: &ActiveEventLoop, _: WindowId, event: WindowEvent) {
        let Some(session) = self.session.as_mut() else {
            return;
        };
        match event {
            WindowEvent::CloseRequested => event_loop.exit(),
            WindowEvent::Resized(physical) => {
                if let Some(size) = SurfaceSize::new(physical.width, physical.height) {
                    session.renderer.resize(size);
                    session.window.request_redraw();
                }
            }
            WindowEvent::RedrawRequested => match session.renderer.render() {
                Ok(frame) => {
                    if frame == Frame::Presented {
                        self.frames_presented += 1;
                    }
                    let limit_reached = self
                        .options
                        .exit_after_frames
                        .is_some_and(|limit| self.frames_presented >= limit.get());
                    if limit_reached {
                        log::info!("presented {} frames", self.frames_presented);
                        event_loop.exit();
                    } else {
                        session.window.request_redraw();
                    }
                }
                Err(error) => self.fail(event_loop, AppError::Renderer(error)),
            },
            _ => {}
        }
    }
}
