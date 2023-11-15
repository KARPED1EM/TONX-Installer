using CommunityToolkit.Mvvm.ComponentModel;
using Installer.Localization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Installer.Pages;

[INotifyPropertyChanged]
public sealed partial class DownloadPage : Page, IPage
{
    public static DownloadPage Current { get; private set; }

    [ObservableProperty]
    private string bepInExDownloadProgressText = Lang.Install_BepInEx_Pending;
    [ObservableProperty]
    private double bepInExDownloadProgress = 0;
    [ObservableProperty]
    private string pluginDownloadProgressText = Lang.Install_BepInEx_Pending;
    [ObservableProperty]
    private double pluginDownloadProgress = 0;

#nullable enable
    private void UpdateBepInExProgress(string? text, double? value)
    {
        Current.DispatcherQueue.TryEnqueue(() =>
        {
            if (value.HasValue) BepInExDownloadProgress = value.Value;
            if (!string.IsNullOrEmpty(text)) BepInExDownloadProgressText = text;
        });
    }
    private void UpdatePluginProgress(string? text, double? value)
    {
        Current.DispatcherQueue.TryEnqueue(() =>
        {
            if (value.HasValue) PluginDownloadProgress = value.Value;
            if (!string.IsNullOrEmpty(text)) PluginDownloadProgressText = text;
        });
    }
#nullable disable

    public DownloadPage()
    {
        Current = this;

        this.InitializeComponent();
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        var thread = new System.Threading.Thread(StartDownlaodAsync);
        thread.Start();
    }

    private const string BepInExDownloadUrl = "https://builds.bepinex.dev/projects/bepinex_be/674/BepInEx-Unity.IL2CPP-win-x86-6.0.0-be.674%2B82077ec.zip";
    private static string BepInExTempPath = Path.GetTempPath() + @"TONX.Installer.BepInEx.zip";

    private static List<string> GameFilesWhiteList = new()
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
    private static List<string> BepInExFilesWhiteList = new()
    {
        "BepInEx",
        "dotnet",
        ".doorstop_version",
        "changelog.txt",
        "doorstop_config.ini",
        "winhttp.dll",
    };

    public bool IsLastStepAvailable => false;
    public bool IsNextStepAvailable => AllDone;

    private static bool AllDone = false;
    public static string FaildMsg = string.Empty;
    private bool DownloadingBepInEx = false;
    private bool DownloadingPlugin = false;

    private async void StartDownlaodAsync()
    {
        string gamePath = SelectPathPage.Current.SelectedPath.TrimEnd('\\').TrimEnd('/');
        string pluginsPath = gamePath + "/BepInEx/plugins";

        // 删除多余文件
        try
        {
            var whiteList = GameFilesWhiteList;
            if (CheckIntegrityOfBepInEx(gamePath)) whiteList.AddRange(BepInExFilesWhiteList);
            foreach (var file in Directory.GetFiles(gamePath).Where(f => !GameFilesWhiteList.Any(w => f.EndsWith(w))))
                File.Delete(file);
            foreach (var folder in Directory.GetDirectories(gamePath).Where(f => !GameFilesWhiteList.Any(w => f.EndsWith(w))))
                Directory.Delete(folder, true);

            if (Directory.Exists(pluginsPath))
            {
                foreach (var file in Directory.GetFiles(pluginsPath))
                    File.Delete(file);
                foreach (var folder in Directory.GetDirectories(pluginsPath))
                    Directory.Delete(folder, true);
            }
        }
        catch (Exception ex)
        {
            UpdateBepInExProgress($"{Lang.FailedTo_Cleanup}: {ex.Message}", null);
            SetFailed(Lang.PlzTryAgainWithUAC);
            return;
        }

        // 检查是否需要安装 BepInEx
        if (CheckIntegrityOfBepInEx(gamePath))
        {
            UpdateBepInExProgress($"{Lang.Install_BepInEx_Done}: {GetBepInExVersion(gamePath)}", 100);
            goto SkippedBepInEx;
        }

        // 下载 BepInEx
        DownloadingBepInEx = true; DownloadingPlugin = false;
        string bepInExDownloadRet = DownloadFile(BepInExDownloadUrl, BepInExTempPath).Result;
        if (!string.IsNullOrEmpty(bepInExDownloadRet))
        {
            UpdateBepInExProgress($"{Lang.Download_BepInEx_Failed}: {bepInExDownloadRet}", null);
            SetFailed(Lang.PlzTryAgainUsingOtherDownlodChennel);
            return;
        }

        // 解压 BepInEx 至游戏目录
        UpdateBepInExProgress(Lang.Install_BepInEx_Extracting, null);
        try { ZipFile.ExtractToDirectory(BepInExTempPath, gamePath); }
        catch (Exception ex)
        {
            UpdateBepInExProgress($"{Lang.FailedTo_Extracting}: {ex.Message}", null);
            SetFailed(Lang.PlzTryAgainWithUAC);
            return;
        }

        // 删除缓存文件（不需要报错）
        File.Delete(BepInExTempPath);

        UpdateBepInExProgress($"{Lang.Install_BepInEx_Done}: {GetBepInExVersion(gamePath)}", 100);

    SkippedBepInEx:

        //下载 TONX.dll
        DownloadingPlugin = true; DownloadingBepInEx = false;
        string pluginPath = pluginsPath + "/TONX.dll";
        if (!Directory.Exists(pluginsPath)) Directory.CreateDirectory(pluginsPath);
        UpdatePluginProgress(Lang.Download_TONX_Requesting, null);
        string pluginUrl = SelectDownlaodChannelPage.SelectedChannel.URLFunc.Invoke();
        if (pluginUrl == null)
        {
            UpdatePluginProgress($"{Lang.Download_TONX_Failed}: {System.Net.HttpStatusCode.NotFound}", null);
            SetFailed(Lang.PlzTryAgainUsingOtherDownlodChennel);
            return;
        }
        string pluginDownloadRet = DownloadFile(pluginUrl, pluginPath).Result;
        if (!string.IsNullOrEmpty(pluginDownloadRet))
        {
            UpdatePluginProgress($"{Lang.Download_TONX_Failed}: {pluginDownloadRet}", null);
            SetFailed(Lang.PlzTryAgainUsingOtherDownlodChennel);
            return;
        }

        UpdatePluginProgress(Lang.Install_TONX_Done, 100);

        //完成
        AllDone = true;
        FaildMsg = string.Empty;
        PageControl.GetPageByInstance(DonePage.Current).Show();
    }

    private async Task<string> DownloadFile(string url, string fileName)
    {
        using var client = new HttpClientDownloadWithProgress(url, fileName);
        client.ProgressChanged += OnDownloadProgressChanged;

        try { await client.StartDownload(); }
        catch (Exception ex)
        { return ex.Message; }

        return string.Empty;
    }

    private int lastUpdateTime;
    private void OnDownloadProgressChanged(long? totalFileSize, long totalBytesDownloaded, double? progressPercentage)
    {
        if (lastUpdateTime == DateTime.Now.Millisecond / 200) return;
        lastUpdateTime = DateTime.Now.Millisecond / 200;

        Current.DispatcherQueue.TryEnqueue(() =>
        {
            string percentageText = $" ({progressPercentage:#0}%)";
            if (DownloadingPlugin) UpdatePluginProgress(Lang.Install_TONX_Downloading + percentageText, progressPercentage);
            else if (DownloadingBepInEx) UpdateBepInExProgress(Lang.Install_BepInEx_Downloading + percentageText, progressPercentage);
        });
    }

    private static void SetFailed(string msg)
    {
        AllDone = true;
        FaildMsg = msg;
        MainPage.Current.UpdateButtonsStatus();
    }

    private bool CheckIntegrityOfBepInEx(string path) // Incomplete
    {
        return !(
            !Directory.Exists(path)
            || !Directory.Exists(path + "/BepInEx/core")
            || !Directory.Exists(path + "/dotnet")
            || (!File.Exists(path + "/winhttp.dll"))
            );
    }
    private string GetBepInExVersion(string path)
    {
        string fileName = path + "/BepInEx/core/BepInEx.Core.dll";
        if (!CheckIntegrityOfBepInEx(path) || !File.Exists(fileName)) return null;
        string version = FileVersionInfo.GetVersionInfo(fileName).ProductVersion;
        version = Regex.Replace(version, @"\+.+", string.Empty);
        return version;
    }
}
