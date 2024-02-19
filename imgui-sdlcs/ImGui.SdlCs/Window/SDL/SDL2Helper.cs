using ImGuiNET;
using Silk.NET.SDL;
using System;
using System.Runtime.InteropServices;

namespace ImGuiExt.SDL
{
    public static class SDL2Helper
    {
        private static Sdl? _SDLCS = null;
        public static Sdl SDLCS
        {
            get {
                if (_SDLCS is null) {
                    _SDLCS = SdlProvider.SDL.Value;
                }
                return _SDLCS;
            }
        }

        /// <summary>
        /// Alternate for ImGui.GetClipboardText
        /// </summary>
        /// <returns></returns>
        public unsafe static string GetClipboardText()
        {
            try {
                return Marshal.PtrToStringUTF8((nint)SDLCS.GetClipboardText())!;
            }
            catch (Exception) {
                // TODO: deal with exception
                return "";
            }
        }

        /// <summary>
        /// Alternate for ImGui.SetClipboardText
        /// </summary>
        /// <param name="text"></param>
        public static void SetClipboardText(string text)
        {
            try {
                SDLCS.SetClipboardText(text);
            }
            catch (Exception) {
                // TODO: deal with exception
            }
        }

        public static ImGuiKey KeycodeToImGuiKey(KeyCode keycode)
        {
            switch (keycode) {
                case KeyCode.KTab: return ImGuiKey.Tab;
                case KeyCode.KLeft: return ImGuiKey.LeftArrow;
                case KeyCode.KRight: return ImGuiKey.RightArrow;
                case KeyCode.KUp: return ImGuiKey.UpArrow;
                case KeyCode.KDown: return ImGuiKey.DownArrow;
                case KeyCode.KPageup: return ImGuiKey.PageUp;
                case KeyCode.KPagedown: return ImGuiKey.PageDown;
                case KeyCode.KHome: return ImGuiKey.Home;
                case KeyCode.KEnd: return ImGuiKey.End;
                case KeyCode.KInsert: return ImGuiKey.Insert;
                case KeyCode.KDelete: return ImGuiKey.Delete;
                case KeyCode.KBackspace: return ImGuiKey.Backspace;
                case KeyCode.KSpace: return ImGuiKey.Space;
                case KeyCode.KReturn: return ImGuiKey.Enter;
                case KeyCode.KEscape: return ImGuiKey.Escape;
                case KeyCode.KQuote: return ImGuiKey.Apostrophe;
                case KeyCode.KComma: return ImGuiKey.Comma;
                case KeyCode.KMinus: return ImGuiKey.Minus;
                case KeyCode.KPeriod: return ImGuiKey.Period;
                case KeyCode.KSlash: return ImGuiKey.Slash;
                case KeyCode.KSemicolon: return ImGuiKey.Semicolon;
                case KeyCode.KEquals: return ImGuiKey.Equal;
                case KeyCode.KLeftbracket: return ImGuiKey.LeftBracket;
                case KeyCode.KBackslash: return ImGuiKey.Backslash;
                case KeyCode.KRightbracket: return ImGuiKey.RightBracket;
                case KeyCode.KBackquote: return ImGuiKey.GraveAccent;
                case KeyCode.KCapslock: return ImGuiKey.CapsLock;
                case KeyCode.KScrolllock: return ImGuiKey.ScrollLock;
                case KeyCode.KNumlockclear: return ImGuiKey.NumLock;
                case KeyCode.KPrintscreen: return ImGuiKey.PrintScreen;
                case KeyCode.KPause: return ImGuiKey.Pause;
                case KeyCode.KKP0: return ImGuiKey.Keypad0;
                case KeyCode.KKP1: return ImGuiKey.Keypad1;
                case KeyCode.KKP2: return ImGuiKey.Keypad2;
                case KeyCode.KKP3: return ImGuiKey.Keypad3;
                case KeyCode.KKP4: return ImGuiKey.Keypad4;
                case KeyCode.KKP5: return ImGuiKey.Keypad5;
                case KeyCode.KKP6: return ImGuiKey.Keypad6;
                case KeyCode.KKP7: return ImGuiKey.Keypad7;
                case KeyCode.KKP8: return ImGuiKey.Keypad8;
                case KeyCode.KKP9: return ImGuiKey.Keypad9;
                case KeyCode.KKPPeriod: return ImGuiKey.KeypadDecimal;
                case KeyCode.KKPDivide: return ImGuiKey.KeypadDivide;
                case KeyCode.KKPMultiply: return ImGuiKey.KeypadMultiply;
                case KeyCode.KKPMinus: return ImGuiKey.KeypadSubtract;
                case KeyCode.KKPPlus: return ImGuiKey.KeypadAdd;
                case KeyCode.KKPEnter: return ImGuiKey.KeypadEnter;
                case KeyCode.KKPEquals: return ImGuiKey.KeypadEqual;
                case KeyCode.KLctrl: return ImGuiKey.LeftCtrl;
                case KeyCode.KLshift: return ImGuiKey.LeftShift;
                case KeyCode.KLalt: return ImGuiKey.LeftAlt;
                case KeyCode.KLgui: return ImGuiKey.LeftSuper;
                case KeyCode.KRctrl: return ImGuiKey.RightCtrl;
                case KeyCode.KRshift: return ImGuiKey.RightShift;
                case KeyCode.KRalt: return ImGuiKey.RightAlt;
                case KeyCode.KRgui: return ImGuiKey.RightSuper;
                case KeyCode.KApplication: return ImGuiKey.Menu;
                case KeyCode.K0: return ImGuiKey._0;
                case KeyCode.K1: return ImGuiKey._1;
                case KeyCode.K2: return ImGuiKey._2;
                case KeyCode.K3: return ImGuiKey._3;
                case KeyCode.K4: return ImGuiKey._4;
                case KeyCode.K5: return ImGuiKey._5;
                case KeyCode.K6: return ImGuiKey._6;
                case KeyCode.K7: return ImGuiKey._7;
                case KeyCode.K8: return ImGuiKey._8;
                case KeyCode.K9: return ImGuiKey._9;
                case KeyCode.KA: return ImGuiKey.A;
                case KeyCode.KB: return ImGuiKey.B;
                case KeyCode.KC: return ImGuiKey.C;
                case KeyCode.KD: return ImGuiKey.D;
                case KeyCode.KE: return ImGuiKey.E;
                case KeyCode.KF: return ImGuiKey.F;
                case KeyCode.KG: return ImGuiKey.G;
                case KeyCode.KH: return ImGuiKey.H;
                case KeyCode.KI: return ImGuiKey.I;
                case KeyCode.KJ: return ImGuiKey.J;
                case KeyCode.KK: return ImGuiKey.K;
                case KeyCode.KL: return ImGuiKey.L;
                case KeyCode.KM: return ImGuiKey.M;
                case KeyCode.KN: return ImGuiKey.N;
                case KeyCode.KO: return ImGuiKey.O;
                case KeyCode.KP: return ImGuiKey.P;
                case KeyCode.KQ: return ImGuiKey.Q;
                case KeyCode.KR: return ImGuiKey.R;
                case KeyCode.KS: return ImGuiKey.S;
                case KeyCode.KT: return ImGuiKey.T;
                case KeyCode.KU: return ImGuiKey.U;
                case KeyCode.KV: return ImGuiKey.V;
                case KeyCode.KW: return ImGuiKey.W;
                case KeyCode.KX: return ImGuiKey.X;
                case KeyCode.KY: return ImGuiKey.Y;
                case KeyCode.KZ: return ImGuiKey.Z;
                case KeyCode.KF1: return ImGuiKey.F1;
                case KeyCode.KF2: return ImGuiKey.F2;
                case KeyCode.KF3: return ImGuiKey.F3;
                case KeyCode.KF4: return ImGuiKey.F4;
                case KeyCode.KF5: return ImGuiKey.F5;
                case KeyCode.KF6: return ImGuiKey.F6;
                case KeyCode.KF7: return ImGuiKey.F7;
                case KeyCode.KF8: return ImGuiKey.F8;
                case KeyCode.KF9: return ImGuiKey.F9;
                case KeyCode.KF10: return ImGuiKey.F10;
                case KeyCode.KF11: return ImGuiKey.F11;
                case KeyCode.KF12: return ImGuiKey.F12;
                case KeyCode.KF13: return ImGuiKey.F13;
                case KeyCode.KF14: return ImGuiKey.F14;
                case KeyCode.KF15: return ImGuiKey.F15;
                case KeyCode.KF16: return ImGuiKey.F16;
                case KeyCode.KF17: return ImGuiKey.F17;
                case KeyCode.KF18: return ImGuiKey.F18;
                case KeyCode.KF19: return ImGuiKey.F19;
                case KeyCode.KF20: return ImGuiKey.F20;
                case KeyCode.KF21: return ImGuiKey.F21;
                case KeyCode.KF22: return ImGuiKey.F22;
                case KeyCode.KF23: return ImGuiKey.F23;
                case KeyCode.KF24: return ImGuiKey.F24;
                case KeyCode.KACBack: return ImGuiKey.AppBack;
                case KeyCode.KACForward: return ImGuiKey.AppForward;
            }
            return ImGuiKey.None;
        }
    }
}