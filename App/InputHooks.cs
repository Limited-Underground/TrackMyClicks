// Copyright 2026 Nenad Bjelanovic
// SPDX-License-Identifier: Apache-2.0
using System.Runtime.InteropServices;
namespace TrackMyClicks;
internal sealed class InputHooks : IDisposable
{
    private readonly HookProc mouseProc, keyboardProc;
    private nint mouse, keyboard;
    private readonly HashSet<int> down = new();
    private POINT? last;
    public event Action<int>? Click;
    public event Action? Key;
    public event Action<double>? Move;
    public event Action<int>? Scroll;
    public InputHooks()
    {
        mouseProc = Mouse; keyboardProc = Keyboard;
        mouse = SetWindowsHookEx(14, mouseProc, GetModuleHandle(null), 0);
        if(mouse==0) { int error=Marshal.GetLastWin32Error(); throw new System.ComponentModel.Win32Exception(error); }
        keyboard = SetWindowsHookEx(13, keyboardProc, GetModuleHandle(null), 0);
        if (mouse == 0 || keyboard == 0) { int error=Marshal.GetLastWin32Error(); Dispose(); throw new System.ComponentModel.Win32Exception(error); }
    }
    private nint Mouse(int code, nint message, nint data)
    {
        if (code >= 0)
        {
            var info = Marshal.PtrToStructure<MouseInfo>(data);
            switch ((int)message)
            {
                case 0x201: Click?.Invoke(0); break;
                case 0x204: Click?.Invoke(1); break;
                case 0x207: Click?.Invoke(2); break;
                case 0x200:
                    if (last is POINT p) { double dx = (double)info.pt.x-p.x, dy=(double)info.pt.y-p.y; Move?.Invoke(Math.Sqrt(dx*dx+dy*dy)); }
                    last = info.pt; break;
                case 0x20A: case 0x20E: Scroll?.Invoke((short)(info.mouseData >> 16)); break;
            }
        }
        return CallNextHookEx(mouse, code, message, data);
    }
    private nint Keyboard(int code, nint message, nint data)
    {
        if (code >= 0)
        {
            // Transient key state suppresses autorepeat; identities never leave this callback or reach storage.
            int vk = Marshal.ReadInt32(data);
            if ((int)message is 0x100 or 0x104) { if(down.Add(vk)) Key?.Invoke(); }
            else if ((int)message is 0x101 or 0x105) down.Remove(vk);
        }
        return CallNextHookEx(keyboard, code, message, data);
    }
    public void Dispose() { if(mouse!=0) UnhookWindowsHookEx(mouse); if(keyboard!=0) UnhookWindowsHookEx(keyboard); mouse=keyboard=0; down.Clear(); }
    private delegate nint HookProc(int code,nint message,nint data);
    [StructLayout(LayoutKind.Sequential)] private struct POINT { public int x,y; }
    [StructLayout(LayoutKind.Sequential)] private struct MouseInfo { public POINT pt; public uint mouseData,flags,time; public nuint extra; }
    [DllImport("user32.dll",SetLastError=true)] private static extern nint SetWindowsHookEx(int id,HookProc proc,nint module,uint thread);
    [DllImport("user32.dll")] private static extern bool UnhookWindowsHookEx(nint hook);
    [DllImport("user32.dll")] private static extern nint CallNextHookEx(nint hook,int code,nint message,nint data);
    [DllImport("kernel32.dll",CharSet=CharSet.Unicode)] private static extern nint GetModuleHandle(string? module);
}


