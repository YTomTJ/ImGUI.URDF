using ImGuiExt;
using ImGuiExt.SDL;
using ImGuiExt.TK;
using ImGuiNET;
using System.Drawing;
using THREE;

namespace ImGui3D.Three.Example;

public abstract class ThreeExample
{
    public ITkWindow view { get; }       // Required
    public GLRenderer renderer { get; }  // Required
    public Scene scene { get; }          // Required
    public Camera camera { get; }        // Required

    public TrackballControls? trackball { get; set; }
    protected Sdl2ImGuiContext? imgui { get; set; }

    public ThreeExample(ITkWindow view)
    {
        this.view = view;
        view.OnResized += View_OnResized;

        this.renderer = new GLRenderer();
        this.scene = new Scene();
        this.camera = new PerspectiveCamera();
    }

    private void View_OnResized(IWindow win, object? e)
    {
        this.Resize(win.Size);
    }

    public void Initialize()
    {
        InitRenderer();
        InitCamera();
        InitCameraController();
        this.Resize(view.Size);
    }

    /// <summary>
    /// 初始ImGui界面
    /// </summary>
    public virtual void InitImGui()
    {
        var im = new Sdl2ImGuiContext_Ext(this.view, 0);
        im.OnWindowStart += (win) => {
            var io = ImGui.GetIO();
            var fonts = io.Fonts;
            fonts.AddFontFromFileTTF("NotoSansSC-Regular.ttf", 18, null, fonts.GetGlyphRangesChineseFull());
        };
        im.ShouldSwapBuffer = false;
        im.BackgroundColor = null;
        im.Mode = LayoutMode.SinglePass;
        this.imgui = im;
    }

    public virtual void FrameUpdate()
    {
        if (trackball is not null) {
            trackball.Enabled = !(imgui?.ImWantMouse) ?? true;
            trackball.Update();
        }
        renderer!.Render(scene, camera);
    }

    protected virtual void InitRenderer()
    {
        this.renderer.Context = view.GContext;
        this.renderer.Width = (int)view.Size.Width;
        this.renderer.Height = (int)view.Size.Height;
        this.renderer.Init();
    }

    protected abstract void InitCamera();

    protected abstract void InitCameraController();

    protected virtual void Resize(Size clientSize)
    {
        this.renderer?.Resize(clientSize.Width, clientSize.Height);
    }
}
