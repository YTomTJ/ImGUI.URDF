using ImGui3D.Three.Example;
using ImGuiExt.TK;
using Silk.NET.SDL;

namespace ImGui3D.Three;

public class ThreeTkWindow : Sdl2TkWindow
{
    private ThreeExample? _Exp;
    public ThreeExample? Exp
    {
        get => _Exp;
        set {
            _Exp = value;
        }
    }

    public ThreeTkWindow(string title = "Window for Three.Net by OpenTK with SDL2(OpenGL)", int width = 1280, int height = 760,
        WindowFlags flags = WindowFlags.Opengl | WindowFlags.Resizable | WindowFlags.Shown)
        : base(title, width, height, flags)
    {
    }

    protected override unsafe void OpenTkRender(int w, int h)
    {
        Exp?.FrameUpdate();
    }
}
