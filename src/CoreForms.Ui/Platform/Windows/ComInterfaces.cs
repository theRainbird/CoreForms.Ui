#pragma warning disable CA1416 // COM APIs are Windows-only, guarded by RuntimeInformation check
using System.Runtime.InteropServices;

namespace CoreForms.Ui.Platform.Windows;

internal static class CLSID
{
    public static readonly Guid FileOpenDialog = new("DC1C5A9C-E88A-4dde-A5A1-60F82A20AEF7");
    public static readonly Guid FileSaveDialog = new("C0B4E2F3-0A21-4aa1-8C20-0A5E3C28C3E8");
}

[Flags]
internal enum FILEOPENDIALOGOPTIONS : uint
{
    FOS_OVERWRITEPROMPT = 0x00000002,
    FOS_STRICTFILETYPES = 0x00000004,
    FOS_NOCHANGEDIR = 0x00000008,
    FOS_PICKFOLDERS = 0x00000020,
    FOS_FORCEFILESYSTEM = 0x00000040,
    FOS_ALLNONSTORAGEITEMS = 0x00000080,
    FOS_NOVALIDATE = 0x00000100,
    FOS_ALLOWMULTISELECT = 0x00000200,
    FOS_PATHMUSTEXIST = 0x00000800,
    FOS_FILEMUSTEXIST = 0x00001000,
    FOS_CREATEPROMPT = 0x00002000,
    FOS_SHAREAWARE = 0x00004000,
    FOS_NOREADONLYRETURN = 0x00008000,
    FOS_NOTESTFILECREATE = 0x00010000,
    FOS_HIDEMRUENTRIES = 0x00020000,
    FOS_HIDEPINNEDPLACES = 0x00040000,
    FOS_NODEREFERENCELINKS = 0x00100000,
    FOS_DONTADDTORECENT = 0x02000000,
    FOS_FORCESHOWHIDDEN = 0x10000000,
    FOS_DEFAULTNOMINIMODE = 0x20000000,
    FOS_FORCEPREVIEWPANEON = 0x40000000,
    FOS_SUPPORTSTREAMABLEITEMS = 0x80000000,
}

[ComImport]
[Guid("42f85136-db7e-49c4-9bac-3a4a2e2e27b0")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IFileDialog
{
    void Show(nint parentHwnd);
    void SetFileTypes(uint cFileTypes, [In] COMDLG_FILTERSPEC[] rgFilterSpec);
    void SetFileTypeIndex(uint iFileType);
    void GetFileTypeIndex(out uint piFileType);
    void Advise();
    void Unadvise();
    void SetOptions(FILEOPENDIALOGOPTIONS fos);
    void GetOptions(out FILEOPENDIALOGOPTIONS pfos);
    void SetDefaultFolder(IShellItem psi);
    void SetFolder(IShellItem psi);
    void GetFolder(out IShellItem ppsi);
    void GetCurrentSelection(out IShellItem ppsi);
    void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string pszName);
    void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);
    void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string pszTitle);
    void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string pszText);
    void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string pszLabel);
    void GetResult(out IShellItem ppsi);
    void AddPlace(IShellItem psi, uint alignment);
    void SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);
    void Close(int hr);
    void SetClientGuid([In] ref Guid guid);
    void ClearClientData();
    void SetFilter(IntPtr pFilter);
}

[ComImport]
[Guid("d57c7288-d4ad-4768-be02-9d969532d960")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IFileOpenDialog
{
    void Show(nint parentHwnd);
    void SetFileTypes(uint cFileTypes, [In] COMDLG_FILTERSPEC[] rgFilterSpec);
    void SetFileTypeIndex(uint iFileType);
    void GetFileTypeIndex(out uint piFileType);
    void Advise();
    void Unadvise();
    void SetOptions(FILEOPENDIALOGOPTIONS fos);
    void GetOptions(out FILEOPENDIALOGOPTIONS pfos);
    void SetDefaultFolder(IShellItem psi);
    void SetFolder(IShellItem psi);
    void GetFolder(out IShellItem ppsi);
    void GetCurrentSelection(out IShellItem ppsi);
    void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string pszName);
    void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);
    void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string pszTitle);
    void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string pszText);
    void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string pszLabel);
    void GetResult(out IShellItem ppsi);
    void AddPlace(IShellItem psi, uint alignment);
    void SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);
    void Close(int hr);
    void SetClientGuid([In] ref Guid guid);
    void ClearClientData();
    void SetFilter(IntPtr pFilter);
    void GetResults(out IntPtr ppenum);
    void GetSelectedItems(out IntPtr ppsai);
}

[ComImport]
[Guid("84bccd23-5fde-4cdb-aea4-af64b83d78ab")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IFileSaveDialog
{
    void Show(nint parentHwnd);
    void SetFileTypes(uint cFileTypes, [In] COMDLG_FILTERSPEC[] rgFilterSpec);
    void SetFileTypeIndex(uint iFileType);
    void GetFileTypeIndex(out uint piFileType);
    void Advise();
    void Unadvise();
    void SetOptions(FILEOPENDIALOGOPTIONS fos);
    void GetOptions(out FILEOPENDIALOGOPTIONS pfos);
    void SetDefaultFolder(IShellItem psi);
    void SetFolder(IShellItem psi);
    void GetFolder(out IShellItem ppsi);
    void GetCurrentSelection(out IShellItem ppsi);
    void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string pszName);
    void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);
    void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string pszTitle);
    void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string pszText);
    void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string pszLabel);
    void GetResult(out IShellItem ppsi);
    void AddPlace(IShellItem psi, uint alignment);
    void SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);
    void Close(int hr);
    void SetClientGuid([In] ref Guid guid);
    void ClearClientData();
    void SetFilter(IntPtr pFilter);
    void SetSaveAsItem(IShellItem psi);
    void SetProperties();
    void SetCollectedProperties();
    void GetProperties();
    void ApplyProperties();
}

[ComImport]
[Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IShellItem
{
    void BindToHandler(IntPtr pbc, [In] ref Guid bhid, [In] ref Guid riid, out IntPtr ppv);
    void GetParent(out IShellItem ppsi);
    void GetDisplayName([MarshalAs(UnmanagedType.U4)] SIGDN sigdnName, [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);
    void GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);
    void Compare(IShellItem psi, uint hint, out int piOrder);
}

internal enum SIGDN : uint
{
    NORMALDISPLAY = 0,
    FILESYSPATH = 0x80058000,
    URL = 0x80068000,
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal struct COMDLG_FILTERSPEC
{
    [MarshalAs(UnmanagedType.LPWStr)]
    public string pszName;

    [MarshalAs(UnmanagedType.LPWStr)]
    public string pszSpec;
}

internal static class Shell32
{
    private const string Shell32Dll = "shell32.dll";

    public static T CreateFileDialog<T>(Guid clsid) where T : class
    {
        var type = Type.GetTypeFromCLSID(clsid);
        if (type == null)
            throw new InvalidOperationException($"Failed to get type from CLSID {clsid}");
        var instance = Activator.CreateInstance(type);
        if (instance == null)
            throw new InvalidOperationException($"Failed to create instance of {clsid}");
        return (T)instance;
    }

    [DllImport(Shell32Dll, CharSet = CharSet.Unicode, PreserveSig = false)]
    public static extern int SHCreateItemFromParsingName(
        [MarshalAs(UnmanagedType.LPWStr)] string pszPath,
        IntPtr pbc,
        [In] ref Guid riid,
        [MarshalAs(UnmanagedType.Interface)] out IShellItem ppv);
}
