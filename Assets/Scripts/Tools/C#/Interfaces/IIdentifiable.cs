using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IIdentifiable
{
    string ID { get; set; }
    void GenerateID();
}
