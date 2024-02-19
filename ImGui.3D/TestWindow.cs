using ImGui3D.Three;

namespace ImGui3D;

internal class TestWindow
{
    static unsafe void Main(string[] args)
    {
        // THREE + OpenTK + ImGUI test
        var window = new ThreeTkWindow();

        var example = new UrdfModelExample(window);
        example.Initialize();
        example.InitImGui();
        //example.LoadUrdf(@"E:\works\YLJA\data\handler\urdf\handler.urdf");

        window.Exp = example;
        window.Show();
    }
}
