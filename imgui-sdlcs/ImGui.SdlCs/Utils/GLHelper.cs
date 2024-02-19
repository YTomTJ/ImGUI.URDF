using ImGuiNET;
using OpenTK.Graphics.OpenGL;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ImGuiExt
{
    public static class GLHelper
    {
        public static unsafe void RenderDrawData(ImDrawDataPtr drawData, int displayW, int displayH)
        {
            // We are using the OpenGL fixed pipeline to make the example code simpler to read!
            // Setup render state: alpha-blending enabled, no face culling, no depth testing, scissor enabled, vertex/texcoord/color pointers.
            int lastProgram;
            GL.GetInteger(GetPName.CurrentProgram, out lastProgram);
            int lastTexture;
            GL.GetInteger(GetPName.TextureBinding2D, out lastTexture);
            var lastViewport = new int[4];
            GL.GetInteger(GetPName.Viewport, lastViewport);
            var lastScissorBox = new int[4];
            GL.GetInteger(GetPName.ScissorBox, lastScissorBox);

            GL.PushAttrib(AttribMask.EnableBit | AttribMask.ColorBufferBit | AttribMask.TransformBit);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.Disable(EnableCap.CullFace);
            GL.Disable(EnableCap.DepthTest);
            GL.Enable(EnableCap.ScissorTest);
            GL.EnableClientState(ArrayCap.VertexArray);
            GL.EnableClientState(ArrayCap.TextureCoordArray);
            GL.EnableClientState(ArrayCap.ColorArray);
            GL.Enable(EnableCap.Texture2D);

            GL.UseProgram((uint)lastProgram);

            // Handle cases of screen coordinates != from framebuffer coordinates (e.g. retina displays)
            ImGuiIOPtr io = ImGui.GetIO();
            drawData.ScaleClipRects(io.DisplayFramebufferScale);

            // Setup orthographic projection matrix
            GL.Viewport(0, 0, displayW, displayH);
            GL.MatrixMode(MatrixMode.Projection);
            GL.PushMatrix();
            GL.LoadIdentity();
            GL.Ortho(
                0.0f,
                io.DisplaySize.X / io.DisplayFramebufferScale.X,
                io.DisplaySize.Y / io.DisplayFramebufferScale.Y,
                0.0f, -1.0f,
                1.0f
            );
            GL.MatrixMode(MatrixMode.Modelview);
            GL.PushMatrix();
            GL.LoadIdentity();

            // Render command lists
            for (int n = 0; n < drawData.CmdListsCount; n++) {
                ImDrawListPtr cmdList = new ImDrawListPtr(drawData.CmdLists[n]);
                var vtxBuffer = cmdList.VtxBuffer;
                var idxBuffer = cmdList.IdxBuffer;

                GL.VertexPointer(2, VertexPointerType.Float, Unsafe.SizeOf<ImDrawVert>(),
                    IntPtr.Add(vtxBuffer.Data, Marshal.OffsetOf<ImDrawVert>("pos").ToInt32()));
                GL.TexCoordPointer(2, TexCoordPointerType.Float, Unsafe.SizeOf<ImDrawVert>(),
                    IntPtr.Add(vtxBuffer.Data, Marshal.OffsetOf<ImDrawVert>("uv").ToInt32()));
                GL.ColorPointer(4, ColorPointerType.UnsignedByte, Unsafe.SizeOf<ImDrawVert>(),
                    IntPtr.Add(vtxBuffer.Data, Marshal.OffsetOf<ImDrawVert>("col").ToInt32()));

                long idxBufferOffset = 0;
                for (int cmdi = 0; cmdi < cmdList.CmdBuffer.Size; cmdi++) {
                    ImDrawCmdPtr pcmd = cmdList.CmdBuffer[cmdi];
                    if (pcmd.UserCallback != IntPtr.Zero) {
                        // TODO: pcmd.UserCallback.Invoke(ref cmdList, ref pcmd);
                        throw new NotImplementedException();
                    }
                    else {
                        GL.BindTexture(TextureTarget.Texture2D, (int)pcmd.TextureId);
                        GL.Scissor(
                            (int)pcmd.ClipRect.X,
                            (int)(io.DisplaySize.Y - pcmd.ClipRect.W),
                            (int)(pcmd.ClipRect.Z - pcmd.ClipRect.X),
                            (int)(pcmd.ClipRect.W - pcmd.ClipRect.Y)
                        );
                        GL.DrawElements(BeginMode.Triangles, (int)pcmd.ElemCount, DrawElementsType.UnsignedShort, new IntPtr((long)idxBuffer.Data + idxBufferOffset));
                    }
                    idxBufferOffset += pcmd.ElemCount * 2 /*sizeof(ushort)*/ ;
                }
            }

            // Restore modified state
            GL.DisableClientState(ArrayCap.ColorArray);
            GL.DisableClientState(ArrayCap.TextureCoordArray);
            GL.DisableClientState(ArrayCap.VertexArray);
            GL.BindTexture(TextureTarget.Texture2D, lastTexture);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.PopMatrix();
            GL.MatrixMode(MatrixMode.Projection);
            GL.PopMatrix();
            GL.PopAttrib();
            GL.Viewport(lastViewport[0], lastViewport[1], lastViewport[2], lastViewport[3]);
            GL.Scissor(lastScissorBox[0], lastScissorBox[1], lastScissorBox[2], lastScissorBox[3]);
        }
    }
}