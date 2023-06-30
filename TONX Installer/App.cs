using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Windows;

namespace TONX_Installer;

class App
{
    static readonly Dictionary<string, Version> EachVersionMD5 = new()
    {
        { "254321c6fdf0b1de79aff77fa6ad825e", new(2023, 6, 13) },
        { "908fe1e7366c7e7d44d385c0e47fad50", new(2023, 3, 28) },
    };
    static readonly List<string> GameFilesWhiteList = new()
    {
        //EPIC
        ".egstore",
        //ALL
        "Among Us_Data",
        "Among Us.exe",
        "baselib.dll",
        "GameAssembly.dll",
        "msvcp140.dll",
        "UnityCrashHandler32.exe",
        "UnityPlayer.dll",
        "vcruntime140.dll",
    };

    static List<string> ExclusionList = new();
    static string Game_Path = "";

    [STAThread]
    static void Main()
    {

    StartAutoFind:

        Game_Path = "";

        if (FindPath_ModRegister(ref Game_Path) && IsValidAmongUsFolder(Game_Path)) goto ConfirmPath;
        if (FindPath_SteamRegister(ref Game_Path) && IsValidAmongUsFolder(Game_Path)) goto ConfirmPath;
        if (FindPath_SteamUninstallRegister(ref Game_Path) && IsValidAmongUsFolder(Game_Path)) goto ConfirmPath;

        if (FindPath_AppSwitchedRegister(ref Game_Path)) goto ConfirmPath;
        if (FindPath_ShowJumpViewRegister(ref Game_Path)) goto ConfirmPath;
        if (FindPath_StoreRegister(ref Game_Path)) goto ConfirmPath;
        if (FindPath_MuiCacheRegister(ref Game_Path)) goto ConfirmPath;

    StartManualSelect:

        FolderPicker dialog = new();
        dialog.Title = "请选择 AmongUs 游戏目录：";
        dialog.OkButtonLabel = "选择文件夹";
        dialog.Multiselect = false;

        if (!dialog.ShowDialog() ?? false) Application.Current.Shutdown();

        if (!IsValidAmongUsFolder(dialog.ResultPath, true))
        {
            var dialogResponse = MessageBox.Show("您选择的不是有效的 AmongUs 路径\n请问要重新选择吗？", "错误：", MessageBoxButton.YesNo, MessageBoxImage.Error, MessageBoxResult.Yes);
            if (dialogResponse == MessageBoxResult.Yes) goto StartManualSelect;
            else Application.Current.Shutdown();
        }
        else
        {
            Game_Path = dialog.ResultPath;
            goto PathFound;
        }

    ConfirmPath:

        Game_Path = TrimGameFolder(Game_Path);

        var msgResponse = MessageBox.Show($"找到 AmongUs 目录：\n{Game_Path}\n请问这是正确的游戏路径吗？", "询问：", MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.Yes);
        switch (msgResponse)
        {
            case MessageBoxResult.No:
                ExclusionList.Add(Game_Path.Replace("\\", "/"));
                goto StartAutoFind;
            case MessageBoxResult.Cancel:
                Application.Current.Shutdown();
                break;
        }

    PathFound:

        string gameMd5 = GetMD5HashFromFile(Game_Path + "Among Us.exe");
        if (!EachVersionMD5.TryGetValue(gameMd5, out var gameVer) || gameVer == null)
        {
            MessageBox.Show($"安装失败，您的 AmongUs 版本不受支持", "错误：", MessageBoxButton.OK, MessageBoxImage.Error);
            Application.Current.Shutdown();
        }

        string plunginsPath = Game_Path + "BepInEx/plugins/";
        if (Directory.Exists(plunginsPath) && Directory.GetFiles(plunginsPath).Length > 0)
        {
            var overrideResponse = MessageBox.Show("检测到您已安装其他模组或插件\n安装TONX会将其覆盖，请问要继续吗？", "警告：", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.Yes);
            if (overrideResponse == MessageBoxResult.No) Application.Current.Shutdown();
        }

        try
        {
            RestoreVanillaGame();

            var resNames = Assembly.GetExecutingAssembly().GetManifestResourceNames();
            int allCount = resNames.Length;
            int succeedCount = 0;
            bool correctVersionFound = false;
            List<Version> supportVersionList = new();

            foreach (var resName in resNames)
            {
                var stream = Assembly.GetEntryAssembly()?.GetManifestResourceStream(resName);
                if (stream == null) continue;
                string fileName = resName.Replace($"TONX_Installer.Resource.", string.Empty);
                fileName = Regex.Replace(fileName, @"TONX.*dll", "TONX.dll");

                if (resName.EndsWith(".zip"))
                {
                    var path = Path.GetTempFileName();
                    WriteToFile(resName, path);

                    ZipFile.ExtractToDirectory(path, Game_Path);
                    File.Delete(path);

                    succeedCount++;
                }
                else
                {
                    if (!Directory.Exists(plunginsPath))
                        Directory.CreateDirectory(plunginsPath);

                    var fileVerInfo = GetFileVersionInfoFromStream(stream);
                    if (fileName.EndsWith("TONX.dll") && Version.TryParse(fileVerInfo?.ProductVersion, out var fileVer))
                    {
                        if (gameVer != fileVer)
                        {
                            supportVersionList.Add(fileVer);
                            allCount--;
                            continue;
                        }
                        else
                        {
                            correctVersionFound = true;
                        }
                    }

                    var path = plunginsPath + fileName;
                    WriteToFile(resName, path);

                    succeedCount++;
                }
            }

            static void WriteToFile(string resName, string path)
            {
                var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resName);
                if (stream == null) return;
                BufferedStream input = new(stream);
                var output = File.OpenWrite(path);
                byte[] data = new byte[1024];
                int lengthEachRead;
                while ((lengthEachRead = input.Read(data, 0, data.Length)) > 0)
                    output.Write(data, 0, lengthEachRead);
                output.Flush();
                output.Close();
            }

            if (correctVersionFound)
            {
                string failedMsg = succeedCount < allCount ? $"，失败{allCount - succeedCount}个" : string.Empty;
                bool done = failedMsg == string.Empty;
                string doneMsg = "TONX已成功安装，现在您可以启动游戏了";
                if (!done) doneMsg = "TONX安装失败，请尝试其它安装方式";
                MessageBox.Show($"共{allCount}个文件，成功{succeedCount}个{failedMsg}\n{doneMsg}", done ? "恭喜：" : "错误：", MessageBoxButton.OK, done ? MessageBoxImage.Information : MessageBoxImage.Error);
            }
            else
            {
                string supportMsg = "TONX 支持的 AmongUs 版本：";
                foreach(var ver in supportVersionList) supportMsg += ver.ToString() + " 或 ";
                supportMsg = supportMsg.TrimEnd('或').Trim();
                string yourVerMsg = "您的 AmongUs 版本：" + gameVer?.ToString();
                MessageBox.Show($"安装失败，您的 AmongUs 版本不受支持\n{supportMsg}\n{yourVerMsg}", "错误：", MessageBoxButton.OK, MessageBoxImage.Error);
                RestoreVanillaGame();
            }
        }
        catch (UnauthorizedAccessException)
        {
            MessageBox.Show($"操作文件被拒绝或没有权限\n请确保游戏关闭后以管理员权限重试", "错误：", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        Application.Current.Shutdown();
    }
    static bool RestoreVanillaGame()
    {
        try
        {
            foreach (var file in Directory.GetFiles(Game_Path).Where(f => !GameFilesWhiteList.Any(w => f.EndsWith(w))))
            {
                File.Delete(file);
            }
            foreach (var folder in Directory.GetDirectories(Game_Path).Where(f => !GameFilesWhiteList.Any(w => f.EndsWith(w))))
            {
                Directory.Delete(folder, true);
            }
            return true;
        }
        catch
        {
            throw;
        }
    }
    static FileVersionInfo GetFileVersionInfoFromStream(Stream stream)
    {
        string path = Path.GetTempFileName();
        var fs = File.Create(path);
        stream.CopyTo(fs);
        fs.Close();
        var ret = FileVersionInfo.GetVersionInfo(path);
        File.Delete(path);
        return ret;
    }
    static string GetMD5HashFromFile(string fileName)
    {
        try
        {
            using var md5 = MD5.Create();
            using var stream = File.OpenRead(fileName);
            var hash = md5.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
        catch
        {
            return "";
        }
    }
    static bool FindPath_ModRegister(ref string result)
    {
        var keyTree = Registry.CurrentUser.OpenSubKey(@"Software\AU-TONX");
        if (keyTree == null) return false;
        if (keyTree.GetValue("Path") is not string ret) return false;
        result = ret;
        return true;
    }
    static bool FindPath_SteamRegister(ref string result)
    {
        var keyTree = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam");
        if (keyTree == null) return false;
        if (keyTree.GetValue("InstallPath") is not string steamPath) return false;
        string fullPath = steamPath + @"\steamapps\common\Among Us\";
        result = fullPath;
        return true;
    }
    static bool FindPath_SteamUninstallRegister(ref string result)
    {
        var keyTree = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Steam");
        if (keyTree == null) return false;
        if (keyTree.GetValue("UninstallString") is not string steamPath) return false;
        string fullPath = Path.GetDirectoryName(steamPath) + @"\steamapps\common\Among Us\";
        result = fullPath;
        return true;
    }
    static bool FindPath_MuiCacheRegister(ref string result)
    {
        var keyTree = Registry.CurrentUser.OpenSubKey(@"Software\Classes\Local Settings\Software\Microsoft\Windows\Shell\MuiCache\");
        if (keyTree == null) return false;
        var keys = keyTree.GetValueNames().Where(k => k.Contains("Among Us.exe")).ToList();
        if (keys == null || keys.Count < 1) return false;

        foreach (var key in keys)
        {
            string name = key.Replace(".FriendlyAppName", string.Empty).TrimEnd('.');
            if (!IsValidAmongUsFolder(Path.GetDirectoryName(name) ?? "")) continue;
            result = name;
            return true;
        }

        return false;
    }
    static bool FindPath_AppSwitchedRegister(ref string result)
        => Register_FindValidPathInKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\FeatureUsage\AppSwitched\", ref result);
    static bool FindPath_ShowJumpViewRegister(ref string result)
        => Register_FindValidPathInKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\FeatureUsage\ShowJumpView\", ref result);
    static bool FindPath_StoreRegister(ref string result)
        => Register_FindValidPathInKey(@"Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Compatibility Assistant\Store\", ref result);
    static bool Register_FindValidPathInKey(string input, ref string result)
    {
        var keyTree = Registry.CurrentUser.OpenSubKey(input);
        if (keyTree == null) return false;
        var keys = keyTree.GetValueNames().Where(k => k.Contains("Among Us.exe")).ToList();
        if (keys == null || keys.Count < 1) return false;

        foreach (var key in keys)
        {
            string name = key;
            if (!IsValidAmongUsFolder(Path.GetDirectoryName(name) ?? "")) continue;
            result = name;
            return true;
        }

        return false;
    }
    static bool IsValidAmongUsFolder(string path, bool ignoreExclusion = false)
    {
        path = TrimGameFolder(path);
        if (!ignoreExclusion && ExclusionList.Contains(path)) return false;
        return !(
            !Directory.Exists(path)
            || !Directory.Exists(path + "Among Us_Data")
            || !File.Exists(path + "Among Us.exe")
            || !File.Exists(path + "UnityCrashHandler32.exe")
            );
    }
    static string TrimGameFolder(string path)
    {
        if (path.EndsWith("Among Us.exe")) path = Path.GetDirectoryName(path) ?? "";
        path = path.Replace("\\", "/");
        if (!path.EndsWith('/')) path += "/";
        return path;
    }
}
