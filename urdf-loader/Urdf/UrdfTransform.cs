using THREE;

namespace URDFLoader;

#pragma warning disable CS8625
public class UrdfTransform : ObjectBase
{
    public UrdfTransform Parent
    {
        get => new UrdfTransform(Instance.Parent);
        set => Instance.Parent = value.Instance;
    }

    public Vector3 Position
    {
        get => Instance.Position;
        set => Instance.Position = value;
    }

    public Euler Rotation
    {
        get => Instance.Rotation;
        set => Instance.Rotation = value;
    }

    public Quaternion Quaternion
    {
        get => Instance.Quaternion;
        set => Instance.Quaternion = value;
    }

    public Vector3 Scale
    {
        get => Instance.Scale;
        set => Instance.Scale = value;
    }

    public Matrix4 Matrix
    {
        get => Instance.Matrix;
    }

    public UrdfTransform(Matrix4? tf = null) : this(new(), tf)
    {
    }

    public UrdfTransform(Object3D obj, Matrix4? tf = null) : base(obj)
    {
        if (tf is not null) {
            this.Instance.ApplyMatrix4(tf);
        }
    }

    public void AddChild(UrdfObject obj)
    {
        obj.Instance.Parent = this.Instance;
    }

    public void RemoveChild(UrdfObject obj)
    {
        obj.Instance.Parent = null;
    }

    public void Reset()
    {
        this.Instance.ApplyMatrix4(Matrix4.Identity());
    }
}
