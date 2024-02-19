using ImGui3D.Three.Example;
using ImGuiExt;
using ImGuiExt.TK;
using ImGuiNET;
using THREE;
using Vector2 = System.Numerics.Vector2;

namespace ImGui3D.Three;

public class ModelExample : ThreeExample
{
    public ModelExample(ITkWindow view) : base(view)
    {
    }

    protected override void Resize(System.Drawing.Size clientSize)
    {
        base.Resize(clientSize);
        camera.Aspect = this.view.AspectRatio;
        camera.UpdateProjectionMatrix();
    }

    #region Initialize

    protected override void InitRenderer()
    {
        base.InitRenderer();
        this.renderer.SetClearColor(new Color().SetHex(0x000000));
        this.renderer.ShadowMap.Enabled = true;
        this.renderer.ShadowMap.type = Constants.PCFSoftShadowMap;
    }

    protected override void InitCamera()
    {
        camera.Fov = 45.0f;
        camera.Aspect = this.view.AspectRatio;
        camera.Near = 0.1f;
        camera.Far = 1000.0f;
        camera.Position.Set(10, 10, 10);
        camera.LookAt(new Vector3(0, 0, 0));
    }

    protected override void InitCameraController()
    {
        trackball = new TrackballControls(this.view, this.camera);
        trackball.StaticMoving = false;
        trackball.RotateSpeed = 3.0f;
        trackball.ZoomSpeed = 2;
        trackball.PanSpeed = 2;
        trackball.NoZoom = false;
        trackball.NoPan = false;
        trackball.NoRotate = false;
        trackball.StaticMoving = true;
        trackball.DynamicDampingFactor = 0.2f;
    }

    /// <inheritdoc/>
    public override void InitImGui()
    {
        base.InitImGui();
        if (imgui is Sdl2ImGuiContext_Ext ext) {
            ext.Action = ImGuiLayout;
            ext.OverlayOpacity = 0.4f;
        }
        else if (imgui is Sdl2ImGuiContext ctx) {
            ctx.OnLayoutUpdate += ImGuiLayout;
        }
    }

    private bool ImGuiLayout()
    {
        OnImGuiUpdate();
        AddCameraInfo();
        return true;
    }

    protected virtual void OnImGuiUpdate() { }

    #endregion

    #region Actions

    protected enum Location
    {
        center = -2,
        custom = -1,
        top_left,
        top_right,
        bottom_left,
        bottom_right,
    };

    protected static bool StartOverlay(string name, ref bool p_open, Location loc = Location.top_left, float alpha = 0.35f)
    {
        ImGuiWindowFlags window_flags = ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize
            | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNav
            | ImGuiWindowFlags.NoBringToFrontOnFocus;
        if (loc >= 0) {
            const float PAD = 10.0f;
            var viewport = ImGui.GetMainViewport();
            var work_pos = viewport.WorkPos;  // Use work area to avoid menu-bar/task-bar, if any!
            var work_size = viewport.WorkSize;
            Vector2 window_pos = new();
            Vector2 window_pos_pivot = new();
            window_pos.X = ((int)loc & 1) > 0 ? (work_pos.X + work_size.X - PAD) : (work_pos.X + PAD);
            window_pos.Y = ((int)loc & 2) > 0 ? (work_pos.Y + work_size.Y - PAD) : (work_pos.Y + PAD);
            window_pos_pivot.X = ((int)loc & 1) > 0 ? 1.0f : 0.0f;
            window_pos_pivot.Y = ((int)loc & 2) > 0 ? 1.0f : 0.0f;
            ImGui.SetNextWindowPos(window_pos, ImGuiCond.Always, window_pos_pivot);
            window_flags |= ImGuiWindowFlags.NoMove;
        }
        ImGui.SetNextWindowBgAlpha(alpha);
        return ImGui.Begin(name, ref p_open, window_flags);
    }

    protected virtual void AddCameraInfo()
    {
        bool open = true;
        if (StartOverlay("Frame Info", ref open, Location.bottom_right)) {
            ImGui.Separator();
            ImGui.Text($"视角上向: {camera.Up.X:F3}, {camera.Up.Y:F3}, {camera.Up.Z:F3}");
            ImGui.Text($"视角位置: {camera.Position.X:F3}, {camera.Position.Y:F3}, {camera.Position.Z:F3}");
            ImGui.End();
        }
    }

    #endregion
}
