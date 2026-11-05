using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace RevEx.Desktop.ViewModels.Common;

/// <summary>Обновляет отображаемые списки, сохраняя сам экземпляр коллекции и привязки интерфейса.</summary>
public static class ObservableCollectionExtensions
{
    public static void ReplaceWith<T>(this ObservableCollection<T> collection, IEnumerable<T> items)
    {
        var snapshot = items.ToArray();
        collection.Clear();
        foreach (var item in snapshot)
        {
            collection.Add(item);
        }
    }
}
