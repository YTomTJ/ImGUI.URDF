using System;
using System.Drawing;
using System.Threading.Tasks;

namespace ImGuiExt.SDL
{
    public delegate void WindowProc(IWindow win);
    public delegate void WindowEvent(IWindow win, object? e);

    public interface IWindow : IDisposable
    {
        public bool IsOpened { get; }

        public float MaxFPS { get; set; }

        public string Title { get; set; }

        public Point Position { get; set; }

        public Size Size { get; set; }

        public float AspectRatio { get; }

        public IntPtr HWnd { get; }

        public event WindowProc OnStart;
        public event WindowProc OnLoop;
        public event WindowProc OnExit;
        public event WindowEvent OnResized;
        public event WindowEvent OnKeyDown;
        public event WindowEvent OnKeyUp;
        public event WindowEvent OnMouseDown;
        public event WindowEvent OnMouseMove;
        public event WindowEvent OnMouseUp;
        public event WindowEvent OnMouseWheel;
        public event WindowEvent OnAddController;
        public event WindowEvent OnRemoveController;
        public event WindowEvent OnTextInput;

        public void Show();
        public Task ShowAsync();
        public void Hide();
        public void Close();
    }
}