use std::{ffi::OsString, fmt, num::NonZeroU32};

pub(crate) const USAGE: &str = "\
Usage: grootscape-client [--exit-after-frames <count>]
       grootscape-client --version | --help

Options:
  --exit-after-frames <count>  Close the window after presenting <count> frames.
  --version                    Print the client version.
  --help                       Print this message.";

#[derive(Debug, PartialEq, Eq)]
pub(crate) enum Command {
    Run(RunOptions),
    Version,
    Help,
}

#[derive(Debug, Default, PartialEq, Eq)]
pub(crate) struct RunOptions {
    pub(crate) exit_after_frames: Option<NonZeroU32>,
}

#[derive(Debug, PartialEq, Eq)]
pub(crate) struct ParseError(String);

impl fmt::Display for ParseError {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        f.write_str(&self.0)
    }
}

pub(crate) fn parse(args: impl IntoIterator<Item = OsString>) -> Result<Command, ParseError> {
    let mut args = args.into_iter();
    let mut options = RunOptions::default();

    while let Some(arg) = args.next() {
        match arg.to_str() {
            Some("--version") if options == RunOptions::default() => {
                return only(Command::Version, args);
            }
            Some("--help") if options == RunOptions::default() => return only(Command::Help, args),
            Some("--exit-after-frames") if options.exit_after_frames.is_none() => {
                let count = args
                    .next()
                    .and_then(|value| value.to_str()?.parse().ok())
                    .ok_or_else(|| {
                        ParseError("--exit-after-frames needs a positive count".into())
                    })?;
                options.exit_after_frames = Some(count);
            }
            _ => {
                return Err(ParseError(format!(
                    "unexpected argument {}",
                    arg.to_string_lossy()
                )));
            }
        }
    }

    Ok(Command::Run(options))
}

fn only(command: Command, mut rest: impl Iterator<Item = OsString>) -> Result<Command, ParseError> {
    match rest.next() {
        None => Ok(command),
        Some(arg) => Err(ParseError(format!(
            "unexpected argument {}",
            arg.to_string_lossy()
        ))),
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    fn parse_strs(args: &[&str]) -> Result<Command, ParseError> {
        parse(args.iter().map(OsString::from))
    }

    #[test]
    fn no_arguments_runs_until_closed() {
        assert_eq!(parse_strs(&[]), Ok(Command::Run(RunOptions::default())));
    }

    #[test]
    fn reads_frame_limit() {
        assert_eq!(
            parse_strs(&["--exit-after-frames", "3"]),
            Ok(Command::Run(RunOptions {
                exit_after_frames: NonZeroU32::new(3)
            }))
        );
    }

    #[test]
    fn rejects_zero_or_missing_frame_limit() {
        assert!(parse_strs(&["--exit-after-frames", "0"]).is_err());
        assert!(parse_strs(&["--exit-after-frames"]).is_err());
    }

    #[test]
    fn version_and_help_stand_alone() {
        assert_eq!(parse_strs(&["--version"]), Ok(Command::Version));
        assert_eq!(parse_strs(&["--help"]), Ok(Command::Help));
        assert!(parse_strs(&["--version", "--help"]).is_err());
        assert!(parse_strs(&["--exit-after-frames", "1", "--version"]).is_err());
    }

    #[test]
    fn rejects_unknown_arguments() {
        assert!(parse_strs(&["--bogus"]).is_err());
    }
}
