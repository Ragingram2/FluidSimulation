using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Volume
{
    private (Vector3Int minBnd, Vector3Int maxBnd) bounds;
    public (Vector3Int minBnd, Vector3Int maxBnd) Bounds => bounds;

    private Vector3Int boundDims;
    public Vector3Int BoundDims => boundDims;

    private int width;
    public int Width => width;

    private int height;
    public int Height => height;

    private int depth;
    public int Depth => depth;

    public Volume() { }
    public Volume((Vector3Int minBnd, Vector3Int maxBnd) _bounds)
    {
        bounds = _bounds;
        width = bounds.maxBnd.x - bounds.minBnd.x;
        height = bounds.maxBnd.y - bounds.minBnd.y;
        depth = bounds.maxBnd.z - bounds.minBnd.z;
        boundDims = new Vector3Int(width, height, depth);
    }

    public Volume SetNewBounds((Vector3Int minBnd, Vector3Int maxBnd) newBounds)
    {
        bounds = newBounds;
        width = bounds.maxBnd.x - bounds.minBnd.x;
        height = bounds.maxBnd.y - bounds.minBnd.y;
        depth = bounds.maxBnd.z - bounds.minBnd.z;
        return this;
    }

    public string GetCellKey(Vector3 _vec)
    {
        var vec = GetCellIndex(_vec);
        return GetCellKey(vec.x, vec.y, vec.z);
    }


    public string GetCellKey(int _x, int _y, int _z)
    {
        string key = _x + "." + _y + "." + _z;
        return key;
    }

    public Vector3Int GetCellIndex(Vector3 vec)
    {
        return GetCellIndex(vec.x, vec.y, vec.z);
    }

    public Vector3Int GetCellIndex(float _x, float _y, float _z)
    {
        float xVal = Mathf.Clamp01((_x - bounds.minBnd.x) / (width));
        float yVal = Mathf.Clamp01((_y - bounds.minBnd.y) / (height));
        float zVal = Mathf.Clamp01((_z - bounds.minBnd.z) / (depth));

        int xIndex = (int)Mathf.Floor(xVal * (width - 1));
        int yIndex = (int)Mathf.Floor(yVal * (height - 1));
        int zIndex = (int)Mathf.Floor(zVal * (depth - 1));
        return new Vector3Int(xIndex, yIndex, zIndex);
    }

    private Vector3 convertToVec(string key)
    {
        var vals = key.Split(',');
        return new Vector3(int.Parse(vals[0]), int.Parse(vals[1]), int.Parse(vals[2]));
    }

    public List<string> FindNearby(string targetKey, float rad)
    {
        var pos = convertToVec(targetKey);
        var i1 = GetCellIndex(pos.x - rad, pos.y - rad, pos.z - rad);
        var i2 = GetCellIndex(pos.x + rad, pos.y + rad, pos.z + rad);

        var clients = new List<string>();

        for (int x = i1[0], xn = i2[0]; x <= xn; ++x)
        {
            for (int y = i1[1], yn = i2[1]; y <= yn; ++y)
            {
                for (int z = i1[2], zn = i2[2]; z <= zn; ++z)
                {
                    var key = GetCellKey(x, y, z);
                    clients.Add(key);
                }
            }
        }
        return clients;
    }

}


public class Particle
{
    public Vector3 position;
    public string key;
    public float radius;
}


public class ParticleSpatialVolume : MonoBehaviour
{
    [SerializeField]
    private Volume volume = new Volume();
    private Dictionary<string, List<Particle>> cells = new Dictionary<string, List<Particle>>();

    public void Init((Vector3Int minBnd, Vector3Int maxBnd) bounds)
    {
        volume.SetNewBounds(bounds);
    }

    public void Step(float searchRadius)
    {
        //Clear neighborList
        foreach (List<Particle> cell in cells.Values)
        {
            cell.Clear();
        }

        //parsing particles into keys
        foreach (Particle p in Simulator.particles)
        {
            string key = volume.GetCellKey(p.position);
            p.key = key;
            if (!cells.ContainsKey(key))
                cells.Add(key,new List<Particle>());
            cells[key].Add(p);
        }

        //Generate new neighborList
        foreach (string cell in cells.Keys)
        {
            var keyList = volume.FindNearby(cell, searchRadius);
            foreach (string k in keyList)
            {
                cells[cell].AddRange(cells[k]);
            }
        }
    }

    public Vector3Int MinimumBound { get { return volume.Bounds.minBnd; } }
    public Vector3Int MaximumBound { get { return volume.Bounds.maxBnd; } }
    public Vector3Int BoundDims { get { return volume.BoundDims; } }

}
