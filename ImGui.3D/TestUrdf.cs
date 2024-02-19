using URDFLoader;

namespace ImGui3D;

static class TestUrdf
{
    public static void Main(string[] args)
    {
        string urdf_path = @"E:\works\YLJA\tmp\App\ImGui.3D\urdf-loaders\urdf\T12\urdf\T12.URDF";
        UrdfRobot robot = Loader.LoadUrdf(urdf_path);

        Console.WriteLine(robot.Name);
    }
}