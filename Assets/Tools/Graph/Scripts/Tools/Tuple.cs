using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Tuple<T1,T2>
{
    public T1 Object1;
    public T2 Object2;

    public Tuple(T1 object1,T2 object2)
    {
        Object1 = object1;
        Object2 = object2;
    }
    public override bool Equals(object obj)
    {
        if(obj is Tuple<T1,T2>)
        {
            Tuple<T1, T2> objCasted = (Tuple<T1, T2>) obj;
            if (objCasted.Object1.Equals(Object1) && objCasted.Object2.Equals(Object2))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        else
        {
            return false;
        }
    }
    public override int GetHashCode()
    {
        return Object1.GetHashCode() * Object2.GetHashCode();
    }
}
