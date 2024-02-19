using THREE;

namespace URDFLoader;
public class DAELoader
{
    /// <summary>
    /// loads all meshes associated with the dae file
    /// </summary>
    /// <param name="data">should be the string contents of the dae file</param>
    /// <param name="textures">a collection of the names of the textures associated with the meshes, if there are no texture or you do not care about them pass string[0]</param>
    /// <returns></returns>
    public static Mesh[] Load(string data, ref string[] textures)
    {
        var cLite = new ColladaLite(data);
        var Meshes = cLite.meshes.ToArray();
        if (textures.Length > 0) {
            textures = cLite.textureNames.ToArray();
        }
        return Meshes;
    }

    /// <summary>
    /// loads all meshes associated with the dae file
    /// </summary>
    /// <param name="data">should be the path to the dae file</param>
    /// <param name="textures">a collection of the names of the textures associated with the meshes, if there are no texture or you do not care about them pass string[0]</param>
    /// <returns></returns>
    public static Mesh[] LoadFromPath(string data, ref string[] textures)
    {
        if (!File.Exists(data)) {
            throw new Exception("File not found at " + data);
        }
        var cLite = new ColladaLite(File.ReadAllText(data));
        var Meshes = cLite.meshes.ToArray();
        if (textures.Length > 0) {
            textures = cLite.textureNames.ToArray();
        }
        return Meshes;
    }
}
