using Silk.NET.SDL;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ImGuiExt.SDL
{
    public unsafe class SDL2Window : IDisposable, IWindow
    {
        private Window* hwnd;

        public IntPtr HWnd => (IntPtr)hwnd;
        protected Window* Wnd => hwnd;

        public void* GLContext { get; }

        public bool IsAlive
        {
            get;
            private set;
        } = false;

        public event WindowProc? OnStart;
        public event WindowProc? OnLoop;
        public event WindowProc? OnExit;
        public event WindowEvent? OnResized;
        public event WindowEvent? OnKeyDown;
        public event WindowEvent? OnKeyUp;
        public event WindowEvent? OnMouseDown;
        public event WindowEvent? OnMouseMove;
        public event WindowEvent? OnMouseUp;
        public event WindowEvent? OnMouseWheel;
        public event WindowEvent? OnAddController;
        public event WindowEvent? OnRemoveController;
        public event WindowEvent? OnTextInput;

        public bool IsOpened => ((WindowFlags)SDL2Helper.SDLCS.GetWindowFlags(hwnd) & WindowFlags.Hidden) == 0;

        private float _MaxFPS = 60.0f;
        public float MaxFPS
        {
            get => _MaxFPS;
            set => _MaxFPS = value;
        }

        public unsafe string Title
        {
            get {
                return Marshal.PtrToStringUTF8((nint)SDL2Helper.SDLCS.GetWindowTitle(hwnd))!;
            }
            set {
                SDL2Helper.SDLCS.SetWindowTitle(hwnd, value);
            }
        }

        public System.Drawing.Point Position
        {
            get {
                int x, y;
                SDL2Helper.SDLCS.GetWindowPosition(hwnd, &x, &y);
                return new System.Drawing.Point(x, y);
            }
            set {
                SDL2Helper.SDLCS.SetWindowPosition(hwnd, value.X, value.Y);
            }
        }

        public Size Size
        {
            get {
                int x, y;
                SDL2Helper.SDLCS.GetWindowSize(hwnd, &x, &y);
                return new Size(x, y);
            }
            set {
                SDL2Helper.SDLCS.SetWindowSize(hwnd, value.Width, value.Height);
            }
        }

        public float AspectRatio
        {
            get {
                int x, y;
                SDL2Helper.SDLCS.GetWindowSize(hwnd, &x, &y);
                return 1.0f * x / y;
            }
        }

        public const int doubleBuffer = 1;
        public const int depthSize = 24;
        public const int stencilSize = 8;
        public const int majorVersion = 3;
        public const int minorVersion = 0;

        public SDL2Window(string title, int width, int height, WindowFlags flags)
        {
            SDL2Helper.SDLCS.GLSetAttribute(GLattr.Doublebuffer, doubleBuffer);
            SDL2Helper.SDLCS.GLSetAttribute(GLattr.DepthSize, depthSize);
            SDL2Helper.SDLCS.GLSetAttribute(GLattr.StencilSize, stencilSize);
            SDL2Helper.SDLCS.GLSetAttribute(GLattr.ContextMajorVersion, majorVersion);
            SDL2Helper.SDLCS.GLSetAttribute(GLattr.ContextMinorVersion, minorVersion);

            SDL2Helper.SDLCS.Init(Sdl.InitEverything);
            if (hwnd != null)
                throw new InvalidOperationException("SDL2Window already initialized, Dispose() first before reusing!");
            this.hwnd = SDL2Helper.SDLCS.CreateWindow(title, Sdl.WindowposCentered, Sdl.WindowposCentered, width, height, (uint)flags);

            this.GLContext = SDL2Helper.SDLCS.GLCreateContext(this.hwnd);
            SDL2Helper.SDLCS.GLMakeCurrent(this.hwnd, this.GLContext);
            SDL2Helper.SDLCS.GLSetSwapInterval(1);  // Enable vsync
        }

        public void Show()
        {
            SDL2Helper.SDLCS.ShowWindow(hwnd);
            this.Run();
        }

        public Task ShowAsync()
        {
            SDL2Helper.SDLCS.ShowWindow(hwnd);
            return Task.Factory.StartNew(() => {
                this.Run();
            });
        }

        public void Hide()
        {
            SDL2Helper.SDLCS.HideWindow(hwnd);
        }

        public void Close()
        {
            IsAlive = false;
        }

        protected virtual void LoopHandler()
        {
            OnLoop?.Invoke(this);
        }

        private void Run()
        {
            IsAlive = true;
            OnStart?.Invoke(this);
            while (IsAlive) {
                Event e;
                while (SDL2Helper.SDLCS.PollEvent(&e) != 0) {
                    if (e.Type == (uint)EventType.Quit)
                        Close();
                    this.EventHandler(e);
                }
                LoopHandler();
            }
            OnExit?.Invoke(this);
        }

        ~SDL2Window()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (hwnd != null) {
                SDL2Helper.SDLCS.DestroyWindow(hwnd);
                hwnd = null;
            }
            GC.SuppressFinalize(this);
        }

        #region Key, GamePad

        public bool EventHandler(Event e)
        {
            switch (e.Type) {

                case (uint)EventType.Windowevent:
                    if (e.Window.Event == (int)WindowEventID.Resized || e.Window.Event == (int)WindowEventID.SizeChanged) {
                        this.OnResized?.Invoke(this, null);
                    }
                    if (e.Window.Event == (int)WindowEventID.Moved) {
                        // TODO: 
                    }
                    break;

                #region MouseEvent

                case (uint)EventType.Mousewheel:
                    this.OnMouseWheel?.Invoke(this, e.Wheel);
                    return true;

                case (uint)EventType.Mousebuttondown:
                    this.OnMouseDown?.Invoke(this, e.Button);
                    return true;

                case (uint)EventType.Mousebuttonup:
                    this.OnMouseUp?.Invoke(this, e.Button);
                    return true;

                case (uint)EventType.Mousemotion:
                    this.OnMouseMove?.Invoke(this, e.Motion);
                    return true;

                #endregion MouseEvent

                #region KeyEvent
                case (uint)EventType.Textinput:
                    unsafe {
                        // THIS IS THE ONLY UNSAFE THING LEFT!
                        var str = Marshal.PtrToStringUTF8(new IntPtr(e.Text.Text));
                        this.OnTextInput?.Invoke(this, str);
                    }
                    return true;

                case (uint)EventType.Keydown:
                case (uint)EventType.Keyup:
                    if (e.Type == (uint)EventType.Keydown) {
                        this.OnKeyDown?.Invoke(this, e.Key);
                    }
                    else {
                        this.OnKeyUp?.Invoke(this, e.Key);
                    }
                    break;

                #endregion KeyEvent

                #region GamepadEvent

                case (uint)EventType.Controllerdeviceadded:
                    this.OnAddController?.Invoke(this, null);
                    break;

                case (uint)EventType.Controllerdeviceremoved:
                    this.OnRemoveController?.Invoke(this, null);
                    break;

                    #endregion GamepadEvent
            }
            return true;
        }

        #endregion
    }
}