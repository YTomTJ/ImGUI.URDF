using THREE;

namespace URDFLoader;

public class ObjectBase : IObject3D
{
    public void AddToScene(Scene scene)
    {
        scene.Add(this.Instance);
    }

    public void RemoveFromScene(Scene scene)
    {
        scene.Remove(this.Instance);
    }

    internal Object3D Instance { get; }

    protected ObjectBase(Object3D instance)
    {
        this.Instance = instance;
    }
}
