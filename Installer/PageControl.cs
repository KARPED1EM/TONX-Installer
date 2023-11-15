using Installer.Pages;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Installer;

public class PageControl
{
    public Type PageType => PageClass.GetType();
    public object PageClass { get; private set; }

    public int IndexOfAllPages { get => AllPages.IndexOf(this); }
    public int IndexOfValidPages { get => AllAvailablePages.IndexOf(this); }

    public bool Showing { get => CurrentPage == this; }

    public bool ShouldShow => PageClass is not IPage ipage || ipage.ShouldShow;
    public bool NextStepAvailable => PageClass is not IPage ipage || ipage.IsNextStepAvailable;
    public bool LastStepAvailable => PageClass is not IPage ipage || ipage.IsLastStepAvailable;

    public bool HasLastPage => IndexOfValidPages > 0;
    public PageControl GetLastPage() => HasLastPage ? AllAvailablePages[IndexOfValidPages - 1] : null;
    public bool HasNextPage => IndexOfValidPages < AllAvailablePages.Count - 1;
    public PageControl GetNextPage() => HasNextPage ? AllAvailablePages[IndexOfValidPages + 1] : null;

    public static PageControl GetPageByType(Type type) => AllPages.Find(p => p.PageType == type);
    public static PageControl GetPageByInstance(object instance) => AllPages.Find(p => p.PageType == instance.GetType());

    public void Show() => MainPage.Current.NavigateTo(PageClass);
    public void GotoLastPage() => GetLastPage()?.Show();
    public void GotoNextPage()
    {
        if (HasNextPage) GetNextPage()?.Show();
        else Application.Current.Exit();
    }

    public static List<PageControl> AllPages = new();
    public static List<PageControl> AllAvailablePages => AllPages.Where(p => p.ShouldShow).ToList();
    public static PageControl CurrentPage => AllPages.Find(p => p.PageType == CurrentPageType);
    public static Type CurrentPageType;

    public static void Init()
    {
        AllPages.Add(new() { PageClass = new SelectPathPage() });
        AllPages.Add(new() { PageClass = new UninstallOtherModsPage() });
        AllPages.Add(new() { PageClass = new SelectDownlaodChannelPage() });
        AllPages.Add(new() { PageClass = new DownloadPage() });
        AllPages.Add(new() { PageClass = new DonePage() });

        MainPage.Current.NavigateTo(AllPages.First().PageClass);
        MainPage.Current.UpdateButtonsStatus();
    }
}
