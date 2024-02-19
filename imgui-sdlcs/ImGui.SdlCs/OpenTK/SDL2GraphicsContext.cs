using ImGuiExt.SDL;
using OpenTK.Windowing.Common;
using Silk.NET.SDL;

namespace ImGuiExt.TK;

public unsafe class SDL2GraphicsContext : IGraphicsContext
{
    private readonly SDL2Window View;

    public SDL2GraphicsContext(SDL2Window view)
    {
        this.View = view;
    }

    public bool IsCurrent => SDL2Helper.SDLCS.GLGetCurrentWindow() == (Window*)this.View.HWnd;

    public int SwapInterval
    {
        get => SDL2Helper.SDLCS.GLGetSwapInterval();
        set => SDL2Helper.SDLCS.GLSetSwapInterval(value);
    }

    public void SwapBuffers()
    {
        SDL2Helper.SDLCS.GLSwapWindow((Window*)this.View.HWnd);
    }

    public void MakeCurrent()
    {
        SDL2Helper.SDLCS.GLMakeCurrent((Window*)this.View.HWnd, this.View.GLContext);
    }

    public void MakeNoneCurrent()
    {
        SDL2Helper.SDLCS.GLMakeCurrent((Window*)this.View.HWnd, null);
    }
}
