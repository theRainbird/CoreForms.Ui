#pragma warning disable CA1416 // Windows-only, guarded by RuntimeInformation check
using System.Runtime.InteropServices;
using System.Text;
using OldSchoolForms.Ui.Core;

namespace OldSchoolForms.Ui.Platform.Windows;

/// <summary>
/// Windows implementation of file dialogs using the Win32 GetOpenFileName/GetSaveFileName API.
/// </summary>
internal static class FileDialogWindows
{
    [Flags]
    private enum OFN_FLAGS : uint
    {
        OFN_READONLY = 0x00000001,
        OFN_OVERWRITEPROMPT = 0x00000002,
        OFN_HIDEREADONLY = 0x00000004,
        OFN_NOCHANGEDIR = 0x00000008,
        OFN_ALLOWMULTISELECT = 0x00000200,
        OFN_PATHMUSTEXIST = 0x00000800,
        OFN_FILEMUSTEXIST = 0x00001000,
        OFN_CREATEPROMPT = 0x00002000,
        OFN_NOREADONLYRETURN = 0x00008000,
        OFN_NODEREFERENCELINKS = 0x00100000,
        OFN_EXPLORER = 0x00080000,
        OFN_DONTADDTORECENT = 0x02000000,
        OFN_FORCESHOWHIDDEN = 0x10000000,
        OFN_ENABLESIZING = 0x00800000,
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct OPENFILENAME
    {
        public uint lStructSize;
        public IntPtr hwndOwner;
        public IntPtr hInstance;
        public IntPtr lpstrFilter;
        public IntPtr lpstrCustomFilter;
        public uint nMaxCustFilter;
        public uint nFilterIndex;
        public IntPtr lpstrFile;
        public uint nMaxFile;
        public IntPtr lpstrFileTitle;
        public uint nMaxFileTitle;
        public IntPtr lpstrInitialDir;
        public IntPtr lpstrTitle;
        public OFN_FLAGS Flags;
        public ushort nFileOffset;
        public ushort nFileExtension;
        public IntPtr lpstrDefExt;
        public IntPtr lCustData;
        public IntPtr lpfnHook;
        public IntPtr lpTemplateName;
        public IntPtr pvReserved;
        public uint dwReserved;
        public uint FlagsEx;
    }

    [DllImport("comdlg32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool GetOpenFileName(ref OPENFILENAME ofn);

    [DllImport("comdlg32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool GetSaveFileName(ref OPENFILENAME ofn);

    private const uint CDERR_DIALOGFAILURE = 0xFFFF;
    private const int FileBufferSize = 65536;

    public static DialogResult ShowOpenFile(
        nint parentHwnd, string title,
        string? initialDirectory, string fileName,
        string filter, int filterIndex,
        bool multiSelect, bool checkFileExists,
        bool dereferenceLinks,
        out string[] selectedFiles)
    {
        selectedFiles = [];

        IntPtr filterPtr = IntPtr.Zero;
        IntPtr fileBuffer = IntPtr.Zero;
        IntPtr titlePtr = IntPtr.Zero;
        IntPtr initialDirPtr = IntPtr.Zero;
        IntPtr defExtPtr = IntPtr.Zero;

        try
        {
            var ofn = new OPENFILENAME();
            ofn.lStructSize = (uint)Marshal.SizeOf<OPENFILENAME>();
            ofn.hwndOwner = parentHwnd;

            var flags = OFN_FLAGS.OFN_EXPLORER | OFN_FLAGS.OFN_HIDEREADONLY
                      | OFN_FLAGS.OFN_NOREADONLYRETURN | OFN_FLAGS.OFN_ENABLESIZING;
            if (multiSelect) flags |= OFN_FLAGS.OFN_ALLOWMULTISELECT;
            if (checkFileExists) flags |= OFN_FLAGS.OFN_FILEMUSTEXIST | OFN_FLAGS.OFN_PATHMUSTEXIST;
            if (!dereferenceLinks) flags |= OFN_FLAGS.OFN_NODEREFERENCELINKS;

            if (!string.IsNullOrEmpty(filter))
            {
                filterPtr = Marshal.StringToHGlobalUni(ConvertFilter(filter));
                ofn.lpstrFilter = filterPtr;
                ofn.nFilterIndex = (uint)Math.Max(1, filterIndex);
            }

            if (!string.IsNullOrEmpty(initialDirectory))
            {
                initialDirPtr = Marshal.StringToHGlobalUni(initialDirectory);
                ofn.lpstrInitialDir = initialDirPtr;
            }

            fileBuffer = Marshal.AllocHGlobal(FileBufferSize * 2);
            Marshal.Copy(Encoding.Unicode.GetBytes(fileName + "\0"), 0, fileBuffer, (fileName.Length + 1) * 2);
            ofn.lpstrFile = fileBuffer;
            ofn.nMaxFile = FileBufferSize;

            if (!string.IsNullOrEmpty(title))
            {
                titlePtr = Marshal.StringToHGlobalUni(title);
                ofn.lpstrTitle = titlePtr;
            }

            ofn.Flags = flags;

            if (!GetOpenFileName(ref ofn))
                return DialogResult.Cancel;

            if (!multiSelect)
            {
                string path = Marshal.PtrToStringUni(fileBuffer) ?? string.Empty;
                selectedFiles = !string.IsNullOrEmpty(path) ? [path] : [];
            }
            else
            {
                selectedFiles = ParseMultiSelect(fileBuffer);
            }

            return selectedFiles.Length > 0 ? DialogResult.OK : DialogResult.Cancel;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FileDialogWindows] OpenFile error: {ex.Message}");
            return DialogResult.Cancel;
        }
        finally
        {
            if (filterPtr != IntPtr.Zero) Marshal.FreeHGlobal(filterPtr);
            if (fileBuffer != IntPtr.Zero) Marshal.FreeHGlobal(fileBuffer);
            if (titlePtr != IntPtr.Zero) Marshal.FreeHGlobal(titlePtr);
            if (initialDirPtr != IntPtr.Zero) Marshal.FreeHGlobal(initialDirPtr);
            if (defExtPtr != IntPtr.Zero) Marshal.FreeHGlobal(defExtPtr);
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

        IntPtr filterPtr = IntPtr.Zero;
        IntPtr fileBuffer = IntPtr.Zero;
        IntPtr titlePtr = IntPtr.Zero;
        IntPtr initialDirPtr = IntPtr.Zero;
        IntPtr defExtPtr = IntPtr.Zero;

        try
        {
            var ofn = new OPENFILENAME();
            ofn.lStructSize = (uint)Marshal.SizeOf<OPENFILENAME>();
            ofn.hwndOwner = parentHwnd;

            var flags = OFN_FLAGS.OFN_EXPLORER | OFN_FLAGS.OFN_HIDEREADONLY
                      | OFN_FLAGS.OFN_NOREADONLYRETURN | OFN_FLAGS.OFN_ENABLESIZING
                      | OFN_FLAGS.OFN_PATHMUSTEXIST;
            if (overwritePrompt) flags |= OFN_FLAGS.OFN_OVERWRITEPROMPT;
            if (createPrompt) flags |= OFN_FLAGS.OFN_CREATEPROMPT;

            if (!string.IsNullOrEmpty(filter))
            {
                filterPtr = Marshal.StringToHGlobalUni(ConvertFilter(filter));
                ofn.lpstrFilter = filterPtr;
                ofn.nFilterIndex = (uint)Math.Max(1, filterIndex);
            }

            if (!string.IsNullOrEmpty(initialDirectory))
            {
                initialDirPtr = Marshal.StringToHGlobalUni(initialDirectory);
                ofn.lpstrInitialDir = initialDirPtr;
            }

            fileBuffer = Marshal.AllocHGlobal(FileBufferSize * 2);
            Marshal.Copy(Encoding.Unicode.GetBytes(fileName + "\0"), 0, fileBuffer, (fileName.Length + 1) * 2);
            ofn.lpstrFile = fileBuffer;
            ofn.nMaxFile = FileBufferSize;

            if (!string.IsNullOrEmpty(title))
            {
                titlePtr = Marshal.StringToHGlobalUni(title);
                ofn.lpstrTitle = titlePtr;
            }

            if (!string.IsNullOrEmpty(defaultExt))
            {
                defExtPtr = Marshal.StringToHGlobalUni(defaultExt);
                ofn.lpstrDefExt = defExtPtr;
            }

            ofn.Flags = flags;

            if (!GetSaveFileName(ref ofn))
                return DialogResult.Cancel;

            selectedFile = Marshal.PtrToStringUni(fileBuffer) ?? string.Empty;
            return !string.IsNullOrEmpty(selectedFile) ? DialogResult.OK : DialogResult.Cancel;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FileDialogWindows] SaveFile error: {ex.Message}");
            return DialogResult.Cancel;
        }
        finally
        {
            if (filterPtr != IntPtr.Zero) Marshal.FreeHGlobal(filterPtr);
            if (fileBuffer != IntPtr.Zero) Marshal.FreeHGlobal(fileBuffer);
            if (titlePtr != IntPtr.Zero) Marshal.FreeHGlobal(titlePtr);
            if (initialDirPtr != IntPtr.Zero) Marshal.FreeHGlobal(initialDirPtr);
            if (defExtPtr != IntPtr.Zero) Marshal.FreeHGlobal(defExtPtr);
        }
    }

    /// <summary>
    /// Converts our "Description|*.ext1;*.ext2" filter format to the Win32
    /// double-null-terminated format: "Description\0*.ext1;*.ext2\0\0".
    /// </summary>
    private static string ConvertFilter(string filter)
    {
        if (string.IsNullOrEmpty(filter))
            return "\0\0";

        string[] parts = filter.Split('|');
        var sb = new StringBuilder();

        for (int i = 0; i + 1 < parts.Length; i += 2)
        {
            sb.Append(parts[i]);
            sb.Append('\0');
            sb.Append(parts[i + 1]);
            sb.Append('\0');
        }

        // If odd number of parts, add unnamed filter for the last part
        if (parts.Length % 2 == 1)
        {
            sb.Append('\0');
            sb.Append(parts[^1]);
            sb.Append('\0');
        }

        sb.Append('\0'); // Final null terminator
        return sb.ToString();
    }

    /// <summary>
    /// Parses the multi-select file buffer format.
    /// Directory is the first string, followed by file names.
    /// If single file selected, the buffer just contains the file path.
    /// </summary>
    private static string[] ParseMultiSelect(IntPtr buffer)
    {
        // Read the first string - could be a directory or the full path
        string first = Marshal.PtrToStringUni(buffer) ?? string.Empty;
        if (string.IsNullOrEmpty(first))
            return [];

        // Read the second string - if it's empty, it's a single file selection
        int offset = (first.Length + 1) * 2;
        string second = Marshal.PtrToStringUni(buffer + offset) ?? string.Empty;

        if (string.IsNullOrEmpty(second))
        {
            // Single file selected, first is the full path
            return [first];
        }

        // Multi-file selected: first is the directory, followed by file names
        var files = new List<string>();
        string directory = first;

        while (!string.IsNullOrEmpty(second))
        {
            files.Add(Path.Combine(directory, second));
            offset += (second.Length + 1) * 2;
            second = Marshal.PtrToStringUni(buffer + offset) ?? string.Empty;
        }

        return files.ToArray();
    }
}
