using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ClothGenerator))]
public class ClothEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ClothGenerator myScript = (ClothGenerator)target;
        if (GUILayout.Button("Generate Cloth"))
        {
            myScript.Initialize();
        }
    }

}
