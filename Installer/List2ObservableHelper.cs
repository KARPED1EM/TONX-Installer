using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Installer;

public static class List2ObservableHelper
{
    public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> enumerable)
    {
        return new ObservableCollection<T>(enumerable);
    }
    public static ObservableCollection<T> ToObservableCollection<T>(this List<T> enumerable)
    {
        return new ObservableCollection<T>(enumerable);
    }
}