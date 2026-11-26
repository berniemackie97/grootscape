#include "grootscape/protocol.hpp"
#include <charconv>
#include <iomanip>
#include <iostream>
#include <string_view>
#include <vector>
int main(int argc, char **argv) try {
    using namespace grootscape::protocol;
    if (argc == 1) {
        for (auto byte : encode_hello(Platform::linux))
            std::cout << std::hex << std::setw(2) << std::setfill('0')
                      << static_cast<unsigned>(byte);
        return 0;
    }
    if (argc != 2)
        return 2;
    const std::string_view hex(argv[1]);
    if (hex.size() % 2 != 0 || hex.size() > max_message_length * 2)
        return 2;
    std::vector<std::uint8_t> bytes;
    for (std::size_t i = 0; i < hex.size(); i += 2) {
        unsigned byte = 0;
        const auto [end, error] = std::from_chars(hex.data() + i, hex.data() + i + 2, byte, 16);
        if (error != std::errc{} || end != hex.data() + i + 2)
            return 2;
        bytes.push_back(static_cast<std::uint8_t>(byte));
    }
    const auto message = decode_server(bytes);
    if (const auto *welcome = std::get_if<Welcome>(&message)) {
        std::cout << welcome->tick_duration_ms;
        return 0;
    }
    return 1;
}

catch (const std::exception &error) {
    std::cerr << error.what() << '\n';
    return 1;
}
