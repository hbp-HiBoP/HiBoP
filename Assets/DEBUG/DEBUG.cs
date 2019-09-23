using UnityEngine;
using UnityEditor;
using HBP.Data.Tags;

public static class DEBUG
{
    [MenuItem("DEBUG/Adrien/Main")]
    private static void Main()
    {
        IntTag ageTag = new IntTag("age", true, 0, 100);
        IntTag PoidsTag = new IntTag("poids", true, 0, 50);
        EmptyTag emptyTag = new EmptyTag();
        IntTagValue age = new IntTagValue(ageTag, 245);
        IntTagValue poids = new IntTagValue(PoidsTag, 23);
        poids.Copy(age);
        Debug.Log(age.Value);
        Debug.Log(poids.Value);
    }
}