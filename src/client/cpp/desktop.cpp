#include "grootscape/cli.hpp"
#include "grootscape/renderer.hpp"
#include "grootscape/version.h"
#include <SDL3/SDL_main.h>
#include <iostream>
#include <memory>
#include <vector>
namespace {
struct Video {
    Video() {
        if (!SDL_Init(SDL_INIT_VIDEO))
            throw std::runtime_error(SDL_GetError());
    }
    ~Video() { SDL_Quit(); }
    Video(const Video &) = delete;
    Video &operator=(const Video &) = delete;
};
} // namespace
int main(int argc, char **argv) try {
    std::vector<std::string_view> args;
    for (int i = 1; i < argc; ++i)
        args.emplace_back(argv[i]);
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
    try {
        const Video video;
        const std::unique_ptr<SDL_Window, decltype(&SDL_DestroyWindow)> window(
            SDL_CreateWindow("Grootscape", 1280, 720,
                             SDL_WINDOW_RESIZABLE | SDL_WINDOW_HIGH_PIXEL_DENSITY),
            SDL_DestroyWindow);
        if (!window)
            throw std::runtime_error(SDL_GetError());
        grootscape::Renderer renderer(*window);
        std::cout << "rendering with " << renderer.backend() << '\n';
        std::uint32_t frames = 0;
        while (true) {
            SDL_Event event;
            while (SDL_PollEvent(&event))
                if (event.type == SDL_EVENT_QUIT)
                    return 0;
            if (renderer.render()) {
                if (options.frame_limit != 0 && ++frames >= options.frame_limit) {
                    std::cout << "presented " << frames << " frames\n";
                    return 0;
                }
            } else {
                SDL_Delay(16);
            }
        }
    } catch (const std::exception &error) {
        std::cerr << error.what() << '\n';
        return 1;
    }
}

catch (const std::exception &error) {
    std::cerr << error.what() << '\n';
    return 1;
}
