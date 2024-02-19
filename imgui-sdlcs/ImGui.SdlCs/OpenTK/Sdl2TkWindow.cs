using ImGuiExt.SDL;
using OpenTK.Graphics.OpenGL;
using Silk.NET.SDL;
using System.Diagnostics;

namespace ImGuiExt.TK;

public class Sdl2TkWindow : SDL2Window, ITkWindow
{
    public static readonly System.Numerics.Vector4 DefaultClearColor = new(0.4f, 0.5f, 0.6f, 1.0f);

    public ImGuiTkContext TkContext { get; private set; }

    public SDL2GraphicsContext GContext { get; private set; }
    public System.Numerics.Vector4 BackgroundColor { get; set; } = DefaultClearColor;

    public Sdl2TkWindow(string title = "OpenTK with SDL2(OpenGL)", int width = 1280, int height = 760,
        WindowFlags flags = WindowFlags.Opengl | WindowFlags.Resizable | WindowFlags.Shown)
        : base(title, width, height, flags)
    {
        this.OnStart += OpenTkInit;
        this.OnExit += OpenTkDeinit;

        // Initialize OpenTK
        this.TkContext = new ImGuiTkContext();
        GL.LoadBindings(this.TkContext);
        OpenTK.Graphics.ES30.GL.LoadBindings(this.TkContext);
        this.GContext = new SDL2GraphicsContext(this);
    }

    protected virtual void OpenTkInit(IWindow win)
    {
    }

    protected virtual void OpenTkDeinit(IWindow win)
    {
    }

    protected virtual void OpenTkRender(int wdith, int height)
    {
    }

    protected unsafe override void LoopHandler()
    {
        Debug.Assert(this.Wnd != null);

        int w = (int)this.Size.Width;
        int h = (int)this.Size.Height;
        var lastViewport = new int[4];
        GL.GetInteger(GetPName.Viewport, lastViewport);
        GL.Viewport(0, 0, w, h);

        OpenTkRender(w, h);
        base.LoopHandler();

        GL.Viewport(lastViewport[0], lastViewport[1], lastViewport[2], lastViewport[3]);
        SDL2Helper.SDLCS.GLSwapWindow(this.Wnd);
    }
}
