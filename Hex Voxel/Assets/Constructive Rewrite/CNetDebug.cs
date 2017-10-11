using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CNetDebug
{

    public static void DrawEdge(Vector3 startPoint, Vector3 endPoint, Color color)
    {
        
        Debug.DrawLine(startPoint, endPoint, color, 10);
    }
}
