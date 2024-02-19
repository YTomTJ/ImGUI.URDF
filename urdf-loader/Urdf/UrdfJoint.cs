using CommunityToolkit.Mvvm.ComponentModel;
using THREE;

namespace URDFLoader;

#pragma warning disable CS8625

public enum JointType
{
    Unknown,
    Revolute,
    Continuous,
    Prismatic,
    Floating,
    Planar,
    Fixed,
}

/// <summary>
/// Object describing a URDF joint with joint transform, associated geometry, etc
/// </summary>
public class UrdfJoint : ObservableObject, IObject3D
{
    public string Name { get; set; } = "";
    public JointType Type { get; set; } = JointType.Unknown;
    public Vector3 Axis { get; set; } = Vector3.Zero();
    public float? Min { get; set; }
    public float? Max { get; set; }

    public UrdfTransform Origin { get; }
    public UrdfTransform Transform { get; }

    public UrdfLink? Parent { get; private set; }
    public UrdfLink? Child { get; private set; }

    public AxesHelper Frame { get; }

    /// <summary>
    /// Joint position. Use degree for angles.
    /// </summary>
    public float Position
    {
        get => GetPosition();
        set {
            var q = Position;
            if (this.SetProperty(ref q, value)) {
                this.SetPosition(q);
            }
        }
    }

    public UrdfJoint(Matrix4 tf)
    {
        this.Origin = new UrdfTransform(tf) { };
        this.Transform = new UrdfTransform() { };
        this.Frame = new AxesHelper(1);
    }

    public void SetParent(UrdfLink? link)
    {
        if (this.Parent is not null) {
            this.Parent.Children.Remove(this);
        }
        link?.Children.Add(this);
        this.Parent = link;
    }

    public void SetChild(UrdfLink? link)
    {
        if (this.Child is not null) {
            this.Child.Parent = null;
        }
        if (link is not null) link.Parent = this;
        this.Child = link;
    }

    private float GetPosition()
    {
        switch (Type) {
            case JointType.Fixed: {
                    break;
                }
            case JointType.Continuous:
            case JointType.Revolute: {
                    var q = new Quaternion().SetFromAxisAngle(Axis, 0);
                    return this.Transform.Quaternion.AngleTo(q) * MathUtils.RAD2DEG;
                }
            case JointType.Prismatic:
            case JointType.Floating:
            case JointType.Planar: {
                    return this.Transform.Position.Dot(this.Axis);
                }
        }
        return float.NaN;
    }

    private void SetPosition(float val)
    {
        switch (Type) {
            case JointType.Fixed: {
                    break;
                }
            case JointType.Continuous:
            case JointType.Revolute: {
                    val = val * MathUtils.DEG2RAD;
                    if (Type == JointType.Revolute) {
                        if (Min.HasValue) {
                            val = Math.Clamp(val, Min.Value, float.MaxValue);
                        }
                        if (Max.HasValue) {
                            val = Math.Clamp(val, float.MinValue, Max.Value);
                        }
                    }
                    this.Transform.Quaternion = new Quaternion().SetFromAxisAngle(Axis, val);
                    break;
                }
            case JointType.Prismatic:
            case JointType.Floating:
            case JointType.Planar: {
                    this.Transform.Position = this.Axis * val;
                    break;
                }
        }
    }

    public void AddToScene(Scene scene)
    {
        this.Origin.AddToScene(scene);
        this.Transform.AddToScene(scene);
        scene.Add(this.Frame);
        if (this.Parent is not null) this.Origin.Instance.Parent = this.Parent.Origin.Instance; // FIXME: {Prb:TfChainLink}
        this.Transform.Instance.Parent = this.Origin.Instance; // FIXME: {Prb:TfChainLink}
        this.Frame.Parent = this.Transform.Instance; // FIXME: {Prb:TfChainLink}
    }

    public void RemoveFromScene(Scene scene)
    {
        this.Origin.RemoveFromScene(scene);
        this.Transform.RemoveFromScene(scene);
        scene.Remove(this.Frame);
        this.Origin.Instance.Parent = null; // FIXME: {Prb:TfChainLink}
        this.Transform.Instance.Parent = null; // FIXME: {Prb:TfChainLink}
        this.Frame.Parent = null; // FIXME: {Prb:TfChainLink}
    }
}
