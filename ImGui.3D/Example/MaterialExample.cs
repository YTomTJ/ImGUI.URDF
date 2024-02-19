using ImGuiExt;
using ImGuiExt.TK;
using ImGuiNET;
using THREE;

namespace ImGui3D.Three.Example;

public class MaterialExample : ThreeExample
{
    public MaterialExample(ITkWindow view) : base(view)
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
        camera.Position.Set(0, 20, 40);
        camera.LookAt(new Vector3(10, 0, 0));
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
        }
        else if (imgui is Sdl2ImGuiContext ctx) {
            ctx.OnLayoutUpdate += ImGuiLayout;
        }
    }

    private bool ImGuiLayout()
    {
        foreach (var item in materialsLib) {
            AddBasicMaterialSettings(item.Value, item.Key + "-THREE.Material");
            AddSpecificMaterialSettings(item.Value, item.Key + "-THREE.MeshStandardMaterial");
        }
        return true;
    }

    #endregion

    #region Actions

    private int wireframeLinejoinIndex = 0;
    private int wireframeLinecapIndex = 0;

    protected Dictionary<string, Material> materialsLib = new Dictionary<string, Material>();

    protected virtual void AddBasicMaterialSettings(Material material, string name)
    {
        int currentSide = material.Side;
        int shadowSide = material.ShadowSide == null ? 0 : material.ShadowSide.Value;
        if (ImGui.TreeNode(name)) {
            ImGui.Text($"id={material.Id}");
            ImGui.Text($"uuid={material.Uuid}");
            ImGui.Text($"name={material.Name}");
            ImGui.SliderFloat("opacity", ref material.Opacity, 0.0f, 1.0f);
            ImGui.Checkbox("transparent", ref material.Transparent);
            ImGui.Checkbox("visible", ref material.Visible);
            if (ImGui.Combo("side", ref currentSide, "FrontSide\0BackSide\0BothSide\0")) {
                material.Side = currentSide;
            }
            ImGui.Checkbox("colorWrite", ref material.ColorWrite);
            if (ImGui.Checkbox("flatShading", ref material.FlatShading)) {
                material.NeedsUpdate = true;
            }
            ImGui.Checkbox("premultipliedAlpha", ref material.PremultipliedAlpha);
            ImGui.Checkbox("dithering", ref material.Dithering);
            if (ImGui.Combo("shadowSide", ref shadowSide, "FrontSide\0BackSide\0BothSide\0")) {
                material.ShadowSide = shadowSide;
            }
            ImGui.Checkbox("fog", ref material.Fog);
            ImGui.TreePop();
        }
    }

    protected virtual void AddColorPicker(Material material)
    {
        System.Numerics.Vector3 color = new();
        if (material.Color is not null) {
            color = new System.Numerics.Vector3(material.Color.Value.R, material.Color.Value.G, material.Color.Value.B);
        }
        if (ImGui.ColorPicker3("color", ref color)) {
            material.Color = new Color(color.X, color.Y, color.Z);
        }
    }

    protected virtual void AddEmissivePicker(Material material)
    {
    }

    protected virtual void AddSpecularPicker(Material material)
    {
    }

    protected virtual void AddShiness(Material material)
    {
    }

    protected virtual void AddRoughness(Material material)
    {
    }

    protected virtual void AddMetalness(Material material)
    {
    }

    protected virtual void AddWireframeProperty(Material material)
    {
        ImGui.Checkbox("wireframe", ref material.Wireframe);
        ImGui.SliderFloat("wireframeLineWidth", ref material.WireframeLineWidth, 0, 20);
    }

    protected virtual void AddWireframeLineProperty(Material material)
    {
        if (ImGui.Combo("wireframeLinejoin", ref wireframeLinejoinIndex, "round\0bevel\0miter\0")) {
            if (wireframeLinejoinIndex == 0) material.WireframeLineJoin = "round";
            else if (wireframeLinejoinIndex == 1) material.WireframeLineJoin = "bevel";
            else material.WireframeLineJoin = "miter";
        }
        if (ImGui.Combo("wireframeLinecap", ref wireframeLinecapIndex, "butt\0round\0square\0")) {
            if (wireframeLinecapIndex == 0) material.WireframeLineCap = "round";
            else if (wireframeLinecapIndex == 1) material.WireframeLineCap = "bevel";
            else material.WireframeLineCap = "miter";
        }
    }

    protected virtual void AddSpecificMaterialSettings(Material material, string name)
    {
        Color? materialColor = material.Color;
        Color? emissiveColor = material.Emissive;

        if (ImGui.TreeNode(name)) {
            if (materialColor != null)
                AddColorPicker(material);
            if (emissiveColor != null)
                AddEmissivePicker(material);
            AddSpecularPicker(material);
            ImGui.TreePop();
        }
        AddShiness(material);
        AddMetalness(material);
        AddRoughness(material);
        AddWireframeProperty(material);
        AddWireframeLineProperty(material);
    }

    #endregion
}
