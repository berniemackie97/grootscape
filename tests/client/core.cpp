#include "grootscape/cli.hpp"
#include "grootscape/native.h"
#include "grootscape/version.h"
#include <array>
#include <iostream>
#include <string_view>
void test_fixtures();
int main() {
    try {
        test_fixtures();
        if (std::string_view(grootscape_engine_version()) != GROOTSCAPE_VERSION)
            throw std::runtime_error("C ABI version");
        if (grootscape::parse_options({}).frame_limit != 0)
            throw std::runtime_error("default options");
        const std::array<std::string_view, 2> valid{"--exit-after-frames", "3"};
        if (grootscape::parse_options(valid).frame_limit != 3)
            throw std::runtime_error("frame limit");
        for (const auto invalid : {"0", "-1", "", "3x", "4294967296"}) {
            const std::array<std::string_view, 2> args{"--exit-after-frames", invalid};
            bool rejected = false;
            try {
                (void)grootscape::parse_options(args);
            } catch (const std::invalid_argument &) {
                rejected = true;
            }
            if (!rejected)
                throw std::runtime_error("accepted invalid limit");
        }
        const std::array<std::string_view, 2> mixed{"--version", "--help"};
        bool rejected = false;
        try {
            (void)grootscape::parse_options(mixed);
        } catch (const std::invalid_argument &) {
            rejected = true;
        }
        if (!rejected)
            throw std::runtime_error("accepted mixed commands");
        std::cout << "Client contracts passed\n";
    } catch (const std::exception &error) {
        std::cerr << error.what() << '\n';
        return 1;
    }
}
