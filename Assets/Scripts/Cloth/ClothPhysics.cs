using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClothPhysics : MonoBehaviour
{

    Texel[] texels;
    int width; int height;
    public void Initialize(int _width, int _height)
    {
        width = _width;
        height = _height;

        texels = new Texel[width*height];
    }
}
