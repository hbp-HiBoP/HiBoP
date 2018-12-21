using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class TransformExtension
{
    public static string GetFullName( this Transform transform)
    {
        StringBuilder stringBuilder = new StringBuilder();
        List<string> names = new List<string>();
        Transform tr = transform;
        while(tr != null)
        {
            names.Add(tr.name);
            tr = tr.parent;
        }
        int size = names.Count;
        for (int i = size - 1; i >= 0; i--)
        {
            stringBuilder.Append(names[i]);
            if(i > 0) stringBuilder.Append("/");
        }
        return stringBuilder.ToString();
    }
}
