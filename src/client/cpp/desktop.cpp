#include "grootscape/cli.hpp"
#include "grootscape/renderer.hpp"
#include "grootscape/version.h"

#include <SDL3/SDL.h>
#include <SDL3/SDL_main.h>

#include <cstddef>
#include <cstdint>
#include <exception>
#include <iostream>
#include <memory>
#include <stdexcept>
#include <string_view>
#include <vector>

namespace {
struct Video {
    Video() {
        if (!SDL_Init(SDL_INIT_VIDEO)) {
            const std::runtime_error error(SDL_GetError());
            SDL_Quit();
            throw error;
        }
    }
    ~Video() { SDL_Quit(); }
    Video(const Video &) = delete;
    Video &operator=(const Video &) = delete;
};
} // namespace
int main(int argc, char **argv) try {
    std::vector<std::string_view> args;
    args.reserve(static_cast<std::size_t>(argc > 0 ? argc - 1 : 0));

    for (int i = 1; i < argc; ++i) {
        args.emplace_back(argv[i]);
    }
    grootscape::Options options;
    try {
        options = grootscape::parse_options(args);
    } catch (const std::invalid_argument &error) {
        std::cerr << error.what() << '\n';
        return 2;
    }
    if (options.command == grootscape::Options::Command::version) {
        std::cout << "grootscape-client " GROOTSCAPE_VERSION "\n";
        return 0;
    }
    if (options.command == grootscape::Options::Command::help) {
        std::cout << "grootscape-client [--version | --help | --exit-after-frames <count>]\n";
        return 0;
    }

    const Video video;
    const std::unique_ptr<SDL_Window, decltype(&SDL_DestroyWindow)> window(
        SDL_CreateWindow("Grootscape", 1280, 720,
                         SDL_WINDOW_RESIZABLE | SDL_WINDOW_HIGH_PIXEL_DENSITY),
        SDL_DestroyWindow);

    if (!window) {
        throw std::runtime_error(SDL_GetError());
    }
    grootscape::Renderer renderer(*window);
    if (const char *renderer_name = renderer.backend(); renderer_name != nullptr) {
        std::cout << "rendering with " << renderer_name << '\n';
    } else {
        std::cerr << "Could not determine renderer name: " << SDL_GetError() << '\n';
    }
    std::uint32_t frames = 0;
    while (true) {
        SDL_Event event;
        while (SDL_PollEvent(&event)) {
            if (event.type == SDL_EVENT_QUIT) {
                return 0;
            }
        }

        if (!renderer.render()) {
            SDL_Delay(16);
            continue;
        }

        if (options.frame_limit != 0 && ++frames >= options.frame_limit) {
            std::cout << "presented " << frames << " frames\n";
            return 0;
        }
    }
}

catch (const std::exception &error) {
    std::cerr << error.what() << '\n';
    return 1;
}
