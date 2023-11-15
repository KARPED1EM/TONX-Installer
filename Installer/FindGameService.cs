using Microsoft.Win32;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Installer;

internal class FindGameService
{
    public static IReadOnlyList<string> FoundGamePaths => _FoundGamePaths.ToList();
    private static List<string> _FoundGamePaths = new();

    protected static List<(RegistryKey, string)> registryKeysToSearch => new()
    {
        // Config of TONX
        (Registry.CurrentUser, @"Software\AU-TONX\"),

        // Install path of AmongUs
        (Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\FeatureUsage\AppSwitched\"),
        (Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\FeatureUsage\ShowJumpView\"),
        (Registry.CurrentUser, @"Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Compatibility Assistant\Store\"),
        (Registry.CurrentUser, @"Software\Classes\Local Settings\Software\Microsoft\Windows\Shell\MuiCache\"),

        // Install path of Steam
        (Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Valve\Steam"),
        (Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Steam"),
    };

    public static void SearchAllByRegistry()
    {
        foreach (var kvp in registryKeysToSearch)
        {
            var (valid, path) = FindValidPathInKey(kvp.Item1, kvp.Item2);
            if (valid) _FoundGamePaths.Add(path);
        }
    }
    private static (bool, string) FindValidPathInKey(RegistryKey registerRoot, string inputKey)
    {
        var notFound = (false, string.Empty);
        var keyTree = registerRoot.OpenSubKey(inputKey);
        if (keyTree == null) return notFound;
        var keys = keyTree.GetValueNames().Where(k => k.Contains("Among Us.exe")).ToList();
        if (keys == null || keys.Count < 1) return notFound;

        foreach (var key in keys)
        {
            string path = key;

            // Remove .FriendlyAppName suffix when searching in MuiCache
            if (path.EndsWith(".FriendlyAppName"))
                path = path.Remove(path.IndexOf(".FriendlyAppName"));
            // Get superior directory when key is point to a executable file
            if (path.EndsWith(".exe"))
                path = Path.GetDirectoryName(path);
            // Point to AmongUs folder when key is Steam Folder
            if (path.EndsWith("Steam"))
                path += @"\steamapps\common\Among Us\";
            else if (path.EndsWith("Steam\\"))
                path += @"steamapps\common\Among Us\";

            path = TrimGamePath(path);
            if (!IsValidAmongUsFolder(path)) continue;
            if (_FoundGamePaths.Contains(path)) continue;
            return (true, path);
        }

        return notFound;
    }
    public static bool IsValidAmongUsFolder(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;
        path = TrimGamePath(path);
        return !(
            !Directory.Exists(path)
            || !Directory.Exists(path + "/Among Us_Data")
            || !File.Exists(path + "/Among Us.exe")
            || !File.Exists(path + "/GameAssembly.dll")
            || !File.Exists(path + "/UnityCrashHandler32.exe")
            );
    }
    private static string TrimGamePath(string path)
    {
        if (path.EndsWith("Among Us.exe")) path = Path.GetDirectoryName(path) ?? "";
        path = path.Replace("\\", "/").TrimEnd('/');
        return path;
    }
}
