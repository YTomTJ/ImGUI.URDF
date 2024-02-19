using System.Xml;
using THREE;

namespace URDFLoader;

public static class Helper
{
    public static XmlNode? GetXmlNodeChildByName(XmlNode parent, string name)
    {
        foreach (XmlNode n in parent.ChildNodes) {
            if (n.Name == name) {
                return n;
            }
        }
        return null;
    }

    /// <summary>
    /// returns the first instance of a child node with the name "name" null if it couldn't be found
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="name"></param>
    /// <param name="recursive"></param>
    /// <returns></returns>
    public static XmlNode[] GetXmlNodeChildrenByName(XmlNode parent, string name, bool recursive = false)
    {
        List<XmlNode> nodes = new List<XmlNode>();
        foreach (XmlNode n in parent.ChildNodes) {
            if (n.Name == name) {
                nodes.Add(n);
            }
            if (recursive) {
                var recursiveChildren = GetXmlNodeChildrenByName(n, name, true);
                foreach (XmlNode x in recursiveChildren) {
                    nodes.Add(x);
                }
            }
        }
        return nodes.ToArray();
    }

    public static Euler Vector3ToEuler(Vector3 vec)
    {
        return new Euler().SetFromVector3(vec, Loader.ORDER);
    }

    public static Quaternion Vector3ToQuaternion(Vector3 rpy)
    {
        return new Quaternion().SetFromEuler(Vector3ToEuler(rpy));
    }

    public static Vector3 QuaternionToVector3(Quaternion quat)
    {
        return new Euler().SetFromQuaternion(quat, Loader.ORDER).ToVector3();
    }

    public static Matrix4 ComposeUrdfTransform(Vector3 pos, Vector3 rpy, bool q)
    {
        var R = new Matrix4().Compose(Vector3.Zero(), new Quaternion().SetFromEuler(new Euler().SetFromVector3(rpy, Loader.ORDER)), Vector3.One());
        var T = new Matrix4().Compose(pos, Quaternion.Identity(), Vector3.One());
        return (T * R);
    }

    //public static Quaternion Vector4ToQuaternion(Vector4 xyzw)
    //{
    //    return new Quaternion(xyzw.X, xyzw.Y, xyzw.Z, xyzw.W);
    //}

    ///// <summary>
    ///// Convert URDF X-Y-Z to Three.js type.
    ///// URDF:     Y left | Z up | X forward
    ///// Three.js: X right | Y up | Z forward
    ///// </summary>
    ///// <param name="v"></param>
    ///// <returns></returns>
    //public static Vector3 UrdfToThreePos(Vector3 v)
    //{
    //    return v;
    //}

    ///// <summary>
    ///// Convert URDF scale to Three.js type.
    ///// </summary>
    //public static Vector3 UrdfToThreeScale(Vector3 v)
    //{
    //    //return new Vector3(v.Y, v.Z, v.X);
    //    return v;
    //}

    ///// <summary>
    ///// Convert URDF rotations to Three.js type.
    ///// URDF(Fixed Axis rotation, XYZ): roll on X | pitch on Y | yaw on Z | radians
    ///// Three.js: roll on Z | yaw on Y | pitch on X | degrees
    ///// </summary>
    ///// <param name="v"></param>
    ///// <returns></returns>
    //public static Vector3 UrdfToThreeRot(Vector3 v)
    //{
    //    //// Negate X and Z because we're going from Right to Left handed rotations. Y is handled because the axis itself is flipped
    //    //v.X *= -1;
    //    //v.Z *= -1;
    //    //v *= MathUtils.RAD2DEG;

    //    //// swap the angle values
    //    //v = new Vector3(v.Y, v.Z, v.X);

    //    //// Applying rotations in ZYX ordering, as indicated above
    //    //var q = Quaternion.Identity();
    //    //q *= new Quaternion().SetFromEuler(new Euler(0, v.Y, 0));
    //    //q *= new Quaternion().SetFromEuler(new Euler(v.X, 0, 0));
    //    //q *= new Quaternion().SetFromEuler(new Euler(0, 0, v.Z));

    //    //// The following rotation is the same as the previous rotations in order
    //    //// q = Quaternion.Euler(v.Y, v.Z, v.X);
    //    //return new Euler().SetFromQuaternion(q, null).ToVector3();
    //    return v;
    //}
}
