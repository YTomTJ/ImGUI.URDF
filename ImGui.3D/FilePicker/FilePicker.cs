// https://github.com/mellinoe/synthapp/blob/master/src/synthapp/Widgets/FilePicker.cs

using ImGuiNET;
using System.Numerics;
using System.Reflection;

namespace ImGui3D.Widgets;

public class FilePicker
{
    private const string FilePickerID = "###FilePicker";
    private static readonly Dictionary<object, FilePicker> s_filePickers = new Dictionary<object, FilePicker>();
    private static readonly Vector2 DefaultFilePickerSize = new Vector2(600, 400);

    public string? CurrentFolder { get; set; }
    public string? SelectedFile { get; set; }

    public static FilePicker GetFilePicker(object key, string? startingPath = null)
    {
        if (File.Exists(startingPath)) {
            startingPath = new FileInfo(startingPath).DirectoryName;
        }
        else if (string.IsNullOrEmpty(startingPath) || !Directory.Exists(startingPath)) {
            startingPath = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location);
            if (string.IsNullOrEmpty(startingPath)) {
                startingPath = AppContext.BaseDirectory;
            }
        }

        if (!s_filePickers.TryGetValue(key, out FilePicker? fp)) {
            fp = new FilePicker();
            fp.CurrentFolder = startingPath;
            s_filePickers.Add(key, fp);
        }
        return fp;
    }

    public bool Draw(ref string selected)
    {
        string label = "";
        if (selected != null) {
            if (Util.TryGetFileInfo(selected, out FileInfo realFile)) {
                label = realFile.Name;
            }
            else {
                label = "<Select File>";
            }
        }
        if (ImGui.Button(label)) {
            ImGui.OpenPopup(FilePickerID);
        }

        bool result = true;
        ImGui.SetNextWindowSize(DefaultFilePickerSize, ImGuiCond.FirstUseEver);
        if (ImGui.BeginPopupModal(FilePickerID, ref result, ImGuiWindowFlags.NoTitleBar)) {
            result = DrawFolder(ref selected, true);
            ImGui.EndPopup();
        }

        return result;
    }

    private bool DrawFolder(ref string selected, bool returnOnSelection = false)
    {
        ImGui.Text("Current Folder: " + CurrentFolder);
        bool result = false;

        if (ImGui.BeginChild(1, new Vector2(0, 600), 0)) {
            DirectoryInfo di = new DirectoryInfo(CurrentFolder);
            if (di.Exists) {
                if (di.Parent != null) {
                    ImGui.PushStyleColor(ImGuiCol.Text, Util.GetColor(System.Drawing.Color.Yellow));
                    if (ImGui.Selectable("../", false, ImGuiSelectableFlags.DontClosePopups)) {
                        CurrentFolder = di.Parent.FullName;
                    }
                    ImGui.PopStyleColor();
                }
                foreach (var fse in Directory.EnumerateFileSystemEntries(di.FullName)) {
                    if (Directory.Exists(fse)) {
                        string name = Path.GetFileName(fse);
                        ImGui.PushStyleColor(ImGuiCol.Text, Util.GetColor(System.Drawing.Color.Yellow));
                        if (ImGui.Selectable(name + "/", false, ImGuiSelectableFlags.DontClosePopups)) {
                            CurrentFolder = fse;
                        }
                        ImGui.PopStyleColor();
                    }
                    else {
                        string name = Path.GetFileName(fse);
                        bool isSelected = SelectedFile == fse;
                        if (ImGui.Selectable(name, isSelected, ImGuiSelectableFlags.DontClosePopups)) {
                            SelectedFile = fse;
                            if (returnOnSelection) {
                                result = true;
                                selected = SelectedFile;
                            }
                        }
                        if (ImGui.IsMouseDoubleClicked(0)) {
                            result = true;
                            selected = SelectedFile;
                            ImGui.CloseCurrentPopup();
                        }
                    }
                }
            }

        }
        ImGui.EndChild();

        if (ImGui.Button("Cancel")) {
            result = false;
            ImGui.CloseCurrentPopup();
        }

        if (SelectedFile != null) {
            ImGui.SameLine();
            if (ImGui.Button("Open")) {
                result = true;
                selected = SelectedFile;
                ImGui.CloseCurrentPopup();
            }
        }

        return result;
    }
}
