using System;
using System.Runtime.InteropServices;
using System.Text;
using PureSFTP.Models;

namespace PureSFTP.Services;

public sealed class SystemCredentialStore : ICredentialStore
{
    private const string ServiceName = "PureSftp";
    private const string SecurityFramework = "/System/Library/Frameworks/Security.framework/Security";
    private const string CoreFoundationFramework = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
    private const int NoErr = 0;
    private const int ErrSecItemNotFound = -25300;
    private const int CredTypeGeneric = 1;
    private const int CredPersistLocalMachine = 2;

    public string? ReadPassword(SavedConnection connection)
    {
        if (OperatingSystem.IsMacOS())
        {
            return ReadMacPassword(GetAccountName(connection));
        }

        if (OperatingSystem.IsWindows())
        {
            return ReadWindowsPassword(GetWindowsTargetName(connection));
        }

        return null;
    }

    public bool SavePassword(SavedConnection connection, string password)
    {
        if (OperatingSystem.IsMacOS())
        {
            return SaveMacPassword(GetAccountName(connection), password);
        }

        if (OperatingSystem.IsWindows())
        {
            return SaveWindowsPassword(GetWindowsTargetName(connection), connection.Username, password);
        }

        return false;
    }

    public void DeletePassword(SavedConnection connection)
    {
        if (OperatingSystem.IsMacOS())
        {
            DeleteMacPassword(GetAccountName(connection));
            return;
        }

        if (OperatingSystem.IsWindows())
        {
            CredDelete(GetWindowsTargetName(connection), CredTypeGeneric, 0);
        }
    }

    private static string GetAccountName(SavedConnection connection) => $"connection:{connection.Id}";

    private static string GetWindowsTargetName(SavedConnection connection) => $"{ServiceName}.Connection.{connection.Id}";

    private static string? ReadMacPassword(string accountName)
    {
        var serviceBytes = Encoding.UTF8.GetBytes(ServiceName);
        var accountBytes = Encoding.UTF8.GetBytes(accountName);
        var status = SecKeychainFindGenericPassword(
            IntPtr.Zero,
            (uint)serviceBytes.Length,
            serviceBytes,
            (uint)accountBytes.Length,
            accountBytes,
            out var passwordLength,
            out var passwordData,
            out var itemRef);

        if (status == ErrSecItemNotFound)
        {
            return null;
        }

        if (status != NoErr || passwordData == IntPtr.Zero)
        {
            return null;
        }

        try
        {
            var passwordBytes = new byte[(int)passwordLength];
            Marshal.Copy(passwordData, passwordBytes, 0, (int)passwordLength);
            return Encoding.UTF8.GetString(passwordBytes);
        }
        finally
        {
            SecKeychainItemFreeContent(IntPtr.Zero, passwordData);
            if (itemRef != IntPtr.Zero)
            {
                CFRelease(itemRef);
            }
        }
    }

    private static bool SaveMacPassword(string accountName, string password)
    {
        var serviceBytes = Encoding.UTF8.GetBytes(ServiceName);
        var accountBytes = Encoding.UTF8.GetBytes(accountName);
        var passwordBytes = Encoding.UTF8.GetBytes(password);

        var status = SecKeychainFindGenericPassword(
            IntPtr.Zero,
            (uint)serviceBytes.Length,
            serviceBytes,
            (uint)accountBytes.Length,
            accountBytes,
            out _,
            out var passwordData,
            out var itemRef);

        if (passwordData != IntPtr.Zero)
        {
            SecKeychainItemFreeContent(IntPtr.Zero, passwordData);
        }

        if (status == NoErr && itemRef != IntPtr.Zero)
        {
            try
            {
                return SecKeychainItemModifyAttributesAndData(itemRef, IntPtr.Zero, (uint)passwordBytes.Length, passwordBytes) == NoErr;
            }
            finally
            {
                CFRelease(itemRef);
            }
        }

        status = SecKeychainAddGenericPassword(
            IntPtr.Zero,
            (uint)serviceBytes.Length,
            serviceBytes,
            (uint)accountBytes.Length,
            accountBytes,
            (uint)passwordBytes.Length,
            passwordBytes,
            out itemRef);

        if (itemRef != IntPtr.Zero)
        {
            CFRelease(itemRef);
        }

        return status == NoErr;
    }

    private static void DeleteMacPassword(string accountName)
    {
        var serviceBytes = Encoding.UTF8.GetBytes(ServiceName);
        var accountBytes = Encoding.UTF8.GetBytes(accountName);
        var status = SecKeychainFindGenericPassword(
            IntPtr.Zero,
            (uint)serviceBytes.Length,
            serviceBytes,
            (uint)accountBytes.Length,
            accountBytes,
            out _,
            out var passwordData,
            out var itemRef);

        if (passwordData != IntPtr.Zero)
        {
            SecKeychainItemFreeContent(IntPtr.Zero, passwordData);
        }

        if (status == NoErr && itemRef != IntPtr.Zero)
        {
            SecKeychainItemDelete(itemRef);
            CFRelease(itemRef);
        }
    }

    private static string? ReadWindowsPassword(string targetName)
    {
        if (!CredRead(targetName, CredTypeGeneric, 0, out var credentialPointer))
        {
            return null;
        }

        try
        {
            var credential = Marshal.PtrToStructure<Credential>(credentialPointer);
            if (credential.CredentialBlob == IntPtr.Zero || credential.CredentialBlobSize == 0)
            {
                return string.Empty;
            }

            var passwordBytes = new byte[(int)credential.CredentialBlobSize];
            Marshal.Copy(credential.CredentialBlob, passwordBytes, 0, passwordBytes.Length);
            return Encoding.Unicode.GetString(passwordBytes);
        }
        finally
        {
            CredFree(credentialPointer);
        }
    }

    private static bool SaveWindowsPassword(string targetName, string username, string password)
    {
        var passwordBytes = Encoding.Unicode.GetBytes(password);
        var targetNamePointer = Marshal.StringToCoTaskMemUni(targetName);
        var usernamePointer = Marshal.StringToCoTaskMemUni(username);
        var passwordPointer = Marshal.AllocCoTaskMem(passwordBytes.Length);

        try
        {
            Marshal.Copy(passwordBytes, 0, passwordPointer, passwordBytes.Length);
            var credential = new Credential
            {
                Type = CredTypeGeneric,
                TargetName = targetNamePointer,
                CredentialBlob = passwordPointer,
                CredentialBlobSize = (uint)passwordBytes.Length,
                Persist = CredPersistLocalMachine,
                UserName = usernamePointer,
            };

            return CredWrite(ref credential, 0);
        }
        finally
        {
            Marshal.FreeCoTaskMem(targetNamePointer);
            Marshal.FreeCoTaskMem(usernamePointer);
            Marshal.FreeCoTaskMem(passwordPointer);
        }
    }

    [DllImport(SecurityFramework)]
    private static extern int SecKeychainAddGenericPassword(
        IntPtr keychain,
        uint serviceNameLength,
        byte[] serviceName,
        uint accountNameLength,
        byte[] accountName,
        uint passwordLength,
        byte[] passwordData,
        out IntPtr itemRef);

    [DllImport(SecurityFramework)]
    private static extern int SecKeychainFindGenericPassword(
        IntPtr keychain,
        uint serviceNameLength,
        byte[] serviceName,
        uint accountNameLength,
        byte[] accountName,
        out uint passwordLength,
        out IntPtr passwordData,
        out IntPtr itemRef);

    [DllImport(SecurityFramework)]
    private static extern int SecKeychainItemModifyAttributesAndData(IntPtr itemRef, IntPtr attrList, uint length, byte[] data);

    [DllImport(SecurityFramework)]
    private static extern int SecKeychainItemDelete(IntPtr itemRef);

    [DllImport(SecurityFramework)]
    private static extern int SecKeychainItemFreeContent(IntPtr attrList, IntPtr data);

    [DllImport(CoreFoundationFramework)]
    private static extern void CFRelease(IntPtr cf);

    [DllImport("Advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredRead(string target, int type, int reservedFlag, out IntPtr credential);

    [DllImport("Advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredWrite(ref Credential userCredential, uint flags);

    [DllImport("Advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredDelete(string target, int type, int flags);

    [DllImport("Advapi32.dll")]
    private static extern void CredFree(IntPtr credential);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct Credential
    {
        public uint Flags;
        public uint Type;
        public IntPtr TargetName;
        public IntPtr Comment;
        public long LastWritten;
        public uint CredentialBlobSize;
        public IntPtr CredentialBlob;
        public uint Persist;
        public uint AttributeCount;
        public IntPtr Attributes;
        public IntPtr TargetAlias;
        public IntPtr UserName;
    }
}
