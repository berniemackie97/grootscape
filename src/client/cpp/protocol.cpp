#include "grootscape/protocol.hpp"
namespace grootscape::protocol {
ServerMessage decode_server(std::span<const std::uint8_t> bytes) noexcept {
    if (bytes.empty())
        return DecodeError::empty;
    if (bytes[0] == 2) {
        if (bytes.size() < 3)
            return DecodeError::truncated;
        if (bytes[1] != 0 || bytes[2] != version)
            return DecodeError::unsupported_version;
        if (bytes.size() < 5)
            return DecodeError::truncated;
        if (bytes.size() > 5)
            return DecodeError::trailing_bytes;
        return Welcome{
            static_cast<std::uint16_t>((static_cast<unsigned>(bytes[3]) << 8U) | bytes[4])};
    }
    if (bytes[0] == 3) {
        if (bytes.size() < 2)
            return DecodeError::truncated;
        if (bytes[1] < 1 || bytes[1] > 2)
            return DecodeError::invalid_value;
        if (bytes.size() > 2)
            return DecodeError::trailing_bytes;
        return Reject{static_cast<RejectReason>(bytes[1])};
    }
    return DecodeError::unknown_opcode;
}
} // namespace grootscape::protocol
