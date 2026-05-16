#pragma warning disable CA1416 // COM APIs are Windows-only, guarded by RuntimeInformation check
using System.Runtime.InteropServices;
using CoreForms.Ui.Core;

namespace CoreForms.Ui.Platform.Windows;

/// <summary>
/// Windows implementation of file dialogs using the native COM IFileDialog API (Vista+).
/// </summary>
internal static class FileDialogWindows
{
    private static Guid GetShellItemGuid() => new("43826d1e-e718-42ee-bc55-a1e261c37bfe");

    public static DialogResult ShowOpenFile(
        nint parentHwnd, string title,
        string? initialDirectory, string fileName,
        string filter, int filterIndex,
        bool multiSelect, bool checkFileExists,
        bool dereferenceLinks,
        out string[] selectedFiles)
    {
        selectedFiles = [];

        try
        {
            var dialog = Shell32.CreateFileDialog<IFileOpenDialog>(CLSID.FileOpenDialog);

            var options = FILEOPENDIALOGOPTIONS.FOS_FORCEFILESYSTEM;
            if (multiSelect) options |= FILEOPENDIALOGOPTIONS.FOS_ALLOWMULTISELECT;
            if (checkFileExists) options |= FILEOPENDIALOGOPTIONS.FOS_FILEMUSTEXIST;
            if (!dereferenceLinks) options |= FILEOPENDIALOGOPTIONS.FOS_NODEREFERENCELINKS;
            dialog.SetOptions(options);

            if (!string.IsNullOrEmpty(title))
                dialog.SetTitle(title);

            var filterSpecs = ParseFilter(filter);
            if (filterSpecs.Length > 0)
            {
                dialog.SetFileTypes((uint)filterSpecs.Length, filterSpecs);
                if (filterIndex > 0 && filterIndex <= filterSpecs.Length)
                    dialog.SetFileTypeIndex((uint)filterIndex);
            }

            if (!string.IsNullOrEmpty(initialDirectory))
                SetFolder((IFileDialog)dialog, initialDirectory);

            if (!string.IsNullOrEmpty(fileName))
                dialog.SetFileName(fileName);

            dialog.Show(parentHwnd);

            if (multiSelect)
            {
                var results = new List<string>();
                dialog.GetResults(out IntPtr shellItemArrayPtr);
                if (shellItemArrayPtr != IntPtr.Zero)
                {
                    var shellItemArray = MarshalShellItemArray(shellItemArrayPtr);
                    if (shellItemArray != null)
                        results.AddRange(shellItemArray);
                }

                if (results.Count == 0)
                {
                    dialog.GetResult(out IShellItem item);
                    string? path = GetDisplayPath(item);
                    if (path != null)
                        results.Add(path);
                }

                selectedFiles = results.ToArray();
            }
            else
            {
                dialog.GetResult(out IShellItem item);
                string? path = GetDisplayPath(item);
                selectedFiles = path != null ? [path] : [];
            }

            return selectedFiles.Length > 0 ? DialogResult.OK : DialogResult.Cancel;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FileDialogWindows] OpenFile error: {ex.Message}");
            return DialogResult.Cancel;
        }
    }

    public static DialogResult ShowSaveFile(
        nint parentHwnd, string title,
        string? initialDirectory, string fileName,
        string filter, int filterIndex,
        string defaultExt, bool overwritePrompt,
        bool createPrompt,
        out string selectedFile)
    {
        selectedFile = string.Empty;

        try
        {
            var dialog = Shell32.CreateFileDialog<IFileSaveDialog>(CLSID.FileSaveDialog);

            var options = FILEOPENDIALOGOPTIONS.FOS_FORCEFILESYSTEM
                        | FILEOPENDIALOGOPTIONS.FOS_PATHMUSTEXIST;
            if (overwritePrompt) options |= FILEOPENDIALOGOPTIONS.FOS_OVERWRITEPROMPT;
            if (!string.IsNullOrEmpty(defaultExt))
                dialog.SetDefaultExtension(defaultExt);
            dialog.SetOptions(options);

            if (!string.IsNullOrEmpty(title))
                dialog.SetTitle(title);

            var filterSpecs = ParseFilter(filter);
            if (filterSpecs.Length > 0)
            {
                dialog.SetFileTypes((uint)filterSpecs.Length, filterSpecs);
                if (filterIndex > 0 && filterIndex <= filterSpecs.Length)
                    dialog.SetFileTypeIndex((uint)filterIndex);
            }

            if (!string.IsNullOrEmpty(initialDirectory))
                SetFolder((IFileDialog)dialog, initialDirectory);

            if (!string.IsNullOrEmpty(fileName))
                dialog.SetFileName(fileName);

            dialog.Show(parentHwnd);

            dialog.GetResult(out IShellItem item);
            string? path = GetDisplayPath(item);
            selectedFile = path ?? string.Empty;

            return !string.IsNullOrEmpty(selectedFile) ? DialogResult.OK : DialogResult.Cancel;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FileDialogWindows] SaveFile error: {ex.Message}");
            return DialogResult.Cancel;
        }
    }

    private static COMDLG_FILTERSPEC[] ParseFilter(string filter)
    {
        if (string.IsNullOrEmpty(filter))
            return [];

        string[] parts = filter.Split('|');
        var result = new List<COMDLG_FILTERSPEC>();

        for (int i = 0; i + 1 < parts.Length; i += 2)
        {
            result.Add(new COMDLG_FILTERSPEC
            {
                pszName = parts[i],
                pszSpec = parts[i + 1]
            });
        }

        return result.ToArray();
    }

    private static string? GetDisplayPath(IShellItem item)
    {
        try
        {
            item.GetDisplayName(SIGDN.FILESYSPATH, out string path);
            return path;
        }
        catch { return null; }
    }

    private static void SetFolder(IFileDialog dialog, string path)
    {
        try
        {
            var shellItemGuid = GetShellItemGuid();
            Shell32.SHCreateItemFromParsingName(path, IntPtr.Zero, ref shellItemGuid, out IShellItem folder);
            dialog.SetFolder(folder);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FileDialogWindows] SetFolder error: {ex.Message}");
        }
    }

    private static List<string>? MarshalShellItemArray(IntPtr ptr)
    {
        try
        {
            var shellItemArray = (IShellItemArray?)Marshal.GetObjectForIUnknown(ptr);
            if (shellItemArray == null) return null;

            shellItemArray.GetCount(out uint count);
            var results = new List<string>();
            for (uint i = 0; i < count; i++)
            {
                shellItemArray.GetItemAt(i, out IShellItem item);
                string? path = GetDisplayPath(item);
                if (path != null) results.Add(path);
            }
            return results;
        }
        catch { return null; }
    }
}

[ComImport]
[Guid("b63ea76d-1f85-456f-a19c-48159efa858b")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IShellItemArray
{
    void BindToHandler(IntPtr pbc, [In] ref Guid bhid, [In] ref Guid riid, out IntPtr ppv);
    void GetPropertyStore(uint flags, [In] ref Guid riid, out IntPtr ppv);
    void GetPropertyDescriptionList(IntPtr keyType, [In] ref Guid riid, out IntPtr ppv);
    void GetAttributes(uint attribFlags, uint sfgaoMask, out uint psfgaoAttribs);
    void GetCount(out uint pdwNumItems);
    void GetItemAt(uint dwIndex, [MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);
    void EnumItems(out IntPtr ppenumShellItems);
}
