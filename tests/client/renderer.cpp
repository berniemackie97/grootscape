#include "grootscape/renderer.hpp"
#include <cmath>
#include <iostream>
#include <memory>
#include <stdexcept>
int main() {
    if (!SDL_Init(SDL_INIT_VIDEO))
        return 1;
    int result = 0;
    try {
        const std::unique_ptr<SDL_Window, decltype(&SDL_DestroyWindow)> window(
            SDL_CreateWindow("Renderer test", 64, 64, SDL_WINDOW_HIDDEN | SDL_WINDOW_RESIZABLE),
            SDL_DestroyWindow);
        if (!window)
            throw std::runtime_error(SDL_GetError());
        for (int cycle = 0; cycle < 3; ++cycle) {
            grootscape::Renderer renderer(*window);
            if (!renderer.render())
                throw std::runtime_error("frame skipped");
            const std::unique_ptr<SDL_Surface, decltype(&SDL_DestroySurface)> pixels(
                SDL_RenderReadPixels(SDL_GetRenderer(window.get()), nullptr), SDL_DestroySurface);
            if (!pixels)
                throw std::runtime_error(SDL_GetError());
            float red = 0, green = 0, blue = 0, alpha = 0;
            if (!SDL_ReadSurfacePixelFloat(pixels.get(), 0, 0, &red, &green, &blue, &alpha))
                throw std::runtime_error(SDL_GetError());
            if (std::abs(red - 0.05F) > 0.01F || std::abs(green - 0.11F) > 0.01F ||
                std::abs(blue - 0.08F) > 0.01F || alpha < 0.99F)
                throw std::runtime_error("clear pixel mismatch");
            if (!SDL_SetWindowSize(window.get(), 128, 96) || !renderer.render())
                throw std::runtime_error("resize failed");
        }
    } catch (const std::exception &error) {
        std::cerr << error.what() << '\n';
        result = 1;
    }
    SDL_Quit();
    return result;
}
