#pragma once
#include <array>
#include <cstddef>
#include <cstdint>
#include <span>
#include <variant>

namespace grootscape::protocol {
inline constexpr std::uint16_t version = 1;
inline constexpr std::size_t max_message_length = 1024;

enum class Platform : std::uint8_t {
    windows = 1,
    linux = 2,
    macos = 3,
    android = 4,
    web = 5
};

enum class RejectReason : std::uint8_t {
    unsupported_version = 1,
    malformed = 2
};

enum class DecodeError : std::uint8_t {
    empty,
    unknown_opcode,
    truncated,
    trailing_bytes,
    invalid_value,
    unsupported_version
};

struct Welcome {
    std::uint16_t tick_duration_ms;
    bool operator==(const Welcome &) const = default;
};

struct Reject {
    RejectReason reason;
    bool operator==(const Reject &) const = default;
};

using ServerMessage = std::variant<Welcome, Reject, DecodeError>;
[[nodiscard]] constexpr std::array<std::uint8_t, 4> encode_hello(Platform platform) noexcept {
    return {
        1,
        static_cast<std::uint8_t>(version >> 8U),
        static_cast<std::uint8_t>(version & 0xFFU),
        static_cast<std::uint8_t>(platform)
    };
}

[[nodiscard]] ServerMessage decode_server(std::span<const std::uint8_t> bytes) noexcept;
} // namespace grootscape::protocol
