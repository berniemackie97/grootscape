#![forbid(unsafe_code)]

use std::{env, ffi::OsStr, process::ExitCode};

const USAGE: &str = "Usage: grootscape-client [--help | --version]";

fn main() -> ExitCode {
    let mut args = env::args_os().skip(1);

    match (args.next().as_deref(), args.next()) {
        (Some(arg), None) if arg == OsStr::new("--version") => {
            println!("grootscape-client {}", env!("CARGO_PKG_VERSION"));
            ExitCode::SUCCESS
        }
        (arg, None) if arg.is_none() || arg == Some(OsStr::new("--help")) => {
            println!("{USAGE}");
            ExitCode::SUCCESS
        }
        _ => {
            eprintln!("{USAGE}");
            ExitCode::from(2)
        }
    }
}
