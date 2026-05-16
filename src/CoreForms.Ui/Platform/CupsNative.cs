using System.Runtime.InteropServices;

namespace CoreForms.Ui.Platform;

/// <summary>
/// Minimal CUPS (Common Unix Printing System) P/Invoke for printer enumeration.
/// </summary>
internal static class CupsNative
{
    private const string CupsLib = "libcups";

    [DllImport(CupsLib)]
    private static extern int cupsGetDests(out IntPtr dests);

    [DllImport(CupsLib)]
    private static extern void cupsFreeDests(int numDests, IntPtr dests);

    // cups_dest_t layout (approximate, depends on CUPS version):
    //   char* name      (offset 0, pointer)
    //   char* instance  (offset 8, pointer)
    //   int is_default  (offset 16)
    //   int num_options (offset 20)
    //   void* options   (offset 24, pointer)

    /// <summary>
    /// Enumerates all installed printers via CUPS.
    /// </summary>
    public static string[] EnumeratePrinters()
    {
        try
        {
            int count = cupsGetDests(out IntPtr dests);
            if (count <= 0 || dests == IntPtr.Zero)
                return [];

            var printers = new List<string>();
            IntPtr ptr = dests;

            // On 64-bit systems, cups_dest_t is 32 bytes
            int structSize = IntPtr.Size * 3 + sizeof(int) * 2;

            for (int i = 0; i < count; i++)
            {
                IntPtr namePtr = Marshal.ReadIntPtr(ptr);
                if (namePtr != IntPtr.Zero)
                {
                    string name = Marshal.PtrToStringUTF8(namePtr) ?? "";
                    if (!string.IsNullOrEmpty(name) && !printers.Contains(name))
                        printers.Add(name);
                }
                ptr += structSize;
            }

            cupsFreeDests(count, dests);
            return printers.ToArray();
        }
        catch
        {
            // Fallback: try lpstat
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "lpstat",
                    Arguments = "-e",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                };

                using var proc = System.Diagnostics.Process.Start(psi);
                if (proc == null)
                    return [];

                string output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit(5000);
                return output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            }
            catch
            {
                return [];
            }
        }
    }
}
