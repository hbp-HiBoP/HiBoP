using UnityEngine;
using Ionic.Zip;
using System.IO;
using CielaSpike;
using HBP.UI;
using HBP.UI.Tags;
using HBP.Data.Tags;

public class DEBUG : MonoBehaviour
{
    public TagList tagList;

    private void Start()
    {
        //tagModifier.Item = new FloatTag("Poids", true, 0,200);
        Tag tag = new EnumTag("Latéralité", new string[] { "Gaucher", "Droitier", "Ambidextre" });
        Tag tag2 = new IntTag("Age");
        Tag tag3 = new FloatTag("Poids");
        Tag tag4 = new EmptyTag("Tutu");
        Tag tag5 = new BoolTag("Vivant");
        Tag[] tags = new Tag[] { tag, tag2, tag3, tag4, tag5 };
        //tagList.Objects = tags;
        tagList.Initialize();
        foreach (var item in tags)
        {
            tagList.Add(item);
        }
    }
}