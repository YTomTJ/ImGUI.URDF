using ImGuiExt.TK;
using ImGuiNET;
using THREE;

namespace ImGui3D.Three.Example;

public class MeshMaterialExample : MaterialExample
{
    public Mesh plane, cube, sphere;
    public Object3D selectedMesh;
    public Group gopher;
    public AmbientLight ambientLight;
    public SpotLight spotLight;
    public Material meshMaterial;

    public int selectedIndex = 0;
    public float step = 0;
    public float rotationSpeed = 0.001f;

#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
    public MeshMaterialExample(ITkWindow view) : base(view)
#pragma warning restore CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
    {
        BuildGeometry();
    }

    public virtual void BuildMeshMaterial()
    {
        meshMaterial = new MeshBasicMaterial() {
            Color = Color.Hex(0x7777ff),
            Name = "Basic Material",
            FlatShading = true,
            Opacity = 0.01f,
            ColorWrite = true,
            Fog = true
        };
    }

    public virtual void BuildGroundGeometry()
    {
        var groundGeometry = new PlaneGeometry(100, 100, 4, 4);
        var groundMesh = new Mesh(groundGeometry, new MeshBasicMaterial() { Color = Color.Hex(0x777777) });
        groundMesh.Rotation.X = (float)(-System.Math.PI / 2);
        groundMesh.Position.Y = -20;
        scene.Add(groundMesh);
    }

    public virtual void BuildMesh()
    {
        var sphereGeometry = new SphereGeometry(14, 20, 20);
        var cubeGeometry = new BoxGeometry(15, 15, 15);
        var planeGeometry = new PlaneGeometry(14, 14, 4, 4);
        sphere = new Mesh(sphereGeometry, meshMaterial);
        cube = new Mesh(cubeGeometry, meshMaterial);
        plane = new Mesh(planeGeometry, meshMaterial);

        sphere.Position.Set(0, 3, 2);
        cube.Position.Copy(sphere.Position);
        plane.Position.Copy(sphere.Position);
    }

    public virtual void BuildGopher()
    {
        OBJLoader loader = new OBJLoader();
        gopher = loader.Load(@"gopher.obj");

        ComputeNormalsGroup(gopher);

        SetMaterialGroup(meshMaterial, gopher);

        gopher.Scale.Set(4, 4, 4);
    }

    public virtual void AddAmbientLight()
    {
        ambientLight = new AmbientLight(Color.Hex(0x0c0c0c));
        scene.Add(ambientLight);
    }

    public virtual void AddSpotLight()
    {
        spotLight = new SpotLight(Color.Hex(0xffffff));
        spotLight.Position.Set(-40, 60, -10);
        spotLight.CastShadow = true;
        scene.Add(spotLight);
    }

    public virtual void BuildGeometry()
    {
        BuildGroundGeometry();
        BuildMeshMaterial();
        BuildMesh();
        BuildGopher();

        materialsLib.Add(meshMaterial.Name, meshMaterial);

        scene.Add(cube);
        selectedMesh = cube;

        AddAmbientLight();
        AddSpotLight();

    }

    public override void FrameUpdate()
    {
        base.FrameUpdate();
        step += 0.001f;
        selectedMesh.Rotation.Y = step;
    }

    public void ComputeNormalsGroup(Group group)
    {
        group.Traverse(o => {
            if (o is Mesh) {
                var tempGeom = new Geometry();
                tempGeom.FromBufferGeometry((BufferGeometry)o.Geometry);
                tempGeom.ComputeFaceNormals();
                tempGeom.MergeVertices();
                tempGeom.ComputeFlatVertexNormals();

                tempGeom.NormalsNeedUpdate = true;

                o.Geometry = tempGeom;
            }
        });
    }

    public void SetMaterialGroup(Material material, Group group)
    {
        group.Traverse(o => {
            o.Material = material;
            if (o is Mesh && o.Materials.Count > 1) {
                for (var i = 0; i < o.Materials.Count; i++)
                    o.Materials[i] = material;
            }
        });
    }

    protected override void AddSpecificMaterialSettings(Material material, string name)
    {
        base.AddSpecificMaterialSettings(material, name);

        if (ImGui.Combo("SelectedObject", ref selectedIndex, "Cube\0Sphere\0Plane\0Gopher\0")) {
            scene.Remove(plane);
            scene.Remove(cube);
            scene.Remove(sphere);
            scene.Remove(gopher);

            switch (selectedIndex) {
                case 0:
                    scene.Add(cube);
                    selectedMesh = cube;
                    break;
                case 1:
                    scene.Add(sphere);
                    selectedMesh = sphere;
                    break;
                case 2:
                    scene.Add(plane);
                    selectedMesh = plane;
                    break;
                case 3:
                    scene.Add(gopher);
                    selectedMesh = gopher;
                    break;
            }
        }
    }
}
