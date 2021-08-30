using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;


public class Client
{
    public Vector3 position;
    public Vector3 size;
    public Vector3Int[] indicies;
    public string key;

    public void MoveClient(Vector3 velocity)
    {
        velocity = new Vector3(velocity.x, 0, velocity.y);
        position += velocity * Time.deltaTime;
    }
}

public class SpatialHashing : MonoBehaviour
{
    [SerializeField]
    private (Vector3 minBnd, Vector3 maxBnd) bounds;
    [SerializeField]
    private Vector3 dimensions;
    [SerializeField]
    private Vector3 searchSize;
    Vector3 cellSize;
    private float width;
    private float height;
    private float depth;
    private Dictionary<string, HashSet<Client>> cells = new Dictionary<string, HashSet<Client>>();
    private List<Client> clients = new List<Client>();
    private List<GameObject> clientGos = new List<GameObject>();
    private Client target;
    private HashSet<Client> nearby;

    private void Start()
    {
        bounds = ((-Vector3.one) * (dimensions.x / 2), Vector3.one * (dimensions.y / 2));
        width = bounds.maxBnd.x - bounds.minBnd.x;
        height = bounds.maxBnd.y - bounds.minBnd.y;
        depth = bounds.maxBnd.z - bounds.minBnd.z;
        cellSize = new Vector3(width / dimensions.x, height / dimensions.y, depth / dimensions.z);

        GameObject go;
        for (int i = 0; i < 50; i++)
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Vector3 randPos = Random.insideUnitSphere;
            randPos.x *= dimensions.x;
            randPos.y *= dimensions.y;
            randPos.z *= dimensions.z;
            go.transform.position = Vector3.zero + randPos;
            var client = NewClient(go.transform.position, Vector3.one);
            if (i == 0)
            {
                target = client;
            }
            clients.Add(client);
            clientGos.Add(go);
        }
    }

    private void Update()
    {
        if (nearby != null)
            nearby.Clear();
        foreach (Client client in clients)
        {
            UpdateClient(client, Random.insideUnitSphere);
        }
        nearby = FindNearby(target.position, searchSize);
        clientGos[clients.IndexOf(target)].GetComponent<MeshRenderer>().material.color = Color.blue;
        for (int i = 0; i < clients.Count; i++)
        {
            if (nearby.Contains(clients[i]))
            {
                clientGos[i].GetComponent<MeshRenderer>().material.color = Color.green;
            }
            else
            {
                clientGos[i].GetComponent<MeshRenderer>().material.color = Color.red;
            }
            clientGos[i].transform.position = clients[i].position;
        }

    }

    private void OnDrawGizmos()
    {
        if (target != null)
        {
            Gizmos.color = new Color(0, 0, 1, .5f);
            Gizmos.DrawCube(clientGos[clients.IndexOf(target)].transform.position, searchSize);
        }
        string[] comps;
        Vector3 pos;
        //foreach (string key in cells.Keys)
        //{
        //    if (cells[key].Count > 0)
        //    {
        //        comps = key.Split('.');
        //        pos = new Vector3(int.Parse(comps[0]), int.Parse(comps[1]), int.Parse(comps[2]));
        //        Gizmos.DrawCube(pos, cellSize);
        //    }
        //}
        Gizmos.color = new Color(0, 1, 1, .5f);
        if (nearby.Count > 0)
        {
            foreach (Client client in nearby)
            {
                comps = client.key.Split('.');
                pos = new Vector3(int.Parse(comps[0]), int.Parse(comps[1]), int.Parse(comps[2]));
                Gizmos.DrawCube(pos, cellSize);
            }
        }
        //Debug.Log("Cell Count: " + cells.Keys.Count);
    }

    public void UpdateClient(Client _client, Vector3 velocity)
    {
        _client.MoveClient(velocity);
        removeClient(_client);
        insertClient(_client);
    }

    public Client NewClient(Vector3 _position, Vector3 _size)
    {
        var client = new Client()
        {
            position = _position,
            size = _size,
            indicies = null,
            key = "0.0.0"
        };
        insertClient(client);
        return client;
    }

    private void insertClient(Client _client)
    {
        float xPos, yPos, zPos, width, height, depth;
        xPos = _client.position.x;
        yPos = _client.position.y;
        zPos = _client.position.z;

        width = _client.size.x;
        height = _client.size.y;
        depth = _client.size.z;

        var i1 = getCellIndex(xPos - width / 2, yPos - height / 2, zPos - depth / 2);
        var i2 = getCellIndex(xPos + width / 2, yPos + height / 2, zPos + depth / 2);

        _client.indicies = new Vector3Int[2] { i1, i2 };

        for (int x = i1[0], xn = i2[0]; x <= xn; ++x)
        {
            for (int y = i1[1], yn = i2[1]; y <= yn; ++y)
            {
                for (int z = i1[2], zn = i2[2]; z <= zn; ++z)
                {
                    string key = generateKey(x, y, z);
                    if (!cells.ContainsKey(key))
                    {
                        cells[key] = new HashSet<Client>();
                    }
                    _client.key = key;
                    cells[key].Add(_client);
                }
            }
        }
    }
    private void removeClient(Client _client)
    {
        Vector3Int i1, i2;
        i1 = _client.indicies[0];
        i2 = _client.indicies[1];

        for (int x = i1[0], xn = i2[0]; x <= xn; ++x)
        {
            for (int y = i1[1], yn = i2[1]; y <= yn; ++y)
            {
                for (int z = i1[2], zn = i2[2]; z <= zn; ++z)
                {
                    string key = generateKey(x, y, z);
                    cells[key].Remove(_client);
                }
            }
        }
    }
    public HashSet<Client> FindNearby(Vector3 _position, Vector3 _bounds)
    {
        float xPos, yPos, zPos, width, height, depth;
        xPos = _position.x;
        yPos = _position.y;
        zPos = _position.z;
        width = _bounds.x;
        height = _bounds.y;
        depth = _bounds.z;

        var i1 = getCellIndex(xPos - width / 2, yPos - height / 2, zPos - depth / 2);
        var i2 = getCellIndex(xPos + width / 2, yPos + height / 2, zPos + depth / 2);

        var clients = new HashSet<Client>();

        for (int x = i1[0], xn = i2[0]; x <= xn; ++x)
        {
            for (int y = i1[1], yn = i2[1]; y <= yn; ++y)
            {
                for (int z = i1[2], zn = i2[2]; z <= zn; ++z)
                {
                    string key = generateKey(x, y, z);

                    if (cells.ContainsKey(key))
                    {
                        foreach (var value in cells[key])
                        {
                            clients.Add(value);
                        }
                    }
                }
            }
        }
        return clients;
    }

    private string generateKey(int _x, int _y, int _z)
    {
        string key = _x + "." + _y + "." + _z;
        return key;
    }

    private Vector3Int getCellIndex(Vector3 vec)
    {
        return getCellIndex(vec.x, vec.y, vec.z);
    }

    private Vector3Int getCellIndex(float _x, float _y, float _z)
    {
        float xVal = Mathf.Clamp01((_x - bounds.minBnd.x) / (width));
        float yVal = Mathf.Clamp01((_y - bounds.minBnd.y) / (height));
        float zVal = Mathf.Clamp01((_z - bounds.minBnd.z) / (depth));

        int xIndex = (int)Mathf.Floor(xVal * (dimensions.x - 1));
        int yIndex = (int)Mathf.Floor(yVal * (dimensions.y - 1));
        int zIndex = (int)Mathf.Floor(zVal * (dimensions.z - 1));
        return new Vector3Int(xIndex, yIndex, zIndex);
    }




}
