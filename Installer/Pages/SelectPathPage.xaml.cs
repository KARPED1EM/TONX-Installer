using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace Installer.Pages;

public sealed partial class SelectPathPage : Page, IPage
{
    public static SelectPathPage Current { get; private set; }
    private ObservableCollection<string> PathsItemsSource
        => FindGameService.FoundGamePaths.ToObservableCollection();
    public string SelectedPath { get; set; }

    private bool pingTaskDone = false;

    public SelectPathPage()
    {
        Current = this;

        FindGameService.SearchAllByRegistry();

        this.InitializeComponent();

        new Thread(() =>
        {
            ModDownloadChannel.Init();
            pingTaskDone = true;
            MainPage.Current.UpdateButtonsStatus();
        }).Start();
    }

    public bool IsNextStepAvailable => FindGameService.IsValidAmongUsFolder(SelectedPath) && pingTaskDone;

    private void SelectGameFolderCombo_Loaded(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(SelectedPath))
        {
            if (SelectGameFolderCombo.Items.Count >= 1)
                SelectGameFolderCombo.SelectedIndex = 0;
        }
        else
        {
            SelectGameFolderCombo.Text = SelectedPath;
        }
        CheckForPathAndUpdateUI(SelectedPath);
    }

    private async void PickGameFolderButton_Click(object sender, RoutedEventArgs e)
    {
        FolderPicker openPicker = new FolderPicker();
        WinRT.Interop.InitializeWithWindow.Initialize(openPicker, MainWindow.Current.hWnd);

        openPicker.SuggestedStartLocation = PickerLocationId.Desktop;
        openPicker.FileTypeFilter.Add("*");

        StorageFolder folder = await openPicker.PickSingleFolderAsync();
        if (folder != null)
        {
            SelectGameFolderCombo.Text = folder.Path;
            CheckForPathAndUpdateUI(folder.Path);
        }
    }

    private void SelectGameFolderCombo_TextSubmitted(ComboBox sender, ComboBoxTextSubmittedEventArgs args)
        => CheckForPathAndUpdateUI(args.Text);

    private void SelectGameFolderCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        => CheckForPathAndUpdateUI(e.AddedItems.First().ToString());

    private void CheckForPathAndUpdateUI(string path)
    {
        SelectedPath = SelectGameFolderCombo.Text;
        NotificationForGameFolder.Visibility = FindGameService.IsValidAmongUsFolder(path) ? Visibility.Collapsed : Visibility.Visible;
        MainPage.Current.UpdateButtonsStatus();
    }


}
