using System;
using System.IO;
using System.Runtime.InteropServices;

namespace PureSFTP.Services;

public static class MacApplicationIconService
{
    private const string ObjCLibrary = "/usr/lib/libobjc.A.dylib";

    public static void ApplyDockIcon()
    {
        if (!OperatingSystem.IsMacOS())
        {
            return;
        }

        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "puresftp.icns");
        if (!File.Exists(iconPath))
        {
            return;
        }

        SetApplicationIcon(iconPath);
    }

    private static void SetApplicationIcon(string iconPath)
    {
        var nsApplicationClass = objc_getClass("NSApplication");
        var sharedApplication = objc_msgSend_IntPtr(nsApplicationClass, sel_registerName("sharedApplication"));
        if (sharedApplication == IntPtr.Zero)
        {
            return;
        }

        var pathString = CreateNSString(iconPath);
        var nsImageClass = objc_getClass("NSImage");
        var image = objc_msgSend_IntPtr(
            objc_msgSend_IntPtr(nsImageClass, sel_registerName("alloc")),
            sel_registerName("initWithContentsOfFile:"),
            pathString);
        objc_msgSend_Void(pathString, sel_registerName("release"));

        if (image != IntPtr.Zero)
        {
            objc_msgSend_Void_IntPtr(sharedApplication, sel_registerName("setApplicationIconImage:"), image);
            objc_msgSend_Void(image, sel_registerName("release"));
        }
    }

    private static IntPtr CreateNSString(string value)
    {
        var utf8 = Marshal.StringToCoTaskMemUTF8(value);
        try
        {
            var nsStringClass = objc_getClass("NSString");
            return objc_msgSend_IntPtr(
                objc_msgSend_IntPtr(nsStringClass, sel_registerName("alloc")),
                sel_registerName("initWithUTF8String:"),
                utf8);
        }
        finally
        {
            Marshal.FreeCoTaskMem(utf8);
        }
    }

    [DllImport(ObjCLibrary)]
    private static extern IntPtr objc_getClass(string name);

    [DllImport(ObjCLibrary)]
    private static extern IntPtr sel_registerName(string name);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
    private static extern IntPtr objc_msgSend_IntPtr(IntPtr receiver, IntPtr selector);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
    private static extern IntPtr objc_msgSend_IntPtr(IntPtr receiver, IntPtr selector, IntPtr arg1);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
    private static extern void objc_msgSend_Void(IntPtr receiver, IntPtr selector);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
    private static extern void objc_msgSend_Void_IntPtr(IntPtr receiver, IntPtr selector, IntPtr arg1);
}
