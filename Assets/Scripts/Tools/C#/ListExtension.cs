using System.Collections.Generic;

public static class ListExtension
{
    public static void Add<T>(this List<T> list,IEnumerable<T> itemsToAdd)
    {
        foreach (var item in itemsToAdd) list.Add(item);
    }
    public static void Remove<T>(this List<T> list, IEnumerable<T> itemsToRemove)
    {
        foreach (var item in itemsToRemove) list.Remove(item);
    }
}
