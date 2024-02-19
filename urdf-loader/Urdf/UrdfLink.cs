using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using THREE;

namespace URDFLoader;

#pragma warning disable CS8625

/// <summary>
/// Object discribing a URDF Link
/// </summary>
public class UrdfLink : ObservableObject, IObject3D
{
    public string Name { get; set; } = "";
    internal UrdfTransform Origin { get; }

    public UrdfJoint? Parent
    {
        get => _Parent;
        set => this.SetProperty(ref _Parent, value);
    }
    private UrdfJoint? _Parent = null;

    public ObservableCollection<UrdfJoint> Children { get; } = new ObservableCollection<UrdfJoint>();

    public ObservableCollection<UrdfObject> Geometries { get; } = new ObservableCollection<UrdfObject>();

    public bool Visible
    {
        get => _Visible;
        set {
            if (this.SetProperty(ref _Visible, value)) {
                foreach (var g in this.Geometries) {
                    g.Instance.Visible = _Visible;
                }
            }
        }
    }
    private bool _Visible = true;

    public UrdfLink()
    {
        this.Origin = new();
    }

    public void AddToScene(Scene scene)
    {
        this.Origin.AddToScene(scene);
        if (Parent is not null)
            this.Origin.Instance.Parent = Parent.Transform.Instance; // FIXME: {Prb:TfChainLink}
        foreach (var item in this.Geometries) {
            item.AddToScene(scene);
            // FIXME: {Prb:TfChainLink} Add object to scene should make cause parent different, here refresh the linking chain. 
            item.Instance.Parent = this.Origin.Instance; // FIXME: {Prb:TfChainLink}
        }
    }

    public void RemoveFromScene(Scene scene)
    {
        this.Origin.RemoveFromScene(scene);
        this.Origin.Instance.Parent = null; // FIXME: {Prb:TfChainLink}
        foreach (var item in this.Geometries) {
            item.RemoveFromScene(scene);
            item.Instance.Parent = null; // FIXME: {Prb:TfChainLink}
        }
    }
}
