﻿using System.Diagnostics;
using System.Numerics;
using System.Reflection;

namespace ImGui3D;

public static class Util
{
    /// <summary>
    /// Converts a double in the [-1, 1] range to a normalized ushort.
    /// </summary>
    /// <param name="value">The value to convert. Must be in the [-1, 1] range.</param>
    /// <returns>A normalized value.</returns>
    public static ushort Normalize(double value)
    {
        value = Clamp(value, -1, 1);
        return (ushort)((value * 0.5 + 0.5) * ushort.MaxValue);
    }

    public static bool IsValidPath(string file)
    {
        try {
            Path.GetFullPath(file);
            return true;
        }
        catch {
            return false;
        }
    }

    /// <summary>
    /// Converts a double in the [-1, 1] range to a normalized short.
    /// </summary>
    /// <param name="value">The value to convert. Must be in the [-1, 1] range.</param>
    /// <returns>A normalized value.</returns>
    public static short DoubleToShort(double value)
    {
        value = Clamp(value, -1, 1);
        return (short)(value * short.MaxValue);
    }

    public static double Clamp(double value, double min, double max)
    {
        if (value <= min) {
            return min;
        }
        else if (value >= max) {
            return max;
        }
        else {
            return value;
        }
    }

    public static bool TryGetFileInfo(string fileName, out FileInfo realFile)
    {
        try {
            realFile = new FileInfo(fileName);
            return true;
        }
        catch {
            realFile = null;
            return false;
        }
    }

    public static float Clamp(float value, float min, float max)
    {
        if (value <= min) {
            return min;
        }
        else if (value >= max) {
            return max;
        }
        else {
            return value;
        }
    }

    public static int Clamp(int value, int min, int max)
    {
        if (value <= min) {
            return min;
        }
        else if (value >= max) {
            return max;
        }
        else {
            return value;
        }
    }

    public static void Mix(float[] a, float[] b, float[] result)
    {
        Debug.Assert(a.Length == b.Length && a.Length == result.Length);
        for (int i = 0; i < a.Length; i++) {
            result[i] = a[i] + b[i];
        }
    }

    public static uint Min(uint a, uint b)
    {
        if (a <= b) {
            return a;
        }
        else {
            return b;
        }
    }

    public static uint Max(uint a, uint b)
    {
        if (a > b) {
            return a;
        }
        else {
            return b;
        }
    }

    public static uint Argb(float a, float r, float g, float b)
    {
        return
            unchecked((uint)(
                (byte)(a * 255.0f) << 24
                | (byte)(r * 255.0f) << 0
                | (byte)(g * 255.0f) << 8
                | (byte)(b * 255.0f) << 16)
            );
    }


    public static uint RgbaToArgb(Vector4 color)
    {
        return Argb(color.W, color.X, color.Y, color.Z);
    }

    public static short[] FloatToShortNormalized(float[] total)
    {
        short[] normalized = new short[total.Length];
        for (int i = 0; i < total.Length; i++) {
            normalized[i] = DoubleToShort(total[i]);
        }

        return normalized;
    }

    //public static PatternTime CalculateFinalNoteEndTime(List<Note> notes)
    //{
    //    PatternTime latest = PatternTime.Zero;
    //    foreach (Note n in notes) {
    //        PatternTime noteEnd = n.StartTime + n.Duration;
    //        if (noteEnd > latest) {
    //            latest = noteEnd;
    //        }
    //    }
    //
    //    return latest;
    //}

    public static Type[] GetTypesWithAttribute(Assembly assembly, Type attributeType)
    {
        return assembly.DefinedTypes.Where(ti => ti.IsDefined(attributeType)).Select(ti => ti.AsType()).ToArray();
    }

    public static Vector4 GetColor(System.Drawing.Color col)
    {
        return new Vector4(col.R / 255.0f, col.G / 255.0f, col.B / 255.0f, 1.0f);
    }
}
