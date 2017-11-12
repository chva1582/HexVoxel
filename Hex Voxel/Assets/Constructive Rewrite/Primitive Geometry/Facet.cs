using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Facet : IEquatable<Facet>
{
    //Organized clockwise when looking at the face
    public HexCell first, second, third;

    public Facet(HexCell f, HexCell s, HexCell t)
    {
        first = f;
        second = s;
        third = t;
    }

    #region Equals
    public bool Equals(Facet obj)
    {
        bool firstBool = (first == obj.first || first == obj.second || first == obj.third);
        bool secondBool = (second == obj.first || second == obj.second || second == obj.third);
        bool thirdBool = (third == obj.first || third == obj.second || third == obj.third);
        return firstBool && secondBool && thirdBool;
    }

    public override bool Equals(object obj)
    {
        bool firstBool = (first == ((Facet)obj).first || first == ((Facet)obj).second || first == ((Facet)obj).third);
        bool secondBool = (second == ((Facet)obj).first || second == ((Facet)obj).second || second == ((Facet)obj).third);
        bool thirdBool = (third == ((Facet)obj).first || third == ((Facet)obj).second || third == ((Facet)obj).third);
        return firstBool && secondBool && thirdBool;
    }

    public override int GetHashCode()
    {
        int firstHash = first.GetHashCode();
        int secondHash = second.GetHashCode();
        int thirdHash = third.GetHashCode();
        int buffer;
        if(firstHash > secondHash)
        {
            buffer = firstHash;
            firstHash = secondHash;
            secondHash = buffer;
        }
        if(secondHash > thirdHash)
        {
            buffer = secondHash;
            secondHash = thirdHash;
            thirdHash = buffer;
        }
        if (firstHash > secondHash)
        {
            buffer = firstHash;
            firstHash = secondHash;
            secondHash = buffer;
        }
        unchecked
        {
            int hash = 47;
            hash = hash * 227 + firstHash;
            hash = hash * 227 + secondHash;
            hash = hash * 227 + thirdHash;
            return hash;
        }
    }

    public static bool operator ==(Facet left, Facet right)
    {
        return right.Equals(left);
    }

    public static bool operator !=(Facet left, Facet right)
    {
        return !right.Equals(left);
    }
    #endregion
}
