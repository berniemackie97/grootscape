#pragma once
#include <SDL3/SDL.h>
#include <memory>
namespace grootscape {
// The host owns the window. Destroy the renderer before the window, on the host thread.
class Renderer final {
    struct Destroy {
        void operator()(SDL_Renderer *renderer) const noexcept;
    };
    std::unique_ptr<SDL_Renderer, Destroy> renderer_;
    SDL_Window *window_;

  public:
    explicit Renderer(SDL_Window &window);
    [[nodiscard]] bool render();
    [[nodiscard]] const char *backend() const noexcept;
};
} // namespace grootscape
