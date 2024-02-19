using THREE;

namespace URDFLoader;

public class UrdfObject : ObjectBase
{
    public string Name
    {
        get => Instance.Name;
        set => Instance.Name = value;
    }

    public Material Material
    {
        get => Instance.Material;
        set => Instance.Material = value;
    }

    public Matrix4 Matrix
    {
        get => Instance.Matrix;
        set => Instance.ApplyMatrix4(value);
    }

    public UrdfObject(Mesh obj) : base(obj)
    {
    }
}
