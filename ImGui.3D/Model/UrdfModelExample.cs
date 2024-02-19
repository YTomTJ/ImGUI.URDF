using ImGui3D.Widgets;
using ImGuiExt.TK;
using ImGuiNET;
using System.ComponentModel.DataAnnotations;
using THREE;
using URDFLoader;
using Color = THREE.Color;
using Path = System.IO.Path;

namespace ImGui3D.Three;

public class UrdfModelExample : ModelExample
{
    #region 坐标轴相关

    private bool ShowAxes
    {
        get => this.Axes is not null;
        set {
            if (ShowAxes != value) {
                if (value) {
                    Axes = new AxesHelper(1);
                    Axes.Scale = this.AxesScale;
                    scene.Add(this.Axes);
                }
                else {
                    scene.Remove(this.Axes!);
                    this.Axes = null;
                }
            }
        }
    }
    private AxesHelper? Axes;
    private Vector3 _AxesScale = Vector3.One() * 20;
    private Vector3 AxesScale
    {
        get => _AxesScale;
        set {
            _AxesScale = value;
            if (Axes != null) {
                Axes.Scale = value;
            }
        }
    }

    #endregion

    private bool ShowGround
    {
        get => this.Ground!.Visible;
        set => this.Ground!.Visible = value;
    }
    private Mesh? Ground;

    public UrdfRobot? Robot { get; private set; }

    public UrdfModelExample(ITkWindow view) : base(view)
    {
        BuildGeometry();
    }

    #region 构造三维环境

    protected override void InitCamera()
    {
        camera.Fov = 45.0f;
        camera.Aspect = this.view.AspectRatio;
        camera.Near = 0.1f;
        camera.Far = 1000.0f;
        camera.Up.Set(0, 0, 1);
        camera.Position.Set(32, -32, 32);
        camera.LookAt(new Vector3(0, 0, 0));
    }

    public virtual void BuildGround()
    {
        var ground = new PlaneGeometry(100, 100);
        this.Ground = new Mesh(ground, new MeshBasicMaterial() { Color = Color.Hex(0x777777) });
        scene.Add(this.Ground);
    }

    public virtual void BuildGeometry()
    {
        this.BuildGround();

        // 小物体，解决“如果只有地面，显示会出现异常”的问题
        var dump = new Mesh(new BoxGeometry(1e-3f, 1e-3f, 1e-3f), new MeshBasicMaterial());
        dump.Position.Set(0, 0, 0);
        scene.Add(dump);

        this.ShowAxes = true;
    }

    #endregion

    private Dictionary<string, object?>? LinkJointList = null;
    private float JointAxesSize = 1;
    private bool JointAxesShow = true;
    protected override void OnImGuiUpdate()
    {
        {
            var v = this.ShowAxes;
            ImGui.Checkbox("世界坐标", ref v);
            this.ShowAxes = v;
        }
        ImGui.SameLine();
        {
            var v = this.ShowGround;
            ImGui.Checkbox("显示地面", ref v);
            this.ShowGround = v;
        }


        ImGui.Separator();
        {
            ImGui.Text("交互:");
            if (ImGui.Button("视角复位")) {
                this.SetRobotHome();
            }

            if (this.trackball is not null) {
                float v = this.trackball.ZoomSpeed;
                ImGui.SliderFloat("滚动速度", ref v, 0.2f, 5.0f);
                this.trackball.ZoomSpeed = v;
            }

            if (this.Robot is not null) {
                if (ImGui.Button("卸载模型")) {
                    this.UnloadUrdf();
                    this.InitCamera();
                }
            }
            else {
                // 显示"打开"对话框
                string selecting = "加载模型 ...";
                var picker = FilePicker.GetFilePicker(this);
                picker.Draw(ref selecting);
                if (picker.SelectedFile != null) {
                    this.LoadUrdf(picker.SelectedFile);
                    picker.SelectedFile = null;
                }
            }
        }

        ImGui.Separator();
        ImGui.Text($"当前模型: {this.Robot?.Name}");
        if (this.Robot is not null) {

            ImGui.Text("基坐标: ");
            ImGui.PushItemWidth(96);
            if (EditVector3("位移", this.Robot.Transform.Position) is Vector3 pos) {
                this.Robot.Transform.Position = pos;
            }
            if (EditVector3("旋转", Helper.QuaternionToVector3(this.Robot.Transform.Quaternion)) is Vector3 rot) {
                this.Robot.Transform.Quaternion = Helper.Vector3ToQuaternion(rot);
            }

            if (ImGui.Checkbox(LabelOnLeft("显示关节坐标"), ref JointAxesShow)) {
                this.UpdateJointAxes();
            }
            ImGui.SameLine();
            if (EditFloat("坐标尺寸", ref JointAxesSize)) {
                this.UpdateJointAxes();
            }
            ImGui.PopItemWidth();

            // 关节控制列表
            if (this.Robot is not null) {
                ImGui.Text("关节列表:");

                if (ImGui.Button("全部显示")) {
                    foreach (var l in this.Robot.Links) {
                        l.Value.Visible = true;
                    }
                }
                ImGui.SameLine();
                if (ImGui.Button("全部隐藏")) {
                    foreach (var l in this.Robot.Links) {
                        l.Value.Visible = false;
                    }
                }

                if ((LinkJointList ?? ListLinksAndJoints()) is Dictionary<string, object?> list) {
                    foreach (var line in list) {
                        if (line.Key.StartsWith("JJJ")) {
                            ImGui.PushStyleColor(ImGuiCol.Text, Util.GetColor(System.Drawing.Color.Orange));
                        }
                        if (line.Key.StartsWith("LLL")) {
                            ImGui.PushStyleColor(ImGuiCol.Text, Util.GetColor(System.Drawing.Color.Green));
                        }
                        ImGui.Text(line.Key.Substring(3));
                        ImGui.PopStyleColor();
                        if (line.Value is UrdfJoint j) {
                            // 关节运动
                            ImGui.SameLine();
                            ImGui.PushItemWidth(96);
                            if (j.Type != JointType.Fixed) {
                                float p = j.Position;
                                if (j.Min.HasValue && j.Max.HasValue) {
                                    if (EditFloat($"{j.Name}", ref p, min: j.Min.Value, max: j.Max.Value)) {
                                        j.Position = p;
                                    }
                                }
                                else {
                                    if (EditFloat($"{j.Name}", ref p)) {
                                        j.Position = p;
                                    }
                                }
                            }
                            ImGui.PopItemWidth();
                        }
                        if (line.Value is UrdfLink l) {
                            // 可视化
                            ImGui.SameLine();
                            var v = l.Visible;
                            ImGui.Checkbox($"##{l.Name}_vis", ref v);
                            l.Visible = v;
                        }
                    }
                }
            }
        }
    }

    #region IMGUI辅助

    private string LabelOnLeft(string label)
    {
        float width = ImGui.CalcItemWidth();
        ImGui.Text(label);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(width);
        string labelID = "##" + label;
        return labelID;
    }

    private Vector3? EditVector3(string name, Vector3 value, string format = "%.03f")
    {
        bool changed = false;
        changed |= EditFloat($"x@{name}", ref value.X);
        ImGui.SameLine();
        changed |= EditFloat($"y@{name}", ref value.Y);
        ImGui.SameLine();
        changed |= EditFloat($"z@{name}", ref value.Z);
        return changed ? value : null;
    }

    private Dictionary<string, float> TempFloatValue = new();
    private bool EditFloat(string name, ref float value, string format = "%.03f", float? min = null, float? max = null)
    {
        if (!TempFloatValue.ContainsKey(name)) {
            TempFloatValue.Add(name, value);
        }
        var v = TempFloatValue[name];
        if (ImGui.InputFloat(LabelOnLeft(name), ref v, min ?? 0, max ?? 0, format, ImGuiInputTextFlags.EnterReturnsTrue) && v != value) {
            TempFloatValue.Remove(name);
            value = v;
            return true;
        }
        TempFloatValue[name] = v;
        return false;
    }

    #endregion

    #region URDF辅助

    public void LoadUrdf(string urdf_path, string? package = null)
    {
        if (this.Robot is not null) {
            this.UnloadUrdf();
        }

        // 加载模型
        urdf_path = Path.GetFullPath(urdf_path);
        var folder = Path.GetFullPath(Path.Combine(urdf_path, "../../../"));
        this.Robot = Loader.LoadUrdf(urdf_path, package ?? folder);
        if (this.Robot is not null) {
            this.Robot.AddToScene(this.scene);
            this.SetRobotHome();
            this.LinkJointList = null;
            this.JointAxesShow = false;
            this.UpdateJointAxes();
        }

    }

    public void UnloadUrdf()
    {
        if (this.Robot is null) {
            return;
        }
        this.Robot.RemoveFromScene(this.scene);
        this.Robot = null;
    }

    /// <summary>
    /// 根据模型调节视图参数
    /// </summary>
    private void SetRobotHome()
    {
        this.InitCamera();

        if (this.Robot is null) {
            return;
        }

        // TODO: 更好的视角控制
        Box3 bbox = this.Robot.GetBoundingBox();
        var scale = bbox.Max - bbox.Min;
        var center = (bbox.Max + bbox.Min) / 2;
        var dir = new Vector3(1, -1, 0.5f).Normalize();
        var at = center + dir * scale.Length() * 3.0f;
        this.camera.LookAt(center.X, center.Y, center.Z);
        this.camera.Position.Set(at.X, at.Y, at.Z * 2.0f);

        // 让模型的最低点位于地面上
        this.Ground?.Position.SetZ(bbox.Min.Z * 1.1f);
        // 让关节坐标系显示大小合理
        this.JointAxesSize = scale.Length() * 0.2f;
    }

    private void UpdateJointAxes()
    {
        if (this.Robot is null)
            return;
        foreach (var j in this.Robot.Joints) {
            j.Value.Frame.Visible = this.JointAxesShow;
            j.Value.Frame.Scale = Vector3.One() * this.JointAxesSize;
        }
    }

    #endregion

    #region 枚举连杆和关节

    private Dictionary<string, object?>? ListLinksAndJoints()
    {
        if (this.Robot is null) {
            return null;
        }
        Dictionary<string, object?> res = new();
        foreach (var link in this.Robot.Links.Where(x => x.Value.Parent is null)) {
            RecursiveLinksAndJoints(ref res, link.Value, 0);
        }
        return res;
    }


    private void RecursiveLinksAndJoints(ref Dictionary<string, object?> v, object? obj, float lv)
    {
        if (obj is UrdfJoint j) {
            v.Add("JJJ" + Prefix(lv) + $"{j.Name}: {j.Type} ", j);
            RecursiveLinksAndJoints(ref v, j.Child, lv + 0.5f);
        }
        if (obj is UrdfLink l) {
            v.Add("LLL" + Prefix(lv) + $"{l.Name} ({string.Join(" ,", l.Geometries.Select(x => x.Name))})", l);
            foreach (var joint in l.Children) {
                RecursiveLinksAndJoints(ref v, joint, lv + 1.0f);
            }
        }
    }

    private static string Prefix(float lv)
    {
        var pf = new string(Enumerable.Repeat(' ', (int)(lv * 8)).ToArray());
        return pf;
    }

    #endregion
}
