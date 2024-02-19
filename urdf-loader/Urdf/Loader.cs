using System.Diagnostics;
using System.Xml;
using THREE;
using Color3 = THREE.Color;
using ColorB4 = System.Drawing.Color;
using Path = System.IO.Path;

namespace URDFLoader;

#pragma warning disable CS8600, CS8602

public struct Color
{
    public float R, G, B, A;
    public Color3 RGB => new Color3(R, G, B);

    public static Color FromSystem(ColorB4 col)
    {
        return new Color() {
            R = col.R / 255.0f,
            G = col.G / 255.0f,
            B = col.B / 255.0f,
            A = col.A / 255.0f,
        };
    }
}

public static class Loader
{
    private const string SINGLE_PACKAGE_KEY = "<DEFAULT>";

    public static RotationOrder ORDER { get; set; } = RotationOrder.XYZ;

    public struct Options
    {
        public string WorkPath;
        public Func<string, string, UrdfObject[]> MeshLoader;
        public UrdfRobot? target;
    }

    /// <summary>
    /// Load the URDF from file and build the robot
    /// Takes a single package path which is assumed to be the package location for all meshes.
    /// </summary>
    /// <param name="urdfPath"></param>
    /// <param name="package"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public static UrdfRobot? LoadUrdf(string urdfPath, string package = "", Options options = new Options())
    {
        Dictionary<string, string> packages = new Dictionary<string, string> {
            { SINGLE_PACKAGE_KEY, package }
        };
        return LoadUrdf(urdfPath, packages, options);
    }

    /// <summary>
    /// Takes a dictionary of packages
    /// </summary>
    /// <param name="urdfPath"></param>
    /// <param name="packages"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public static UrdfRobot? LoadUrdf(string urdfPath, Dictionary<string, string> packages, Options options = new Options())
    {
        if (Path.GetExtension(urdfPath).ToLower() != ".urdf") {
            return null;
        }
        StreamReader reader = new StreamReader(urdfPath);
        string content = reader.ReadToEnd();
        if (options.WorkPath == null) {
            Uri uri = new Uri(urdfPath);
            options.WorkPath = uri.Host + Path.GetDirectoryName(uri.PathAndQuery);
        }
        return ParseUrdf(content, packages, options);
    }

    /// <summary>
    /// Parse the URDF file and return a URDFRobot instance with all associated links and joints
    /// </summary>
    /// <param name="urdfPath"></param>
    /// <param name="package"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public static UrdfRobot ParseUrdf(string urdfPath, string package, Options options = new Options())
    {
        Dictionary<string, string> packages = new Dictionary<string, string> {
            { SINGLE_PACKAGE_KEY, package }
        };
        return ParseUrdf(urdfPath, packages, options);
    }

    public static UrdfRobot ParseUrdf(string urdfContent, Dictionary<string, string> packages, Options options = new Options())
    {
        if (options.MeshLoader == null) {
            options.MeshLoader = LoadMesh;
        }

        // Parse the XML doc
        XmlDocument doc = new XmlDocument();
        doc.LoadXml(urdfContent);

        // Store the information about the link and the objects indexed by link name
        Dictionary<string, XmlNode> xmlLinks = new Dictionary<string, XmlNode>();
        Dictionary<string, XmlNode> xmlJoints = new Dictionary<string, XmlNode>();

        // Indexed by joint name
        Dictionary<string, UrdfJoint> urdfJoints = new Dictionary<string, UrdfJoint>();
        Dictionary<string, UrdfLink> urdfLinks = new Dictionary<string, UrdfLink>();
        Dictionary<string, Material> urdfMaterials = new Dictionary<string, Material>();

        // Indexed by joint name
        Dictionary<string, string> parentNames = new Dictionary<string, string>();

        // First node is the <robot> node
        XmlNode robotNode = Helper.GetXmlNodeChildByName(doc, "robot");
        string robotName = robotNode.Attributes["name"].Value;

        XmlNode[] xmlLinksArray = Helper.GetXmlNodeChildrenByName(robotNode, "link");
        XmlNode[] xmlJointsArray = Helper.GetXmlNodeChildrenByName(robotNode, "joint");

        // Cycle through the links and instantiate the geometry
        foreach (XmlNode linkNode in xmlLinksArray) {
            // Store the XML node for the link
            string linkName = linkNode.Attributes["name"].Value;
            xmlLinks.Add(linkName, linkNode);

            // TODO: if any usage of inertial's origin
            //Matrix4 inertialOrigin = new Matrix4();
            //if (Helper.GetXmlNodeChildByName(linkNode, "inertial") is XmlNode inertialNode) {
            //    // Get the mesh and the origin nodes
            //    XmlNode originNode = Helper.GetXmlNodeChildByName(inertialNode, "origin");
            //
            //    // Extract the position and rotation of the mesh
            //    Vector3 position = Vector3.Zero();
            //    if (originNode != null && originNode.Attributes["xyz"] != null) {
            //        position = ToTuple(originNode.Attributes["xyz"].Value);
            //    }
            //    Vector3 rotation = Vector3.Zero();
            //    if (originNode != null && originNode.Attributes["rpy"] != null) {
            //        rotation = ToTuple(originNode.Attributes["rpy"].Value);
            //    }
            //    inertialOrigin = Helper.ComposeUrdfTransform(position, rotation, true);
            //}

            // create the link gameobject
            UrdfLink urdfLink = new UrdfLink() { Name = linkName };
            urdfLink.Name = linkName;
            urdfLinks.Add(linkName, urdfLink);

            // Get the geometry node and skip it if there isn't one
            XmlNode[] visualNodesArray = Helper.GetXmlNodeChildrenByName(linkNode, "visual");
            // Iterate over all the visual nodes
            foreach (XmlNode xmlVisual in visualNodesArray) {
                XmlNode geomNode = Helper.GetXmlNodeChildByName(xmlVisual, "geometry");
                if (geomNode == null) {
                    continue;
                }
                Material material = new MeshBasicMaterial();
                XmlNode materialNode = Helper.GetXmlNodeChildByName(xmlVisual, "material");
                if (materialNode != null) {
                    var name = materialNode.Attributes["name"] is null ? "" : materialNode.Attributes["name"].Value;
                    if (name == "") {
                        material = LoadMaterial(materialNode); // Load itself or default
                    }
                    else {
                        if (!urdfMaterials.ContainsKey(name)) {
                            material = LoadMaterial(materialNode);
                            urdfMaterials[name] = material; // Add itself or default to its name
                        }
                        else {
                            material = urdfMaterials[name];
                        }
                    }
                }

                Matrix4 visualOrigin;
                {
                    // Get the mesh and the origin nodes
                    XmlNode originNode = Helper.GetXmlNodeChildByName(xmlVisual, "origin");

                    // Extract the position and rotation of the mesh
                    Vector3 position = Vector3.Zero();
                    if (originNode != null && originNode.Attributes["xyz"] != null) {
                        position = ToTuple(originNode.Attributes["xyz"].Value);
                    }
                    Vector3 rotation = Vector3.Zero();
                    if (originNode != null && originNode.Attributes["rpy"] != null) {
                        rotation = ToTuple(originNode.Attributes["rpy"].Value);
                    }
                    visualOrigin = Helper.ComposeUrdfTransform(position, rotation, true);
                }

                XmlNode meshNode =
                    Helper.GetXmlNodeChildByName(geomNode, "mesh") ??
                    Helper.GetXmlNodeChildByName(geomNode, "box") ??
                    Helper.GetXmlNodeChildByName(geomNode, "sphere") ??
                    Helper.GetXmlNodeChildByName(geomNode, "cylinder");

                if (meshNode.Name == "mesh") {
                    // Extract the mesh path
                    string fileName = ResolveMeshPath(meshNode.Attributes["filename"].Value, packages, options.WorkPath ?? "");

                    // Extract the scale from the mesh node
                    Vector3 scale = Vector3.One();
                    if (meshNode.Attributes["scale"] != null) {
                        scale = ToTuple(meshNode.Attributes["scale"].Value);
                    }
                    visualOrigin = visualOrigin.Scale(scale);

                    // load all meshes
                    string extension = Path.GetExtension(fileName).ToLower().Replace(".", "");
                    var models = options.MeshLoader(fileName, extension);

                    // create the rest of the meshes and child them to the click target
                    for (int i = 0; i < models.Length; i++) {
                        if (material != null) {
                            models[i].Material = material;
                        }
                        models[i].Name = i == 0 ? urdfLink.Name : $"{urdfLink.Name} ({i})";
                        models[i].Matrix = visualOrigin;
                        urdfLink.Geometries.Add(models[i]);
                    }
                }
                else {
                    Debug.Assert(false, "These are un-tested.");
                    //// FXIME: Instantiate the primitive geometry
                    //UrdfObject? primitiveObject = null;
                    //UrdfTransform primitiveTransform = new();
                    //switch (meshNode.Name) {
                    //    case "box": {
                    //            primitiveObject = new(new Mesh(new BoxGeometry(1, 1, 1), material: null));
                    //
                    //            Vector3 scale = ToTuple(meshNode.Attributes["size"].Value);
                    //            scale = Helper.UrdfToThreePos(scale);
                    //            primitiveTransform.Scale = scale;
                    //            break;
                    //        }
                    //
                    //    case "sphere": {
                    //            primitiveObject = new(new Mesh(new SphereGeometry(1, 1, 1), material: null));
                    //
                    //            float sphereRadius = float.Parse(meshNode.Attributes["radius"].Value);
                    //            primitiveTransform.Scale = Vector3.One() * sphereRadius * 2;
                    //            break;
                    //        }
                    //
                    //    case "cylinder": {
                    //            primitiveObject = new(new Mesh(new CylinderGeometry(1, 1, 1), material: null));
                    //
                    //            float length = float.Parse(meshNode.Attributes["length"].Value);
                    //            float radius = float.Parse(meshNode.Attributes["radius"].Value);
                    //            primitiveTransform.Scale = new Vector3(radius * 2, length / 2, radius * 2);
                    //            break;
                    //        }
                    //}
                    //
                    //// Position the transform
                    //primitiveTransform.Position = new Vector3().SetFromMatrixPosition(visualOrigin);
                    //primitiveTransform.Rotation = new Euler().SetFromRotationMatrix(visualOrigin);
                    //primitiveObject.Name = $"{urdfLink.Name} ({meshNode.Name})";
                    //if (material != null) {
                    //    primitiveObject.Material = material;
                    //}
                    //urdfLink.Geometries.Add(primitiveObject);
                }
            }
        }

        // Cycle through the joint nodes
        foreach (XmlNode jointNode in xmlJointsArray) {
            string jointName = jointNode.Attributes["name"].Value;

            // store the joints indexed by child name so we can find it later
            // to properly indicate the parents in the joint list
            xmlJoints.Add(jointName, jointNode);

            // Get the links by name
            XmlNode parentNode = Helper.GetXmlNodeChildByName(jointNode, "parent");
            XmlNode childNode = Helper.GetXmlNodeChildByName(jointNode, "child");
            string parentName = parentNode.Attributes["link"].Value;
            string childName = childNode.Attributes["link"].Value;
            UrdfLink parentLink = urdfLinks[parentName];
            UrdfLink childLink = urdfLinks[childName];

            Matrix4 jointOrigin;
            {
                // Position the origin if it's specified
                XmlNode transformNode = Helper.GetXmlNodeChildByName(jointNode, "origin");
                Vector3 position = Vector3.Zero();
                if (transformNode != null && transformNode.Attributes["xyz"] != null) {
                    position = ToTuple(transformNode.Attributes["xyz"].Value);
                }
                Vector3 rotation = Vector3.Zero();
                if (transformNode != null && transformNode.Attributes["rpy"] != null) {
                    rotation = ToTuple(transformNode.Attributes["rpy"].Value);
                }
                jointOrigin = Helper.ComposeUrdfTransform(position, rotation, false);
            }

            // Create the joint
            UrdfJoint urdfJoint = new UrdfJoint(jointOrigin) { Name = jointName };
            if (Enum.TryParse(jointNode.Attributes["type"].Value, true, out JointType jt)) urdfJoint.Type = jt;

            // Set the tree hierarchy
            // Parent the joint to its parent link
            urdfJoint.SetParent(parentLink);
            urdfJoint.SetChild(childLink);

            XmlNode axisNode = Helper.GetXmlNodeChildByName(jointNode, "axis");
            if (axisNode != null) {
                urdfJoint.Axis = ToTuple(axisNode.Attributes["xyz"].Value).Normalize();
            }

            XmlNode limitNode = Helper.GetXmlNodeChildByName(jointNode, "limit");
            if (limitNode != null) {
                // Use double.parse to handle particularly large values.
                if (limitNode.Attributes["lower"] != null) {
                    urdfJoint.Min = (float)double.Parse(limitNode.Attributes["lower"].Value);
                }
                if (limitNode.Attributes["upper"] != null) {
                    urdfJoint.Max = (float)double.Parse(limitNode.Attributes["upper"].Value);
                }
            }

            // save the URDF joint
            urdfJoints.Add(urdfJoint.Name, urdfJoint);
        }

        // loop through all the transforms until we find the one that has no parent
        UrdfRobot robot = options.target ?? new UrdfRobot();
        robot.Name = robotName;
        foreach (KeyValuePair<string, UrdfLink> kv in urdfLinks) {
            if (kv.Value.Parent == null) {
                robot.Links = urdfLinks;
                robot.Joints = urdfJoints;
                string err = "";
                Debug.Assert(robot.IsConsistent(ref err));
                return robot;
            }
        }
        // FIXME: Detect cycle existing
        throw new NotImplementedException("Detect cycle existing, not suppor yet.");
    }

    /// <summary>
    /// Default mesh loading function that can load STLs from file
    /// </summary>
    /// <param name="path"></param>
    /// <param name="ext"></param>
    /// <exception cref="Exception"></exception>
    private static UrdfObject[] LoadMesh(string path, string ext)
    {
        Mesh[] meshes;
        if (ext == "stl") {
            meshes = StlLoader.Load(path);
        }
        else {
            throw new Exception("Filetype '" + ext + "' not supported");
        }
        return meshes.Select(x => new UrdfObject(x)).ToArray();
    }

    private static Material LoadMaterial(XmlNode? materialNode)
    {
        Material material = new MeshBasicMaterial();
        Color color = Color.FromSystem(ColorB4.Gray);
        XmlNode? colorNode = materialNode is null ? null : Helper.GetXmlNodeChildByName(materialNode, "color");
        if (colorNode != null) {
            color = TupleToColor(colorNode.Attributes["rgba"].Value);
        }
        material.Color = color.RGB;
        material.Opacity = color.A;
        return material;
    }

    /// <summary>
    /// Resolves the given mesh path with the package options and working paths to return a full path to the mesh file.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="packages"></param>
    /// <param name="urdfPath"></param>
    /// <returns></returns>
    private static string ResolveMeshPath(string path, Dictionary<string, string> packages, string urdfPath)
    {
        if (path.IndexOf("package://") != 0) {
            return Path.GetFullPath(Path.Combine(urdfPath, path));
        }

        // extract the package name
        string[] split = path.Replace("package://", "").Split(new char[] { '/', '\\' }, 2);
        string targetPackage = split[0];
        string remaining = split[1];

        if (packages.ContainsKey(targetPackage)) {
            return Path.Combine(packages[targetPackage], remaining);
        }
        else if (packages.ContainsKey(SINGLE_PACKAGE_KEY)) {
            string packagePath = packages[SINGLE_PACKAGE_KEY];
            if (packagePath.EndsWith(targetPackage)) {
                return Path.Combine(packagePath, remaining);
            }
            else {
                return Path.Combine(
                    Path.Combine(packagePath, targetPackage),
                    remaining
                );
            }
        }
        throw new Exception("URDFLoader: " + targetPackage + " not found in provided package list!");
    }

    private static Vector3 ToTuple(string str)
    {
        str = str.Trim();
        str = System.Text.RegularExpressions.Regex.Replace(str, "\\s+", " ");
        string[] numbers = str.Split(' ');
        var num = numbers.Select(float.Parse).ToArray();
        return new Vector3(num[0], num[1], num[2]);
    }

    private static Color TupleToColor(string str)
    {
        str = str.Trim();
        str = System.Text.RegularExpressions.Regex.Replace(str, "\\s+", " ");

        string[] numbers = str.Split(' ');
        Color result = new();
        if (numbers.Length == 4) {
            result.R = float.Parse(numbers[0]);
            result.G = float.Parse(numbers[1]);
            result.B = float.Parse(numbers[2]);
            result.A = float.Parse(numbers[3]);
        }

        return result;
    }
}
