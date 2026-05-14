using System.Diagnostics;

var url = args.Length > 0 ? args[0] : "about:blank";

// Find and launch the native helper
var binaryName = "WebBrowserHelper";
var helperPath = Path.Combine(AppContext.BaseDirectory, binaryName);

if (!File.Exists(helperPath))
{
    Console.Error.WriteLine($"Native helper not found: {helperPath}");
    return 1;
}

Process.Start(new ProcessStartInfo
{
    FileName = helperPath,
    Arguments = url,
    UseShellExecute = false
});

return 0;
