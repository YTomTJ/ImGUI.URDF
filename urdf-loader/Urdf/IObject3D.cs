using THREE;

namespace URDFLoader;

public interface IObject3D
{
    public void AddToScene(Scene scene);

    public void RemoveFromScene(Scene scene);
}
