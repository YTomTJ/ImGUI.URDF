using THREE;

namespace URDFLoader;

#pragma warning disable CS8625

/// <summary>
/// Component representing the URDF Robot
/// </summary>
public class UrdfRobot : IObject3D
{
    public string Name { get; set; } = "";
    public UrdfTransform Transform { get; private set; } = new();
    public Dictionary<string, UrdfJoint> Joints = new Dictionary<string, UrdfJoint>();
    public Dictionary<string, UrdfLink> Links = new Dictionary<string, UrdfLink>();

    public bool AddJoint(UrdfJoint joint)
    {
        if (!Joints.ContainsKey(joint.Name)) {
            Joints.Add(joint.Name, joint);
            return true;
        }
        return false;
    }

    public bool AddLink(UrdfLink link)
    {
        if (!Links.ContainsKey(link.Name)) {
            Links.Add(link.Name, link);
            return true;
        }
        return false;
    }

    public Box3 GetBoundingBox()
    {
        Box3 bbox = new Box3(Vector3.One() * float.MaxValue, Vector3.One() * float.MinValue);
        foreach (var link in this.Links) {
            foreach (var obj in link.Value.Geometries) {
                bbox.Min = obj.Instance.Geometry.BoundingBox.Min.Min(bbox.Min);
                bbox.Max = obj.Instance.Geometry.BoundingBox.Max.Max(bbox.Max);
            }
        }
        return bbox;
    }

    //public Dictionary<string, float> TODO: GetAnglesAsDictionary()
    //{
    //    Dictionary<string, float> result = new Dictionary<string, float>();
    //    foreach (KeyValuePair<string, UrdfJoint> kv in joints) {
    //        float angle = kv.Value.angle;
    //        if (result.ContainsKey(kv.Key)) {
    //            result[kv.Key] = angle;
    //        }
    //        else {
    //            result.Add(kv.Key, angle);
    //        }
    //    }
    //    return result;
    //}

    //public void TODO: SetAnglesFromDictionary(Dictionary<string, float> dict)
    //{
    //    foreach (KeyValuePair<string, float> kv in dict) {
    //        if (joints.ContainsKey(kv.Key)) {
    //            joints[kv.Key].SetPosition(kv.Value);
    //        }
    //    }
    //}

    /// <summary>
    /// Validates the structure of the links and joints to verify that everything is consistant.
    /// Does not validate Unity's transform hierarchy or verify that there are no cycles.
    /// </summary>
    /// <param name="errorMsg"></param>
    /// <returns></returns>
    public bool IsConsistent(ref string errorMsg)
    {
        errorMsg = "";

        // verify that
        // * every joint's name matches its key
        // * every joint specifies a joint type
        // * both parent and child match
        foreach (KeyValuePair<string, UrdfJoint> kv in Joints) {
            UrdfJoint j = kv.Value;

            if (j.Name != kv.Key) {
                errorMsg = string.Format("Joint \"{0}'s\" name does not match key \"{1}\"", j.Name, kv.Key);
                return false;
            }

            if (j.Type is JointType.Unknown) {
                errorMsg = string.Format("Joint \"{0}'s\" type is not set", j.Name);
                return false;
            }

            if (j.Parent == null) {
                errorMsg = string.Format("Joint \"{0}\" does not have a parent link", j.Name);
                return false;
            }

            if (!j.Parent.Children.Contains(j)) {
                errorMsg = string.Format("Joint \"{0}'s\" parent link \"{1}\" does not contain it as a child", j.Name, j.Parent.Name);
                return false;
            }

            if (j.Child == null) {
                errorMsg = string.Format("Joint \"{0}\" does not have a child link", j.Name);
                return false;
            }

            if (j.Child.Parent != j) {
                errorMsg = string.Format("Joint \"{0}'s\" child link \"{1}\" does not have it as a parent", j.Name, j.Child.Name);
                return false;
            }
        }

        // verify that
        // * every link's name matches it key
        // * every parent and child matches
        foreach (KeyValuePair<string, UrdfLink> kv in Links) {
            UrdfLink l = kv.Value;

            if (l.Name != kv.Key) {
                errorMsg = string.Format("Link \"{0}'s\" name does not match key \"{1}\"", l.Name, kv.Key);
                return false;
            }

            if (l.Parent != null && l.Parent.Child != l) {
                errorMsg = string.Format("Link \"{0}'s\" parent joint \"{1}\" does not have it as a child", l.Name, l.Parent.Name);
                return false;
            }

            foreach (UrdfJoint j in l.Children) {
                if (j.Parent != l) {
                    errorMsg = string.Format("Link \"{0}'s\" child joint \"{1}\" does not have it as a parent", l.Name, j.Name);
                    return false;
                }
            }
        }
        return true;
    }

    public void AddToScene(Scene scene)
    {
        this.Transform.AddToScene(scene);
        foreach (var item in Links) {
            item.Value.AddToScene(scene);
            if (item.Value.Parent is null) {
                item.Value.Origin.Instance.Parent = this.Transform.Instance; // FIXME: {Prb:TfChainLink}
            }
        }
        foreach (var item in Joints) {
            item.Value.AddToScene(scene);
        }
    }

    public void RemoveFromScene(Scene scene)
    {
        this.Transform.RemoveFromScene(scene);
        foreach (var item in Links) {
            item.Value.RemoveFromScene(scene);
            if (item.Value.Parent is null) {
                item.Value.Origin.Instance.Parent = null; // FIXME: {Prb:TfChainLink}
            }
        }
        foreach (var item in Joints) {
            item.Value.RemoveFromScene(scene);
        }
    }
}
