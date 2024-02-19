using ImGuiExt.SDL;

namespace ImGuiExt.TK;

public interface ITkWindow : IWindow
{
    public ImGuiTkContext TkContext { get; }

    public SDL2GraphicsContext GContext { get; }
}
