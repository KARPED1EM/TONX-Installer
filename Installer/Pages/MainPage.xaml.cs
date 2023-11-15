using CommunityToolkit.Mvvm.ComponentModel;
using Installer.Localization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace Installer.Pages;

[INotifyPropertyChanged]
public sealed partial class MainPage : Page
{
    public static MainPage Current { get; private set; }

    private string AppTitle { get; set; } = Lang.TownOfNextInstaller;
    private string AppVersion { get; set; } = "1.0.0";

    [ObservableProperty]
    private Visibility lastStepButtonVisibility;
    [ObservableProperty]
    private bool lastStepButtonEnabled;
    [ObservableProperty]
    private string nextStepButtonContent;
    [ObservableProperty]
    private bool nextStepButtonEnabled;

    public MainPage()
    {
        Current = this;

        this.InitializeComponent();

        PageControl.Init();
    }

    public async void UpdateButtonsStatus()
    {
        Current.DispatcherQueue.TryEnqueue(async () =>
        {
            var pc = PageControl.CurrentPage;
            LastStepButtonVisibility = pc.HasLastPage ? Visibility.Visible : Visibility.Collapsed;
            LastStepButtonEnabled = pc.LastStepAvailable;
            NextStepButtonEnabled = pc.NextStepAvailable;
            NextStepButtonContent = pc.HasNextPage ? Lang.NextStep : Lang.Done;
        });
    }

    private void LastStepButton_Click(object sender, RoutedEventArgs e) => PageControl.CurrentPage.GotoLastPage();
    private void NextStepButton_Click(object sender, RoutedEventArgs e) => PageControl.CurrentPage.GotoNextPage();

#nullable enable
    public void NavigateTo(object page)
    {
        var pc = page is Type type
            ? PageControl.GetPageByType(type)
            : PageControl.GetPageByInstance(page);
        PageControl.CurrentPageType = pc.PageType;
        Current.DispatcherQueue.TryEnqueue(() =>
        {
            Content_Frame.Content = pc.PageClass;
            UpdateButtonsStatus();
        });
    }
}
