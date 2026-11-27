#pragma once
#include <charconv>
#include <cstdint>
#include <span>
#include <stdexcept>
#include <string_view>
#include <system_error>

namespace grootscape {
struct Options {
    enum class Command : std::uint8_t {
        run,
        version,
        help
    }
    command = Command::run;
    std::uint32_t frame_limit = 0;
};
inline Options parse_options(std::span<const std::string_view> args) {
    if (args.empty()) {
        return {};
    }
    if (args.size() == 1 && args[0] == "--version") {
        return {Options::Command::version, 0};
    }
    if (args.size() == 1 && args[0] == "--help") {
        return {Options::Command::help, 0};
    }
    if (args.size() == 2 && args[0] == "--exit-after-frames") {
        std::uint32_t count = 0;
        if (const auto [end, error] = std::from_chars(args[1].data(), args[1].data() + args[1].size(), count);
            error == std::errc{} && end == args[1].data() + args[1].size() && count > 0) {
            return {Options::Command::run, count};
        }
    }
    throw std::invalid_argument(
        "expected --version, --help, or --exit-after-frames <positive count>");
}
} // namespace grootscape
