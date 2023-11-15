using Installer.Localization;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading;
using Windows.Data.Json;

namespace Installer.Pages;

public sealed partial class SelectDownlaodChannelPage : Page, IPage
{
    public static SelectDownlaodChannelPage Current { get; private set; }
    private ObservableCollection<ModDownloadChannel> DownloadChannelItemsSource
        => ModDownloadChannel.Channels.ToObservableCollection();

    private static ModDownloadChannel _SelectedChannel;
    public static ModDownloadChannel SelectedChannel
    {
        get => _SelectedChannel ?? ModDownloadChannel.Channels.First();
        set => _SelectedChannel = value;
    }

    public SelectDownlaodChannelPage()
    {
        Current = this;

        this.InitializeComponent();
    }

    private void SelectDownloadChannelRadioButtons_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        => SelectDownloadChannelRadioButtons.SelectedItem = SelectedChannel;

    private void SelectDownloadChannelRadioButtons_SelectionChanged(object sender, SelectionChangedEventArgs e)
        => SelectedChannel = e.AddedItems.First() as ModDownloadChannel;

    public static string Get(string url)
    {
        string result = "";
        HttpClient req = new HttpClient();
        var res = req.GetAsync(url).Result;
        Stream stream = res.Content.ReadAsStreamAsync().Result;
        try
        {
            //»ñÈ¡ÄÚÈÝ
            using StreamReader reader = new(stream);
            result = reader.ReadToEnd();
        }
        finally
        {
            stream.Close();
        }
        return result;
    }
}

public class ModDownloadChannel : IComparable<ModDownloadChannel>
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string RootURL { get; set; }
    public Func<string> URLFunc { get; set; }

    private long _Ping = 0;
    public long Ping => _Ping;
    public string PingResult
    {
        get
        {
            int triedTimes = 0;
            while (_Ping == 0)
            {
                triedTimes++;

                Ping pingSender = new();
                PingReply reply = pingSender.Send(RootURL, timeout: 120);

                if (reply.Status == IPStatus.Success) _Ping = reply.RoundtripTime;
                else if (triedTimes >= 3) _Ping = -1;
                else Thread.Sleep(1000);
            }
            return _Ping != -1 ? _Ping + " ms" : Lang.Timeout;
        }
    }

    public static void Init()
    {
        Channels = GetChannels();
        foreach (var channel in Channels)
            _ = channel.PingResult;
        SelectDownlaodChannelPage.SelectedChannel = Channels.Order().Min();
    }

    public int CompareTo(ModDownloadChannel other)
        => Ping.CompareTo(other.Ping);

    public static List<ModDownloadChannel> Channels;
    public static List<ModDownloadChannel> GetChannels()
    {
        return new()
        {
            new()
            {
                Name = "Github",
                Description = Lang.DownloadChannel_Github_Desc,
                RootURL = "github.com",
                URLFunc = () =>
                {
                    return "https://github.com/KARPED1EM/TownOfNext/releases/latest/download/TONX.dll";
                }
            },
            new()
            {
                Name = "Gitee",
                Description = Lang.DownloadChannel_Gitee_Desc,
                RootURL = "gitee.com",
                URLFunc = () =>
                {
                    try
                    {
                        string jsRaw = SelectDownlaodChannelPage.Get("https://gitee.com/api/v5/repos/leeverz/TownOfNext/releases/latest");
                        string latestVer = JsonObject.Parse(jsRaw).GetNamedValue("tag_name").ToString().TrimStart('"').TrimEnd('"');
                        return $"https://gitee.com/leeverz/TownOfNext/releases/download/{latestVer}/TONX.dll";
                    }
                    catch
                    {
                        return null;
                    }
                }
            },
        };
    }
}