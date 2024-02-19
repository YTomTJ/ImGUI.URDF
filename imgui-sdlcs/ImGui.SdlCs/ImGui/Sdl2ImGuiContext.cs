using ImGuiExt.SDL;
using ImGuiNET;
using OpenTK.Graphics.OpenGL;
using Silk.NET.SDL;
using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;

namespace ImGuiExt;

public class Sdl2ImGuiContext : IDisposable
{
    public IWindow Window { get; }

    public bool ShouldSwapBuffer { get; set; } = true;

    public Vector4? BackgroundColor { get; set; } = null;

    public bool ImWantMouse => ImGui.GetIO().WantCaptureMouse;

    public bool ImWantKeyboard => ImGui.GetIO().WantCaptureKeyboard;

    public delegate bool LayoutUpdateMethod();
    /// <summary>
    /// Return false will remove the delegate on end.
    /// </summary>
    public event LayoutUpdateMethod? OnLayoutUpdate;

    public delegate void WindowStartMethod(Sdl2ImGuiContext window);
    public event WindowStartMethod? OnWindowStart;

    public delegate void WindowExitMethod(Sdl2ImGuiContext window);
    public event WindowExitMethod? OnWindowExit;

    //---------------------------------------------------------------------

    public unsafe Sdl2ImGuiContext(string title = "Sdl2ImGuiContext", int width = 1280, int height = 760,
        WindowFlags flags = WindowFlags.Opengl | WindowFlags.Resizable | WindowFlags.Shown)
        : this(new SDL2Window(title, width, height, flags))
    {
    }

    public unsafe Sdl2ImGuiContext(IWindow window)
    {
        this.Window = window;

        // Initialize OpenTK
        GL.LoadBindings(new ImGuiTkContext());

        this.Initialize();
    }

    #region Initialize

    public void Initialize()
    {
        ImGui.CreateContext();
        ImGuiIOPtr io = ImGui.GetIO();
        unsafe {
            // FIXME: 支持多窗口？
            io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;
            io.ConfigFlags |= ImGuiConfigFlags.NavEnableGamepad;
            io.BackendFlags |= ImGuiBackendFlags.HasMouseCursors;       // We can honor GetMouseCursor() values (optional)
            io.BackendFlags |= ImGuiBackendFlags.HasSetMousePos;        // We can honor io.WantSetMousePos requests (optional, rarely used)
            io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;  // We can honor the ImDrawCmd::VtxOffset field, allowing for large meshes.

            //io.SetClipboardTextFn = SDL2Helper.SetClipboardText;
            //io.GetClipboardTextFn = SDL2Helper.GetClipboardText;
            //io.ClipboardUserData = null;
            //io.SetPlatformImeDataFn = SDL2Helper.SetPlatformImeData;

            // Set platform dependent data in viewport
            // Our mouse update function expect PlatformHandle to be filled for the main viewport
            var viewport = ImGui.GetMainViewport();
            viewport.PlatformHandleRaw = IntPtr.Zero;
            var wminfo = new SysWMInfo();
            SDL2Helper.SDLCS.GetVersion(&wminfo.Version);
            if (SDL2Helper.SDLCS.GetWindowWMInfo((Silk.NET.SDL.Window*)this.Window.HWnd, &wminfo)) {
                viewport.PlatformHandleRaw = wminfo.Info.Win.Hwnd;
            }
        }
        io.DisplaySize = new Vector2(this.Window.Size.Width, this.Window.Size.Height);
        CreateDeviceObjects();
        AddEvents();
    }

    private int vboHandle;
    private int vbaHandle;
    private int elementsHandle;
    private int attribLocationTex;
    private int attribLocationProjMtx;
    private int attribLocationVtxPos;
    private int attribLocationVtxUV;
    private int attribLocationVtxColor;
    private int shaderProgram;
    private int shader_vs;
    private int shader_fs;
    private int FontTexture;

    private void CreateDeviceObjects()
    {
        int last_texture, last_array_buffer;
        last_texture = GL.GetInteger(GetPName.TextureBinding2D);
        last_array_buffer = GL.GetInteger(GetPName.ArrayBufferBinding);

        const string vertex_shader_glsl_440_core =
  @"#version 440 core
layout (location = 0) in vec2 Position;
layout (location = 1) in vec2 UV;
layout (location = 2) in vec4 Color;
layout (location = 10) uniform mat4 ProjMtx;
out vec2 Frag_UV;
out vec4 Frag_Color;
void main()
{
  Frag_UV = UV;
  Frag_Color = Color;
  gl_Position = ProjMtx * vec4(Position.xy,0,1);
}";
        const string fragment_shader_glsl_440_core =
  @"#version 440 core
in vec2 Frag_UV;
in vec4 Frag_Color;
layout (location = 20) uniform sampler2D Texture;
layout (location = 0) out vec4 Out_Color;
void main()
{
    Out_Color = Frag_Color * texture(Texture, Frag_UV.st);
}";

        shader_vs = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(shader_vs, vertex_shader_glsl_440_core);
        GL.CompileShader(shader_vs);
        var info = GL.GetShaderInfoLog(shader_vs);
        if (!string.IsNullOrWhiteSpace(info))
            Debug.WriteLine($"GL.CompileShader [VertexShader] had info log: {info}");

        shader_fs = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(shader_fs, fragment_shader_glsl_440_core);
        GL.CompileShader(shader_fs);
        info = GL.GetShaderInfoLog(shader_fs);
        if (!string.IsNullOrWhiteSpace(info))
            Debug.WriteLine($"GL.CompileShader [VertexShader] had info log: {info}");
        shaderProgram = GL.CreateProgram();
        GL.AttachShader(shaderProgram, shader_vs);
        GL.AttachShader(shaderProgram, shader_fs);
        GL.LinkProgram(shaderProgram);
        info = GL.GetProgramInfoLog(shaderProgram);
        if (!string.IsNullOrWhiteSpace(info))
            Debug.WriteLine($"GL.LinkProgram had info log: {info}");

        attribLocationTex = 20;          // glGetUniformLocation(g_ShaderHandle, "Texture");
        attribLocationProjMtx = 10;      // glGetUniformLocation(g_ShaderHandle, "ProjMtx");
        attribLocationVtxPos = 0;        // glGetAttribLocation(g_ShaderHandle, "Position");
        attribLocationVtxUV = 1;         // glGetAttribLocation(g_ShaderHandle, "UV");
        attribLocationVtxColor = 2;      // glGetAttribLocation(g_ShaderHandle, "Color");
        vboHandle = GL.GenBuffer();      // SetupRenderState
        vbaHandle = GL.GenVertexArray(); // SetupRenderState
        elementsHandle = GL.GenBuffer(); // SetupRenderState

        // Restore modified GL state
        GL.BindTexture(TextureTarget.Texture2D, last_texture);
        GL.BindBuffer(BufferTarget.ArrayBuffer, last_array_buffer);
    }

    private unsafe bool CreateFontsTexture()
    {
        ImGuiIOPtr io = ImGui.GetIO();

        byte* pixels;
        int width, height;
        io.Fonts.GetTexDataAsRGBA32(out pixels, out width, out height);

        // Upload texture to graphics system
        int last_texture;
        GL.GetInteger(GetPName.TextureBinding2D, out last_texture);

        FontTexture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, FontTexture);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0,
            OpenTK.Graphics.OpenGL.PixelFormat.Rgba,
            OpenTK.Graphics.OpenGL.PixelType.UnsignedByte, (IntPtr)pixels);

        // Store our identifier
        io.Fonts.TexID = (nint)FontTexture;

        // Restore state
        GL.BindTexture(TextureTarget.Texture2D, last_texture);
        return true;
    }

    #endregion

    #region Events

    private void AddEvents()
    {
        Debug.Assert(this.Window != null);
        this.Window.OnStart += OnStartHander;
        this.Window.OnLoop += OnLoopHandler;
        this.Window.OnExit += OnExitHandler;

        this.Window.OnKeyDown += (s, e) => {
            if (e is KeyboardEvent ke) {
                ImGuiIOPtr io = ImGui.GetIO();
                UpdateKeyModifiers((Keymod)ke.Keysym.Mod);
                ImGuiKey key = SDL2Helper.KeycodeToImGuiKey((KeyCode)ke.Keysym.Sym);
                io.AddKeyEvent(key, true);
                io.SetKeyEventNativeData(key, (int)ke.Keysym.Sym, (int)ke.Keysym.Scancode, (int)ke.Keysym.Scancode);
                // To support legacy indexing (<1.87 user code). Legacy backend uses SDLK_*** as indices to IsKeyXXX() functions.
            }
        };
        this.Window.OnKeyUp += (s, e) => {
            if (e is KeyboardEvent ke) {
                ImGuiIOPtr io = ImGui.GetIO();
                UpdateKeyModifiers((Keymod)ke.Keysym.Mod);
                ImGuiKey key = SDL2Helper.KeycodeToImGuiKey((KeyCode)ke.Keysym.Sym);
                io.AddKeyEvent(key, false);
                io.SetKeyEventNativeData(key, (int)ke.Keysym.Sym, (int)ke.Keysym.Scancode, (int)ke.Keysym.Scancode);
            }
        };

        this.Window.OnMouseDown += (s, e) => {
            if (e is MouseButtonEvent mbe) {
                ImGuiIOPtr io = ImGui.GetIO();
                if (mbe.Button == Sdl.ButtonLeft)
                    io.MouseDown[0] = true;
                if (mbe.Button == Sdl.ButtonRight)
                    io.MouseDown[1] = true;
                if (mbe.Button == Sdl.ButtonMiddle)
                    io.MouseDown[2] = true;
            }
        };
        this.Window.OnMouseUp += (s, e) => {
            if (e is MouseButtonEvent mbe) {
                ImGuiIOPtr io = ImGui.GetIO();
                if (mbe.Button == Sdl.ButtonLeft)
                    io.MouseDown[0] = false;
                if (mbe.Button == Sdl.ButtonRight)
                    io.MouseDown[1] = false;
                if (mbe.Button == Sdl.ButtonMiddle)
                    io.MouseDown[2] = false;
            }
        };

        this.Window.OnMouseMove += (s, e) => {
            if (e is MouseMotionEvent mme) {
                ImGuiIOPtr io = ImGui.GetIO();
                io.MousePos = new Vector2(mme.X, mme.Y);
            }
        };

        this.Window.OnMouseWheel += (s, e) => {
            if (e is MouseWheelEvent mwe) {
                ImGuiIOPtr io = ImGui.GetIO();
                if (mwe.Y > 0)
                    io.MouseWheel = 1;
                if (mwe.Y < 0)
                    io.MouseWheel = -1;
            }
        };

        this.Window.OnAddController += (s, e) => {
            ImGuiIOPtr io = ImGui.GetIO();
            IsGamepadEnable = true;
            io.BackendFlags |= ImGuiBackendFlags.HasGamepad;
        };
        this.Window.OnRemoveController += (s, e) => {
            ImGuiIOPtr io = ImGui.GetIO();
            IsGamepadEnable = false;
            io.BackendFlags &= ~ImGuiBackendFlags.HasGamepad;
        };

        this.Window.OnTextInput += (s, e) => {
            if (e is string str) {
                ImGuiIOPtr io = ImGui.GetIO();
                io.AddInputCharactersUTF8(str);
            }
        };
    }

    private void OnStartHander(IWindow window)
    {
        OnWindowStart?.Invoke(this);
    }

    private void OnExitHandler(IWindow window)
    {
        OnWindowExit?.Invoke(this);
    }

    private void UpdateKeyModifiers(Keymod sdl_key_mods)
    {
        ImGuiIOPtr io = ImGui.GetIO();
        io.AddKeyEvent(ImGuiKey.ModCtrl, (sdl_key_mods & Keymod.Ctrl) != 0);
        io.AddKeyEvent(ImGuiKey.ModShift, (sdl_key_mods & Keymod.Shift) != 0);
        io.AddKeyEvent(ImGuiKey.ModAlt, (sdl_key_mods & Keymod.Alt) != 0);
        io.AddKeyEvent(ImGuiKey.ModSuper, (sdl_key_mods & Keymod.Gui) != 0);
    }

    #endregion

    #region Update

    private unsafe void OnLoopHandler(IWindow window)
    {
        this.UpdateGamepads();
        this.BeginNewFrame();

        ImGui.NewFrame();
        this.UpdateLayout();
        ImGui.Render();

        if (BackgroundColor.HasValue) {
            GL.ClearColor(BackgroundColor.Value.X, BackgroundColor.Value.Y, BackgroundColor.Value.Z, BackgroundColor.Value.W);
            GL.Clear(ClearBufferMask.ColorBufferBit);
        }
        this.RenderDrawData(ImGui.GetDrawData());
        if (ShouldSwapBuffer) {
            SDL2Helper.SDLCS.GLSwapWindow((Window*)this.Window.HWnd);
        }
    }

    protected void UpdateLayout()
    {
        if (OnLayoutUpdate == null) {
            ImGui.Text($"Create a new class inheriting {GetType().FullName}, overriding {nameof(UpdateLayout)}!");
        }
        else {
            foreach (LayoutUpdateMethod del in OnLayoutUpdate.GetInvocationList()) {
                if (!del()) {
                    OnLayoutUpdate -= del;
                }
            }
        }
    }

    #endregion

    #region Draw Backend

    private ulong uTime;
    private int imDrawVertSize = Marshal.SizeOf(default(ImDrawVert));

    public unsafe void BeginNewFrame()
    {
        Debug.Assert(this.Window != null);
        if (FontTexture == 0) {
            this.CreateFontsTexture();
        }

        ImGuiIOPtr io = ImGui.GetIO();
        int w, h;
        SDL2Helper.SDLCS.GetWindowSize((Window*)this.Window.HWnd, &w, &h);
        if ((SDL2Helper.SDLCS.GetWindowFlags((Window*)this.Window.HWnd) & (uint)WindowFlags.Minimized) > 0)
            w = h = 0;
        int display_w, display_h;
        SDL2Helper.SDLCS.GLGetDrawableSize((Window*)this.Window.HWnd, &display_w, &display_h);
        io.DisplaySize = new Vector2((float)w, (float)h);
        if (w > 0 && h > 0) {
            io.DisplayFramebufferScale = new Vector2(1.0f * display_w / w, 1.0f * display_h / h);
        }

        // Setup time step
        ulong current_time = SDL2Helper.SDLCS.GetPerformanceCounter();
        ulong frequency = SDL2Helper.SDLCS.GetPerformanceFrequency();
        if (current_time <= uTime)
            current_time = uTime + 1;
        io.DeltaTime = uTime > 0 ? (float)((double)(current_time - uTime) / frequency) : (float)(1.0f / 60.0f);
        uTime = current_time;

        SDL2Helper.SDLCS.ShowCursor(io.MouseDrawCursor ? 0 : 1);
    }

    public void RenderDrawData(ImDrawDataPtr draw_data)
    {
        var io = ImGui.GetIO();

        // Backup GL state
        int last_active_texture = GL.GetInteger(GetPName.ActiveTexture);
        int last_program = GL.GetInteger(GetPName.CurrentProgram);
        int last_texture = GL.GetInteger(GetPName.TextureBinding2D);
        int last_sampler = GL.GetInteger(GetPName.SamplerBinding);
        int last_array_buffer = GL.GetInteger(GetPName.ColorArrayBufferBinding);
        int[] last_polygon_mode = new int[2]; GL.GetInteger(GetPName.PolygonMode, last_polygon_mode);
        //int[] last_viewport = new int[4]; GL.GetInteger(GetPName.Viewport, last_viewport);
        int[] last_scissor_box = new int[4]; GL.GetInteger(GetPName.ScissorBox, last_scissor_box);
        int last_blend_src_rgb = GL.GetInteger(GetPName.BlendSrcRgb);
        int last_blend_dst_rgb = GL.GetInteger(GetPName.BlendDstRgb);
        int last_blend_src_alpha = GL.GetInteger(GetPName.BlendSrcAlpha);
        int last_blend_dst_alpha = GL.GetInteger(GetPName.BlendDstAlpha);
        int last_blend_equation_rgb = GL.GetInteger(GetPName.BlendEquationRgb);
        int last_blend_equation_alpha = GL.GetInteger(GetPName.BlendEquationAlpha);
        bool last_enable_blend = GL.IsEnabled(EnableCap.Blend);
        bool last_enable_cull_face = GL.IsEnabled(EnableCap.CullFace);
        bool last_enable_depth_test = GL.IsEnabled(EnableCap.DepthTest);
        bool last_enable_scissor_test = GL.IsEnabled(EnableCap.ScissorTest);
        bool clip_origin_lower_left = true;
        GL.ActiveTexture(TextureUnit.Texture0);

        // Setup desired GL state
        // Recreate the VAO every time (this is to easily allow multiple GL contexts to be rendered to. VAO are not shared among GL contexts)
        // The renderer would actually work without any VAO bound, but then our VertexAttrib calls would overwrite the default one currently bound.
        uint vertex_array_object = 0;
        this.SetupRenderState(draw_data, vertex_array_object);

        // Will project scissor/clipping rectangles into framebuffer space
        var clip_off = draw_data.DisplayPos;         // (0,0) unless using multi-viewports
        var clip_scale = draw_data.FramebufferScale; // (1,1) unless using retina display which are often (2,2)

        // Render command lists
        for (int n = 0; n < draw_data.CmdListsCount; n++) {
            var cmd_list = draw_data.CmdLists[n];


            GL.BufferData(BufferTarget.ArrayBuffer, cmd_list.VtxBuffer.Size * imDrawVertSize, cmd_list.VtxBuffer.Data, BufferUsageHint.StreamDraw);
            GL.BufferData(BufferTarget.ElementArrayBuffer, cmd_list.IdxBuffer.Size * sizeof(ushort), cmd_list.IdxBuffer.Data, BufferUsageHint.StreamDraw);

            for (int cmd_i = 0; cmd_i < cmd_list.CmdBuffer.Size; cmd_i++) {
                var pcmd = cmd_list.CmdBuffer[cmd_i];
                if (pcmd.UserCallback != IntPtr.Zero) {
                    // User callback, registered via ImDrawList::AddCallback()
                    // (ImDrawCallback_ResetRenderState is a special callback value used by the user to request the renderer to reset render state.)
                    //if (pcmd.UserCallback == ImGui. ImDrawCallback_ResetRenderState)
                    //  ImGui_ImplOpenGL3_SetupRenderState(draw_data, fb_width, fb_height, vertex_array_object);
                    //else
                    //pcmd->UserCallback(cmd_list, pcmd);
                    Debug.WriteLine("UserCallback" + pcmd.UserCallback.ToString());
                }
                else {
                    // Project scissor/clipping rectangles into framebuffer space
                    System.Numerics.Vector4 clip_rect = new System.Numerics.Vector4();
                    clip_rect.X = (pcmd.ClipRect.X - clip_off.X) * clip_scale.X;
                    clip_rect.Y = (pcmd.ClipRect.Y - clip_off.Y) * clip_scale.Y;
                    clip_rect.Z = (pcmd.ClipRect.Z - clip_off.X) * clip_scale.X;
                    clip_rect.W = (pcmd.ClipRect.W - clip_off.Y) * clip_scale.Y;

                    if (clip_rect.X < io.DisplaySize.X && clip_rect.Y < io.DisplaySize.Y && clip_rect.Z >= 0.0f && clip_rect.W >= 0.0f) {
                        // Apply scissor/clipping rectangle
                        if (clip_origin_lower_left)
                            GL.Scissor((int)clip_rect.X, (int)(io.DisplaySize.Y - clip_rect.W), (int)(clip_rect.Z - clip_rect.X), (int)(clip_rect.W - clip_rect.Y));
                        else

                            GL.Scissor((int)clip_rect.X, (int)clip_rect.Y, (int)clip_rect.Z, (int)clip_rect.W); // Support for GL 4.5 rarely used glClipControl(GL_UPPER_LEFT)

                        // Bind texture, Draw
                        GL.BindTexture(TextureTarget.Texture2D, pcmd.TextureId.ToInt32());

                        GL.DrawElementsBaseVertex(PrimitiveType.Triangles, (int)pcmd.ElemCount, DrawElementsType.UnsignedShort, new IntPtr(pcmd.IdxOffset * sizeof(ushort)), (int)pcmd.VtxOffset);
                        //If glDrawElementsBaseVertex not supported
                        //GL.DrawElements(BeginMode.Triangles, pcmd.ElemCount, sizeof(ImDrawIdx) == 2 ? GL_UNSIGNED_SHORT : GL_UNSIGNED_INT, (void*)(intptr_t)(pcmd->IdxOffset * sizeof(ImDrawIdx)));
                    }
                }
            }
        }

        // Restore modified GL state
        GL.UseProgram(last_program);
        GL.BindTexture(TextureTarget.Texture2D, last_texture);
        GL.BindSampler(0, last_sampler);
        GL.ActiveTexture((TextureUnit)last_active_texture);
        GL.BindBuffer(BufferTarget.ArrayBuffer, last_array_buffer);
        GL.BlendEquationSeparate((BlendEquationMode)last_blend_equation_rgb, (BlendEquationMode)last_blend_equation_alpha);
        GL.BlendFuncSeparate((BlendingFactorSrc)last_blend_src_rgb, (BlendingFactorDest)last_blend_dst_rgb, (BlendingFactorSrc)last_blend_src_alpha, (BlendingFactorDest)last_blend_dst_alpha);
        if (last_enable_blend) GL.Enable(EnableCap.Blend); else GL.Disable(EnableCap.Blend);
        if (last_enable_cull_face) GL.Enable(EnableCap.CullFace); else GL.Disable(EnableCap.CullFace);
        if (last_enable_depth_test) GL.Enable(EnableCap.DepthTest); else GL.Disable(EnableCap.DepthTest);
        if (last_enable_scissor_test) GL.Enable(EnableCap.ScissorTest); else GL.Disable(EnableCap.ScissorTest);
        GL.PolygonMode(MaterialFace.FrontAndBack, (PolygonMode)last_polygon_mode[0]);
        //GL.Viewport(last_viewport[0], last_viewport[1], last_viewport[2], last_viewport[3]);
        GL.Scissor(last_scissor_box[0], last_scissor_box[1], last_scissor_box[2], last_scissor_box[3]);
        GL.DisableVertexAttribArray(attribLocationVtxPos);
        GL.DisableVertexAttribArray(attribLocationVtxUV);
        GL.DisableVertexAttribArray(attribLocationVtxColor);

    }

    private void SetupRenderState(ImDrawDataPtr draw_data, uint vertex_array_object)
    {
        // Setup render state: alpha-blending enabled, no face culling, no depth testing, scissor enabled, polygon fill
        GL.Enable(EnableCap.Blend);
        GL.BlendEquation(BlendEquationMode.FuncAdd);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.Disable(EnableCap.CullFace);
        GL.Disable(EnableCap.DepthTest);
        GL.Enable(EnableCap.ScissorTest);
        GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);

        // Setup viewport, orthographic projection matrix
        // Our visible imgui space lies from draw_data->DisplayPos (top left) to draw_data->DisplayPos+data_data->DisplaySize (bottom right). DisplayPos is (0,0) for single viewport apps.
        //glViewport(0, 0, (GLsizei)fb_width, (GLsizei)fb_height);
        float L = draw_data.DisplayPos.X;
        float R = draw_data.DisplayPos.X + draw_data.DisplaySize.X;
        float T = draw_data.DisplayPos.Y;
        float B = draw_data.DisplayPos.Y + draw_data.DisplaySize.Y;
        OpenTK.Mathematics.Matrix4 ortho_projection = new OpenTK.Mathematics.Matrix4(
          2.0f / (R - L), 0.0f, 0.0f, 0.0f,
          0.0f, 2.0f / (T - B), 0.0f, 0.0f,
          0.0f, 0.0f, -1.0f, 0.0f,
          (R + L) / (L - R), (T + B) / (B - T), 0.0f, 1.0f
          );

        GL.UseProgram(shaderProgram);
        GL.Uniform1(attribLocationTex, 0);
        GL.UniformMatrix4(attribLocationProjMtx, false, ref ortho_projection);
        GL.BindSampler(0, 0); // We use combined texture/sampler state. Applications using GL 3.3 may set that otherwise.

        // Bind vertex/index buffers and setup attributes for ImDrawVert
        GL.BindVertexArray(vbaHandle);
        GL.BindBuffer(BufferTarget.ArrayBuffer, vboHandle);
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, elementsHandle);
        GL.EnableVertexAttribArray(attribLocationVtxPos);
        GL.EnableVertexAttribArray(attribLocationVtxUV);
        GL.EnableVertexAttribArray(attribLocationVtxColor);

        GL.VertexAttribPointer(attribLocationVtxPos, 2, VertexAttribPointerType.Float, false, imDrawVertSize, 0);
        GL.VertexAttribPointer(attribLocationVtxUV, 2, VertexAttribPointerType.Float, false, imDrawVertSize, 8);
        GL.VertexAttribPointer(attribLocationVtxColor, 4, VertexAttribPointerType.UnsignedByte, true, imDrawVertSize, 16);
    }

    #endregion Draw call

    #region Gamepad

    private bool IsGamepadEnable = false;
    private unsafe GameController* game_controller = null;
    private const int thumb_dead_zone = 8000;     /// SDL_gamecontroller.h suggests using this value.

    public unsafe void InitGamepads()
    {
        var io = ImGui.GetIO();
        if ((io.ConfigFlags | ImGuiConfigFlags.NavEnableGamepad) == 0) {
            CloseGamepads();
            return;
        }
        if (game_controller == null) {
            game_controller = SDL2Helper.SDLCS.GameControllerOpen(0);
        }
        io.NavActive = true;
    }

    public unsafe void CloseGamepads()
    {
        if (game_controller != null) {
            SDL2Helper.SDLCS.GameControllerClose(game_controller);
            game_controller = null;
        }
        var io = ImGui.GetIO();
        io.NavActive = false;
    }

    public unsafe void UpdateGamepads()
    {
        if (IsGamepadEnable) {
            InitGamepads();
            MAP_BUTTON(ImGuiKey.GamepadStart, GameControllerButton.Start);
            MAP_BUTTON(ImGuiKey.GamepadBack, GameControllerButton.Back);
            MAP_BUTTON(ImGuiKey.GamepadFaceLeft, GameControllerButton.X); // Xbox X, PS Square
            MAP_BUTTON(ImGuiKey.GamepadFaceRight, GameControllerButton.B); // Xbox B, PS Circle
            MAP_BUTTON(ImGuiKey.GamepadFaceUp, GameControllerButton.Y); // Xbox Y, PS Triangle
            MAP_BUTTON(ImGuiKey.GamepadFaceDown, GameControllerButton.A); // Xbox A, PS Cross
            MAP_BUTTON(ImGuiKey.GamepadDpadLeft, GameControllerButton.DpadLeft);
            MAP_BUTTON(ImGuiKey.GamepadDpadRight, GameControllerButton.DpadRight);
            MAP_BUTTON(ImGuiKey.GamepadDpadUp, GameControllerButton.DpadUp);
            MAP_BUTTON(ImGuiKey.GamepadDpadDown, GameControllerButton.DpadDown);
            MAP_BUTTON(ImGuiKey.GamepadL1, GameControllerButton.Leftshoulder);
            MAP_BUTTON(ImGuiKey.GamepadR1, GameControllerButton.Rightshoulder);
            MAP_ANALOG(ImGuiKey.GamepadL2, GameControllerAxis.Triggerleft, 0, 32767);
            MAP_ANALOG(ImGuiKey.GamepadR2, GameControllerAxis.Triggerright, 0, 32767);
            MAP_BUTTON(ImGuiKey.GamepadL3, GameControllerButton.Leftstick);
            MAP_BUTTON(ImGuiKey.GamepadR3, GameControllerButton.Rightstick);
            MAP_ANALOG(ImGuiKey.GamepadLStickLeft, GameControllerAxis.Leftx, -thumb_dead_zone, -32768);
            MAP_ANALOG(ImGuiKey.GamepadLStickRight, GameControllerAxis.Leftx, +thumb_dead_zone, +32767);
            MAP_ANALOG(ImGuiKey.GamepadLStickUp, GameControllerAxis.Lefty, -thumb_dead_zone, -32768);
            MAP_ANALOG(ImGuiKey.GamepadLStickDown, GameControllerAxis.Lefty, +thumb_dead_zone, +32767);
            MAP_ANALOG(ImGuiKey.GamepadRStickLeft, GameControllerAxis.Rightx, -thumb_dead_zone, -32768);
            MAP_ANALOG(ImGuiKey.GamepadRStickRight, GameControllerAxis.Rightx, +thumb_dead_zone, +32767);
            MAP_ANALOG(ImGuiKey.GamepadRStickUp, GameControllerAxis.Righty, -thumb_dead_zone, -32768);
            MAP_ANALOG(ImGuiKey.GamepadRStickDown, GameControllerAxis.Righty, +thumb_dead_zone, +32767);
        }
        else {
            CloseGamepads();
        }
    }

    private unsafe void MAP_BUTTON(ImGuiKey key, GameControllerButton button)
    {
        var io = ImGui.GetIO();
        io.AddKeyEvent(key, SDL2Helper.SDLCS.GameControllerGetButton(game_controller, button) != 0);
    }

    private unsafe void MAP_ANALOG(ImGuiKey key, GameControllerAxis axis, int v0, int v1)
    {
        var io = ImGui.GetIO();
        float vn = (float)(SDL2Helper.SDLCS.GameControllerGetAxis(game_controller, axis) - v0) / (float)(v1 - v0);
        vn = Math.Clamp(vn, 0.0f, 1.0f);
        io.AddKeyAnalogEvent(key, vn > 0.1f, vn);
    }

    #endregion GamePad

    #region Destroy

    private void DestroyDeviceObjects()
    {
        GL.DeleteVertexArray(vbaHandle);
        GL.DeleteBuffer(vboHandle);
        GL.DeleteBuffer(elementsHandle);
        GL.DetachShader(shaderProgram, shader_vs);
        GL.DetachShader(shaderProgram, shader_fs);
        GL.DeleteProgram(shaderProgram);
    }

    void DestroyFontsTexture()
    {
        var io = ImGui.GetIO();
        GL.DeleteTexture(FontTexture);
        io.Fonts.TexID = IntPtr.Zero;
        FontTexture = 0;
    }

    public void Dispose()
    {
        DestroyFontsTexture();
        DestroyDeviceObjects();
        ImGui.DestroyContext();
    }

    #endregion
}
