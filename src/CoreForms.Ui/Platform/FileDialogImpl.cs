using System.Runtime.InteropServices;
using CoreForms.Ui.Core;

namespace CoreForms.Ui.Platform;

/// <summary>
/// Dispatches file dialog calls to the appropriate platform implementation
/// (Windows COM IFileDialog or Linux XDG Desktop Portal via D-Bus).
/// </summary>
internal static class FileDialogImpl
{
    /// <summary>
    /// Shows an Open File dialog using the native OS file picker.
    /// </summary>
    public static DialogResult ShowOpenFile(
        Form? owner,
        string title,
        string? initialDirectory,
        string fileName,
        string filter,
        int filterIndex,
        string defaultExt,
        bool addExtension,
        bool checkFileExists,
        bool checkPathExists,
        bool dereferenceLinks,
        bool multiSelect,
        out string[] selectedFiles)
    {
        selectedFiles = [];

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            nint parentHwnd = GetParentHwnd(owner);
            return Windows.FileDialogWindows.ShowOpenFile(
                parentHwnd, title, initialDirectory, fileName, filter, filterIndex,
                multiSelect, checkFileExists, dereferenceLinks,
                out selectedFiles);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            string? parentHandle = DBus.PortalFileChooser.GetParentHandleString(owner);
            return DBus.PortalFileChooser.ShowOpenFile(
                parentHandle, title, initialDirectory, fileName, filter, filterIndex,
                multiSelect, out selectedFiles);
        }
        else
        {
            throw new PlatformNotSupportedException(
                "File dialogs are only supported on Windows and Linux.");
        }
    }

    /// <summary>
    /// Shows a Save File dialog using the native OS file picker.
    /// </summary>
    public static DialogResult ShowSaveFile(
        Form? owner,
        string title,
        string? initialDirectory,
        string fileName,
        string filter,
        int filterIndex,
        string defaultExt,
        bool addExtension,
        bool overwritePrompt,
        bool createPrompt,
        out string selectedFile)
    {
        selectedFile = string.Empty;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            nint parentHwnd = GetParentHwnd(owner);
            return Windows.FileDialogWindows.ShowSaveFile(
                parentHwnd, title, initialDirectory, fileName, filter, filterIndex,
                defaultExt, overwritePrompt, createPrompt,
                out selectedFile);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            string? parentHandle = DBus.PortalFileChooser.GetParentHandleString(owner);
            var result = DBus.PortalFileChooser.ShowSaveFile(
                parentHandle, title, initialDirectory, fileName, filter, filterIndex,
                createPrompt, overwritePrompt,
                out string[] selectedFiles);
            if (selectedFiles.Length > 0)
                selectedFile = selectedFiles[0];
            return result;
        }
        else
        {
            throw new PlatformNotSupportedException(
                "File dialogs are only supported on Windows and Linux.");
        }
    }

    private static nint GetParentHwnd(Form? owner)
    {
        if (owner == null)
            return nint.Zero;
        return Platform.GetNativeWindowHandle(owner);
    }
}
