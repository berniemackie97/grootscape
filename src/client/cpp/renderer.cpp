#include "grootscape/renderer.hpp"
#include <stdexcept>
namespace grootscape {
void Renderer::Destroy::operator()(SDL_Renderer *renderer) const noexcept {
    SDL_DestroyRenderer(renderer);
}
Renderer::Renderer(SDL_Window &window)
    : renderer_(SDL_CreateRenderer(&window, nullptr)), window_(&window) {
    if (!renderer_)
        throw std::runtime_error(SDL_GetError());
    // VSync is best effort: software and headless drivers can reject it.
    SDL_SetRenderVSync(renderer_.get(), 1);
}
bool Renderer::render() {
    int width = 0, height = 0;
    if (!SDL_GetWindowSizeInPixels(window_, &width, &height))
        throw std::runtime_error(SDL_GetError());
    if (width <= 0 || height <= 0 || (SDL_GetWindowFlags(window_) & SDL_WINDOW_MINIMIZED) != 0)
        return false;
    if (!SDL_SetRenderDrawColorFloat(renderer_.get(), 0.05F, 0.11F, 0.08F, 1.0F) ||
        !SDL_RenderClear(renderer_.get()) || !SDL_RenderPresent(renderer_.get()))
        throw std::runtime_error(SDL_GetError());
    return true;
}
const char *Renderer::backend() const noexcept { return SDL_GetRendererName(renderer_.get()); }
} // namespace grootscape
