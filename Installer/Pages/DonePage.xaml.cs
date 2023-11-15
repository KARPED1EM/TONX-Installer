using Installer.Localization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Installer.Pages;

public sealed partial class DonePage : Page, IPage
{
    public static DonePage Current { get; private set; }

    public DonePage()
    {
        Current = this;

        this.InitializeComponent();
    }

    public bool IsLastStepAvailable => false;

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        MessageTextBlock.Text = string.IsNullOrEmpty(DownloadPage.FaildMsg)
            ? Lang.AllDoneTips
            : DownloadPage.FaildMsg;
    }
}
