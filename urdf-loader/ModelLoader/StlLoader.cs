using System.Text;
using THREE;

namespace URDFLoader;

#pragma warning disable CS8625

/// <summary>
/// Loads the Binary STL format.
/// </summary>
/// <seealso cref="https://en.wikipedia.org/wiki/STL_(file_format)"/>
public class StlLoader
{
    const int MAX_VERTEX_COUNT = 65000;

    /// <summary>
    /// Returns an array of meshes used to build up the given STL
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    public static Mesh[] Load(string fileName)
    {
        return Parse(File.ReadAllBytes(fileName));
    }

    /// <summary>
    /// Parse the file into meshes
    /// </summary>
    /// <param name="bytes"></param>
    /// <returns></returns>
    public static Mesh[] Parse(byte[] bytes)
    {
        // Read and throw out the header
        string fileType = Encoding.ASCII.GetString(bytes, 0, 5);

        // Check if ASCII or binary
        if (fileType == "solid") {
            string content = Encoding.ASCII.GetString(bytes);
            string[] lines = content.Split('\n');
            return LoadAscii(lines);
        }
        else {
            return LoadBinary(bytes);
        }
    }

    /// <summary>
    /// Parse the ascii stl contents
    /// </summary>
    /// <param name="lines"></param>
    /// <returns></returns>
    public static Mesh[] LoadAscii(string[] lines)
    {
        List<Mesh> meshes = new List<Mesh>();
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<int> triangles = new List<int>();
        int currTri = 0;

        for (int i = 1; i < lines.Length; i++) {
            // "facet normal <x> <y> <z>" or
            // "endsolid <name>"
            string line = lines[i].Trim();
            string[] tokens = line.Split(' ');

            // finish if we hit the end of the file
            if (tokens[0] == "endsolid") {
                break;
            }

            // if a normal is provided
            Vector3 n = Vector3.Zero();
            if (tokens.Length == 5 && tokens[1] == "normal") {
                n = ReadAsciiVector(tokens[2], tokens[3], tokens[4]);
            }

            // "outer loop"
            i++;

            // iterate over the vertices
            for (int j = 0; j < 3; j++) {
                // "vertex <x> <y> <z>"
                i++;
                string vline = lines[i].Trim();
                string[] vtokens = vline.Split(' ');

                Vector3 vertex = ReadAsciiVector(vtokens[1], vtokens[2], vtokens[3]);
                vertices.Add(vertex);
                normals.Add(n);
            }

            triangles.Add(currTri + 0);
            triangles.Add(currTri + 1);
            triangles.Add(currTri + 2);
            currTri += 3;

            // get to "endloop"
            while (!lines[i].Contains("endloop")) i++;

            // "endfacet"
            i++;

            if (vertices.Count > MAX_VERTEX_COUNT - 1) {
                Mesh newMesh = ToMesh(vertices, normals, triangles);
                meshes.Add(newMesh);

                vertices.Clear();
                normals.Clear();
                triangles.Clear();
                currTri = 0;
            }
        }

        if (vertices.Count > 0) {
            Mesh newMesh = ToMesh(vertices, normals, triangles);
            meshes.Add(newMesh);
        }

        return meshes.ToArray();
    }

    /// <summary>
    /// Load a binary implementation
    /// </summary>
    /// <param name="bytes"></param>
    /// <returns></returns>
    public static Mesh[] LoadBinary(byte[] bytes)
    {
        // Some implementation referenced from
        // https://forum.unity.com/threads/stl-file-and-vertex-normals.159844/

        MemoryStream memoryStream = new MemoryStream(bytes);
        BinaryReader reader = new BinaryReader(memoryStream);
        reader.ReadBytes(80);

        uint trianglesCount = reader.ReadUInt32();
        Mesh[] meshes = new Mesh[(int)MathF.Ceiling(trianglesCount * 3.0f / MAX_VERTEX_COUNT)];

        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<int> triangles = new List<int>();
        int currTri = 0;
        int meshIndex = 0;

        for (int i = 0; i < trianglesCount; i++) {
            Vector3 n = ReadBinaryVector(reader);

            for (int j = 0; j < 3; j++) {
                Vector3 v = ReadBinaryVector(reader);
                vertices.Add(v);
                normals.Add(n);
            }

            triangles.Add(currTri + 0);
            triangles.Add(currTri + 1);
            triangles.Add(currTri + 2);
            currTri += 3;

            // Attribute byte count
            // assume unused
            reader.ReadUInt16();

            if (vertices.Count > MAX_VERTEX_COUNT - 1) {
                meshes[meshIndex++] = ToMesh(vertices, normals, triangles);

                vertices.Clear();
                normals.Clear();
                triangles.Clear();
                currTri = 0;
            }
        }

        if (vertices.Count > 0) {
            meshes[meshIndex++] = ToMesh(vertices, normals, triangles);
        }

        return meshes;
    }

    #region Utilities

    /// <summary>
    /// Read a vector from the binary reader
    /// </summary>
    /// <param name="br"></param>
    /// <returns></returns>
    static Vector3 ReadBinaryVector(BinaryReader br)
    {
        return new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
    }

    static Vector3 ReadAsciiVector(string x, string y, string z)
    {
        return new Vector3(-float.Parse(y), float.Parse(z), float.Parse(x));
    }

    /// <summary>
    /// Convert the verts, normals, and triangles to a mesh
    /// </summary>
    /// <param name="vertices"></param>
    /// <param name="normals"></param>
    /// <param name="triangles"></param>
    /// <returns></returns>
    public static Mesh ToMesh(List<Vector3> vertices, List<Vector3> normals, List<int> triangles)
    {
        if (triangles.Count % 3 != 0) {
            throw new ArgumentException("The size of triangles must be times of 3.");
        }
        var facets = new List<Face3>();
        for (int i = 0; i < triangles.Count / 3; i++) {
            facets.Add(new Face3(
                a: triangles[3 * i + 0],
                b: triangles[3 * i + 1],
                c: triangles[3 * i + 2]
            ));
        }

        THREE.Geometry geom = new() {
            Vertices = vertices.ToList(),
            Faces = facets.ToList(),
            Normals = normals.ToList()
        };
        geom.ComputeBoundingBox();
        return new Mesh(geom, material: null);
    }

    #endregion
}
