#include "grootscape/renderer.hpp"
#include "grootscape/version.h"
#include <SDL3/SDL.h>
#include <emscripten/emscripten.h>
#include <exception>
#include <memory>
#include <stdexcept>
#include <string>

namespace {
std::unique_ptr<SDL_Window, decltype(&SDL_DestroyWindow)> window(nullptr, SDL_DestroyWindow);
std::unique_ptr<grootscape::Renderer> renderer;
std::string error;
} // namespace
extern "C" {
EMSCRIPTEN_KEEPALIVE const char *client_version() { return GROOTSCAPE_VERSION; }
EMSCRIPTEN_KEEPALIVE const char *client_error() { return error.c_str(); }
EMSCRIPTEN_KEEPALIVE const char *client_backend() {
    if (!renderer) {
        return "unavailable";
    }

    const char *backend = renderer->backend();
    return backend != nullptr ? backend : "unavailable";
}
EMSCRIPTEN_KEEPALIVE void client_stop() {
    renderer.reset();
    window.reset();
    SDL_Quit();
}
EMSCRIPTEN_KEEPALIVE int client_start(int width, int height) {
    error.clear();

    if (width <= 0 || height <= 0) {
        error = "canvas size must be positive";
        return 0;
    }
    client_stop();
    try {
        SDL_SetHint(SDL_HINT_EMSCRIPTEN_CANVAS_SELECTOR, "#game");
        if (!SDL_Init(SDL_INIT_VIDEO)) {
            throw std::runtime_error(SDL_GetError());
        }
        window.reset(SDL_CreateWindow("Grootscape", width, height, SDL_WINDOW_RESIZABLE));
        if (!window) {
            throw std::runtime_error(SDL_GetError());
        }
        renderer = std::make_unique<grootscape::Renderer>(*window);
        return 1;
    } catch (const std::exception &failure) {
        error = failure.what();
        client_stop();
        return 0;
    }
}
EMSCRIPTEN_KEEPALIVE int client_resize(int width, int height) {
    error.clear();

    if (!window) {
        error = "window is not running";
        return 0;
    }
    if (width <= 0 || height <= 0) {
        error = "canvas size must be positive";
        return 0;
    }
    if (!SDL_SetWindowSize(window.get(), width, height)) {
        error = SDL_GetError();
        return 0;
    }
    return 1;
}
EMSCRIPTEN_KEEPALIVE int client_render() {
    error.clear();

    if (!renderer) {
        error = "renderer is not running";
        return -1;
    }
    try {
        SDL_Event event;
        while (SDL_PollEvent(&event)) {
            if (event.type == SDL_EVENT_QUIT) {
                return 0;
            }
        }
        return renderer->render() ? 1 : 0;
    } catch (const std::exception &failure) {
        error = failure.what();
        return -1;
    }
}
}
int main() { return 0; }
