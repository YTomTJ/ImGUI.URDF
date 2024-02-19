using ImGuiExt.SDL;
using OpenTK;
using System;

namespace ImGuiExt;

public class ImGuiTkContext : IBindingsContext
{
    public unsafe IntPtr GetProcAddress(string procName) => (IntPtr)SDL2Helper.SDLCS.GLGetProcAddress(procName);
}