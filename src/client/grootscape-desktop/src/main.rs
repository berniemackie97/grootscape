//! Grootscape desktop client for Windows, Linux, and macOS.

#![forbid(unsafe_code)]

mod app;
mod cli;

use std::process::ExitCode;

use cli::Command;

fn main() -> ExitCode {
    match cli::parse(std::env::args_os().skip(1)) {
        Ok(Command::Version) => {
            println!("grootscape-client {}", grootscape_engine::VERSION);
            ExitCode::SUCCESS
        }
        Ok(Command::Help) => {
            println!("{}", cli::USAGE);
            ExitCode::SUCCESS
        }
        Ok(Command::Run(options)) => {
            env_logger::Builder::from_env(env_logger::Env::default().default_filter_or("info"))
                .init();
            match app::run(options) {
                Ok(()) => ExitCode::SUCCESS,
                Err(error) => {
                    log::error!("{error}");
                    ExitCode::FAILURE
                }
            }
        }
        Err(error) => {
            eprintln!("{error}\n{}", cli::USAGE);
            ExitCode::from(2)
        }
    }
}
