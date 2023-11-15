using Installer.Pages;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;
using Vanara.PInvoke;
using Windows.Graphics;
using WinRT.Interop;

namespace Installer;

public sealed partial class MainWindow : Window
{
    public static new MainWindow Current { get; private set; }
    public IntPtr hWnd { get; private set; }
    public double UIScale => User32.GetDpiForWindow(hWnd) / 96d;

    public MainWindow()
    {
        Current = this;
        this.InitializeComponent();
        InitializeMainWindow();
    }

    private void InitializeMainWindow()
    {
        hWnd = WindowNative.GetWindowHandle(this);
        SystemBackdrop = new DesktopAcrylicBackdrop();

        var titleBar = AppWindow.TitleBar;
        titleBar.IconShowOptions = IconShowOptions.HideIconAndSystemMenu;
        titleBar.ExtendsContentIntoTitleBar = true;
        titleBar.ButtonBackgroundColor = Colors.Transparent;
        titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
        SetDragRectangles(new RectInt32(0, 0, 100000, (int)(48 * UIScale)));

        ResizeToCertainSize();

        MainWindow_Frame.Content = new MainPage();
    }

    public void SetDragRectangles(params RectInt32[] value)
    {
        AppWindow.TitleBar.SetDragRectangles(value);
    }

    public void ResizeToCertainSize(int width = 0, int height = 0)
    {
        var display = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary);
        var scale = UIScale;
        if (width * height == 0)
        {
            width = (int)(800 * scale);
            height = (int)(540 * scale);
        }
        else
        {
            width = (int)(width * scale);
            height = (int)(height * scale);
        }
        var x = (display.WorkArea.Width - width) / 2;
        var y = (display.WorkArea.Height - height) / 2;
        AppWindow.MoveAndResize(new RectInt32(x, y, width, height));
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsMaximizable = false;
            presenter.IsResizable = false;
        }
    }
}
