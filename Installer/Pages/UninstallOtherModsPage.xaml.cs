using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Installer.Pages;

[INotifyPropertyChanged]
public sealed partial class UninstallOtherModsPage : Page, IPage
{
    public static UninstallOtherModsPage Current { get; private set; }

    [ObservableProperty]
    public ObservableCollection<string> waitToDelItemsSource;

    public static string PluginPath => SelectPathPage.Current.SelectedPath + "/BepInEx/plugins";

    public UninstallOtherModsPage()
    {
        Current = this;

        this.InitializeComponent();
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        WaitToDelItemsSource = GetWaitToDelItemsNames().ToObservableCollection();
    }

    public bool ShouldShow => GetOtherModsPaths()?.Any() ?? false;

    private static IEnumerable<string> GetOtherModsPaths()
        => Directory.Exists(PluginPath)
        ? Directory.EnumerateFiles(PluginPath, "*.dll", SearchOption.AllDirectories)
        : default;

    private static List<string> GetWaitToDelItemsNames()
    {
        var list = new List<string>();
        if (GetOtherModsPaths() == null) return new();
        foreach (var item in GetOtherModsPaths())
            list.Add(Path.GetFileNameWithoutExtension(item));
        return list;
    }
}
