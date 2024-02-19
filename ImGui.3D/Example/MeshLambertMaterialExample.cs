using ImGuiExt.TK;
using THREE;

namespace ImGui3D.Three.Example;

public class MeshLambertMaterialExample : MeshMaterialExample
{
    public MeshLambertMaterialExample(ITkWindow view) : base(view)
    {
    }

    protected override void InitCamera()
    {
        base.InitCamera();
        camera.Position.X = -30;
        camera.Position.Y = 50;
        camera.Position.Z = 40;
        camera.LookAt(new Vector3(10, 0, 0));
    }

    public override void BuildMeshMaterial()
    {
        meshMaterial = new MeshLambertMaterial();
        meshMaterial.Color = Color.Hex(0x7777ff);
        meshMaterial.Name = "MeshLambertMaterial";
    }
}
