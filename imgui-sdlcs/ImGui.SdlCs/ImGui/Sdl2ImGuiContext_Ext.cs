using ImGuiExt.SDL;
using ImGuiNET;
using System.Diagnostics;
using System.Numerics;

namespace ImGuiExt
{
    public enum LayoutMode
    {
        /// <summary>
        /// Not overlay, action will pass to lower layers.
        /// </summary>
        Normal,

        /// <summary>
        /// Overlay on the top, will block action on lower layers.
        /// </summary>
        Overlay,

        /// <summary>
        /// Overlay on the top with only one window, will block action on lower layers.
        /// </summary>
        Single,

        /// <summary>
        /// Overlay on the top with only one window, but not block action on lower layers.
        /// </summary>
        SinglePass,
    };

    public class Sdl2ImGuiContext_Ext : Sdl2ImGuiContext
    {
        /// <summary>
        /// Layout that will run in this window.
        /// You could add layout by OnLayoutUpdate, but that will not include in this windows's management.
        /// </summary>
        public LayoutUpdateMethod? Action;

        public ImGuiWindowFlags Flags { get; set; } = ImGuiWindowFlags.None;

        public LayoutMode Mode { get; set; } = LayoutMode.Normal;

        public bool ShowFps { get; set; } = true;
        public float OverlayOpacity { get; set; } = 0.2f;

        /// <summary>
        /// ImWindow constructor.
        /// </summary>
        /// <param name="mode">Window mode</param>
        /// <param name="action">User UI method</param>
        /// <param name="title">Window caption</param>
        /// <param name="width">Window width</param>
        /// <param name="height">Window height</param>
        /// <param name="flags">Additional flags when mode is WindowMode.Single</param>
        public Sdl2ImGuiContext_Ext(string title, LayoutUpdateMethod? action, int width, int height, ImGuiWindowFlags flags)
            : base(title, width, height)
        {
            Action = action;
            Flags = flags;
            OnLayoutUpdate += ExtUpdateLayout;
        }

        public Sdl2ImGuiContext_Ext(IWindow window, ImGuiWindowFlags flags) : this(null, window, flags)
        {
        }

        public Sdl2ImGuiContext_Ext(LayoutUpdateMethod? action, IWindow window, ImGuiWindowFlags flags) : base(window)
        {
            Action = action;
            Flags = flags;

            OnLayoutUpdate += ExtUpdateLayout;
        }

        private bool ExtUpdateLayout()
        {
            Debug.Assert(this.Window != null);
            if (Mode == LayoutMode.Overlay) {
                ImGui.SetNextWindowPos(new Vector2(0, 0));
                // Fill imgui to window size will block mouse on lower layer.
                ImGui.SetNextWindowSize(new Vector2(this.Window.Size.Width, this.Window.Size.Height));
                ImGui.SetNextWindowBgAlpha(OverlayOpacity);
                ImGui.Begin("Overlay", Flags | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoBringToFrontOnFocus);
                if (ShowFps) ImGui.Text(string.Format("FPS:{0:0.00}", ImGui.GetIO().Framerate));
                ImGui.SetCursorPos(new Vector2(0, 0));
                ImGui.End();
            }
            else if (Mode == LayoutMode.Single || Mode == LayoutMode.SinglePass) {
                ImGui.SetNextWindowPos(new Vector2(0, 0));
                if (Mode == LayoutMode.Single) {
                    // Fill imgui to window size will block mouse on lower layer.
                    ImGui.SetNextWindowSize(new Vector2(this.Window.Size.Width, this.Window.Size.Height));
                }
                ImGui.SetNextWindowBgAlpha(OverlayOpacity);
                ImGui.Begin("Overlay", Flags | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar);
                if (ShowFps) ImGui.Text(string.Format("FPS:{0:0.00}", ImGui.GetIO().Framerate));
            }
            else {
                ImGui.SetNextWindowPos(new Vector2(0, 0));
                ImGui.SetNextWindowBgAlpha(OverlayOpacity);
                ImGui.Begin("Overlay", Flags | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar);
                if (ShowFps) ImGui.Text(string.Format("FPS:{0:0.00}", ImGui.GetIO().Framerate));
                ImGui.End();
            }

            if (this.Action != null && !this.Action()) {
                Window.Close();
            }

            if (Mode == LayoutMode.Single || Mode == LayoutMode.SinglePass) {
                ImGui.End();
            }
            return true;
        }
    }
}