//using ImGuiExt.SDL;
//using ImGuiNET;
//using Silk.NET.Maths;
//using Silk.NET.SDL;
//using System;
//using System.Diagnostics;
//using System.Numerics;
//using System.Runtime.InteropServices;

//namespace ImGuiExt
//{
//    /// <summary>
//    /// Basic window of ImGuiNET with SDL2 + OpenGL.
//    /// </summary>
//    public unsafe class SDL2_SdlRenderer_Window : ImGuiWindowBase
//    {
//        public override IntPtr Renderer { get; protected set; }
//        public override IntPtr FontTexture { get; protected set; }
//        public override ulong uTime { get; protected set; } = 0;

//        public SDL2_SdlRenderer_Window(string title = "SDL2_SdlRenderer_Window", int width = 1280, int height = 760,
//            WindowFlags flags = WindowFlags.Resizable | WindowFlags.AllowHighdpi | WindowFlags.Opengl)
//            : this(new SDL2Window(title, width, height, flags))
//        {
//        }

//        public SDL2_SdlRenderer_Window(IWindow window) : base(window)
//        {
//            // Create SDL_Renderer graphics context
//            Renderer = (nint)SDL2Helper.SDLCS.CreateRenderer((Window*)Window.HWnd, -1, (uint)(RendererFlags.Presentvsync | RendererFlags.Accelerated));
//            if (Renderer == IntPtr.Zero) {
//                throw new Exception("Error creating SDL_Renderer!");
//            }
//            RendererInfo info;
//            SDL2Helper.SDLCS.GetRendererInfo((Renderer*)Renderer, &info);
//            Debug.WriteLine(string.Format("Current SDL_Renderer: {0}", Marshal.PtrToStringUTF8((nint)info.Name)));

//            ImGui.CreateContext();
//            SDL2Helper.Initialize(Window.HWnd);
//            base.Initialize();
//        }

//        protected override unsafe void Create()
//        {
//        }

//        protected override void Render()
//        {
//            ImGuiIOPtr io = ImGui.GetIO();
//            SDL2Helper.SDLCS.RenderSetScale((Renderer*)Renderer, io.DisplayFramebufferScale.X, io.DisplayFramebufferScale.Y);

//            SDL2Helper.SDLCS.SetRenderDrawColor((Renderer*)Renderer, (byte)(BackgroundColor.X * 255), (byte)(BackgroundColor.Y * 255),
//                 (byte)(BackgroundColor.Z * 255), (byte)(BackgroundColor.W * 255));
//            SDL2Helper.SDLCS.RenderClear((Renderer*)Renderer);
//            RenderDrawData(ImGui.GetDrawData());
//            SDL2Helper.SDLCS.RenderPresent((Renderer*)Renderer);
//        }

//        protected override bool CreateFontsTexture()
//        {
//            var io = ImGui.GetIO();
//            var fonts = io.Fonts;

//            // Build texture atlas
//            // Load as RGBA 32-bit (75% of the memory is wasted, but default font is so small) because it is more
//            // likely to be compatible with user's existing shaders. If your ImTextureId represent a higher-level
//            // concept than just a GL texture id, consider calling GetTexDataAsAlpha8() instead to save on GPU memory.
//            fonts.GetTexDataAsRGBA32(out IntPtr pixels, out int width, out int height);

//            // Upload texture to graphics system
//            // (Bilinear sampling is required by default. Set 'io.Fonts->Flags |= ImFontAtlasFlags_NoBakedLines'
//            // or 'style.AntiAliasedLinesUseTex = false' to allow point/nearest sampling)
//            FontTexture = (nint)SDL2Helper.SDLCS.CreateTexture((Renderer*)Renderer, Sdl.PixelformatAbgr8888,
//                (int)TextureAccess.Static, width, height);
//            if (FontTexture == IntPtr.Zero) {
//                throw new Exception("Error creating texture.");
//            }
//            SDL2Helper.SDLCS.UpdateTexture((Texture*)FontTexture, null, (void*)pixels, 4 * width);
//            SDL2Helper.SDLCS.SetTextureBlendMode((Texture*)FontTexture, BlendMode.Blend);
//            SDL2Helper.SDLCS.SetTextureScaleMode((Texture*)FontTexture, ScaleMode.Linear);
//            // Store our identifier
//            io.Fonts.SetTexID(FontTexture);
//            return true;
//        }

//        protected override void DeleteFontsTexture()
//        {
//            SDL2Helper.SDLCS.DestroyTexture((Texture*)FontTexture);
//        }

//        private void RenderDrawData(ImDrawDataPtr draw_data)
//        {
//            float rsx, rsy;
//            SDL2Helper.SDLCS.RenderGetScale((Renderer*)Renderer, &rsx, &rsy);

//            Vector2 render_scale;
//            render_scale.X = (rsx == 1.0f) ? draw_data.FramebufferScale.X : 1.0f;
//            render_scale.Y = (rsy == 1.0f) ? draw_data.FramebufferScale.Y : 1.0f;

//            int fb_width = (int)(draw_data.DisplaySize.X * render_scale.X);
//            int fb_height = (int)(draw_data.DisplaySize.Y * render_scale.Y);
//            if (fb_width == 0 || fb_height == 0)
//                return;

//            Rectangle<int> old_Viewport;
//            Rectangle<int> old_ClipRect;
//            SDL2Helper.SDLCS.RenderGetViewport((Renderer*)Renderer, &old_Viewport);
//            SDL2Helper.SDLCS.RenderGetClipRect((Renderer*)Renderer, &old_ClipRect);

//            Vector2 clip_off = draw_data.DisplayPos;         // (0,0) unless using multi-viewports
//            Vector2 clip_scale = render_scale;

//            for (int n = 0; n < draw_data.CmdListsCount; n++) {
//                ImDrawListPtr cmd_list = draw_data.CmdLists[n];
//                IntPtr vtx_buffer = cmd_list.VtxBuffer.Data;
//                IntPtr idx_buffer = cmd_list.IdxBuffer.Data;

//                for (var cmd_i = 0; cmd_i < cmd_list.CmdBuffer.Size; cmd_i++) {
//                    var pcmd = cmd_list.CmdBuffer[cmd_i];
//                    if (pcmd.UserCallback != IntPtr.Zero) {
//                        Console.WriteLine("UserCallback not implemented");
//                    }
//                    else {
//                        // Project scissor/clipping rectangles into framebuffer space
//                        Vector2 clip_min = new Vector2((pcmd.ClipRect.X - clip_off.X) * clip_scale.X, (pcmd.ClipRect.Y - clip_off.Y) * clip_scale.Y);
//                        Vector2 clip_max = new Vector2((pcmd.ClipRect.Z - clip_off.X) * clip_scale.X, (pcmd.ClipRect.W - clip_off.Y) * clip_scale.Y);

//                        if (clip_min.X < 0.0f) { clip_min.X = 0.0f; }
//                        if (clip_min.Y < 0.0f) { clip_min.Y = 0.0f; }
//                        if (clip_max.X > (float)fb_width) { clip_max.X = (float)fb_width; }
//                        if (clip_max.Y > (float)fb_height) { clip_max.Y = (float)fb_height; }
//                        if (clip_max.X <= clip_min.X || clip_max.Y <= clip_min.Y)
//                            continue;

//                        var r = new Rectangle<int>(
//                            (int)(clip_min.X),
//                            (int)(clip_min.Y),
//                            (int)(clip_max.X - clip_min.X),
//                            (int)(clip_max.Y - clip_min.Y)
//                        );
//                        SDL2Helper.SDLCS.RenderSetClipRect((Renderer*)Renderer, &r);

//                        IntPtr xy_ptr = IntPtr.Add(vtx_buffer, (int)pcmd.VtxOffset);
//                        IntPtr uv_ptr = IntPtr.Add(vtx_buffer, (int)pcmd.VtxOffset + Marshal.OffsetOf<ImDrawVert>("uv").ToInt32());
//                        IntPtr col_ptr = IntPtr.Add(vtx_buffer, (int)pcmd.VtxOffset + Marshal.OffsetOf<ImDrawVert>("col").ToInt32());

//                        int vtx_size = Marshal.SizeOf<ImDrawVert>();
//                        int res = SDL2Helper.SDLCS.RenderGeometryRaw((Renderer*)Renderer, (Texture*)pcmd.GetTexID(),
//                            (float*)xy_ptr, vtx_size,
//                            (Color*)col_ptr, vtx_size,
//                            (float*)uv_ptr, vtx_size,
//                            cmd_list.VtxBuffer.Size - (int)pcmd.VtxOffset,
//                            IntPtr.Add(idx_buffer, (int)pcmd.IdxOffset),
//                            (int)pcmd.ElemCount,
//                            Marshal.SizeOf<ushort>()
//                        );

//                    }
//                }
//            }

//            // Restore modified SDL_Renderer state
//            SDL2Helper.SDLCS.RenderSetViewport((Renderer*)Renderer, &old_Viewport);
//            SDL2Helper.SDLCS.RenderSetClipRect((Renderer*)Renderer, &old_ClipRect);
//        }

//        protected override unsafe void RendererNewFrame()
//        {
//            Debug.Assert(this.Window != null);
//            if (FontTexture == IntPtr.Zero)
//                CreateFontsTexture();

//            ImGuiIOPtr io = ImGui.GetIO();
//            int w, h;
//            SDL2Helper.SDLCS.GetWindowSize((Window*)Window.HWnd, &w, &h);
//            if ((SDL2Helper.SDLCS.GetWindowFlags((Window*)Window.HWnd) & (uint)WindowFlags.Minimized) > 0)
//                w = h = 0;
//            int display_w, display_h;
//            if (Renderer != IntPtr.Zero)
//                SDL2Helper.SDLCS.GetRendererOutputSize((Renderer*)Renderer, &display_w, &display_h);
//            else
//                SDL2Helper.SDLCS.GLGetDrawableSize((Window*)Window.HWnd, &display_w, &display_h);
//            io.DisplaySize = new Vector2((float)w, (float)h);
//            if (w > 0 && h > 0) {
//                io.DisplayFramebufferScale = new Vector2(1.0f * display_w / w, 1.0f * display_h / h);
//            }

//            // Setup time step
//            ulong current_time = SDL2Helper.SDLCS.GetPerformanceCounter();
//            ulong frequency = SDL2Helper.SDLCS.GetPerformanceFrequency();
//            if (current_time <= uTime)
//                current_time = uTime + 1;
//            io.DeltaTime = uTime > 0 ? (float)((double)(current_time - uTime) / frequency) : (float)(1.0f / 60.0f);
//            uTime = current_time;

//            SDL2Helper.SDLCS.ShowCursor(io.MouseDrawCursor ? 0 : 1);
//        }
//    }
//}
