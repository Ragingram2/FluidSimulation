using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;


public class Client
{
    public Vector3 position;
    public Vector3 size;
    public Vector3Int[] indicies;
    public Vector3 velocity;
    public string key;

    public void MoveClient((Vector3 minBnd, Vector3 maxBnd) bounds)
    {
        Vector3 tempPosition = position + (velocity * Time.deltaTime);
        if (tempPosition.x <= bounds.minBnd.x || tempPosition.x >= bounds.maxBnd.x)
        {
            velocity.x *= -1;
        }
        if (tempPosition.y <= bounds.minBnd.y || tempPosition.y >= bounds.maxBnd.y)
        {
            velocity.y *= -1;
        }
        if (tempPosition.z <= bounds.minBnd.z || tempPosition.z >= bounds.maxBnd.z)
        {
            velocity.z *= -1;
        }
        position += velocity * Time.deltaTime;
    }
}

public class SpatialHashing : MonoBehaviour
{
    [SerializeField]
    private Vector3 gridCenter = Vector3.zero;
    [SerializeField]
    private (Vector3 minBnd, Vector3 maxBnd) bounds;
    [SerializeField]
    private Vector3 dimensions;
    [SerializeField]
    private Vector3 searchSize;
    [SerializeField]
    private Material mat;
    Vector3 cellSize;
    private float width;
    private float height;
    private float depth;
    private Dictionary<string, HashSet<Client>> cells = new Dictionary<string, HashSet<Client>>();
    private List<Client> clients = new List<Client>();
    private List<GameObject> clientGos = new List<GameObject>();
    private Dictionary<Vector3,GameObject> cellGos = new Dictionary<Vector3,GameObject>();
    private Client target;
    private HashSet<Client> nearby;

    private void Start()
    {
        bounds = (((-Vector3.one) * dimensions.magnitude / 2f) + gridCenter, (Vector3.one * dimensions.magnitude / 2f) + gridCenter);
        width = bounds.maxBnd.x - bounds.minBnd.x;
        height = bounds.maxBnd.y - bounds.minBnd.y;
        depth = bounds.maxBnd.z - bounds.minBnd.z;
        cellSize = new Vector3(width / dimensions.x, height / dimensions.y, depth / dimensions.z);
        Debug.Log("Dim Mag: " + dimensions.magnitude);
        Debug.Log("CellSize: " + cellSize);


        for (int i = 0; i < 25; i++)
        {
            Vector3 randPos = Random.insideUnitSphere;
            randPos.x *= dimensions.x;
            randPos.y *= dimensions.y;
            randPos.z *= dimensions.z;
            var client = NewClient(randPos, Vector3.one);
            if (i == 0)
            {
                target = client;
            }
        }

        for (float x = bounds.minBnd.x; x < bounds.maxBnd.x; ++x)
        {
            for (float y = bounds.minBnd.y; y < bounds.maxBnd.y; ++y)
            {
                for (float z = bounds.minBnd.z; z < bounds.maxBnd.z; ++z)
                {
                    GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    go.transform.position = new Vector3(x * cellSize.x, y * cellSize.y, z * cellSize.z) - (bounds.minBnd+Vector3.one);
                    go.transform.localScale = cellSize;
                    go.GetComponent<MeshRenderer>().material = mat;
                    go.GetComponent<MeshRenderer>().material.color = new Color(1, 1, 0, .1f);
                    go.SetActive(false);
                    cellGos.Add(new Vector3(x,y,z),go);
                }
            }
        }
    }

    private void Update()
    {
        if (nearby != null)
        {
            nearby.Clear();
        }
        foreach (Client client in clients)
        {
            UpdateClient(client, Random.insideUnitSphere * 10);
        }
        nearby = FindNearby(target.position, searchSize);
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
            //lientGos[i].transform.position = new Vector3(Mathf.Floor(clients[i].position.x/2), Mathf.Floor(clients[i].position.y/2), Mathf.Floor(clients[i].position.z/2));
            clientGos[i].transform.position = new Vector3(clients[i].position.x, clients[i].position.y , clients[i].position.z);
        }
        clientGos[clients.IndexOf(target)].GetComponent<MeshRenderer>().material.color = Color.blue;



        string[] comps;
        Vector3 pos;
        //resets search area box (Pretty slow IK)
        comps = target.key.Split('.');
        pos = new Vector3(int.Parse(comps[0]), int.Parse(comps[1]), int.Parse(comps[2]));
        pos += bounds.minBnd;
        cellGos[pos].SetActive(false);

        foreach (string key in cells.Keys)
        {
            comps = key.Split('.');
            pos = new Vector3(float.Parse(comps[0]), float.Parse(comps[1]), float.Parse(comps[2]));
            pos += bounds.minBnd;
            if (cells[key].Count > 0)
            {
                cellGos[pos].SetActive(true);
            }
            else
            {
                cellGos[pos].SetActive(false);
            }
        }

        if (nearby != null)
        {
            foreach (Client client in nearby)
            {
                comps = client.key.Split('.');
                pos = new Vector3(int.Parse(comps[0]), int.Parse(comps[1]), int.Parse(comps[2]));
                pos += bounds.minBnd;
                cellGos[pos].GetComponent<MeshRenderer>().material.color = new Color(0,1,1,.5f);
            }
        }


        //search area
        comps = target.key.Split('.');
        pos = new Vector3(int.Parse(comps[0]), int.Parse(comps[1]), int.Parse(comps[2]));
        pos += bounds.minBnd;
        cellGos[pos].GetComponent<MeshRenderer>().material.color = new Color(0, 1, 0, .5f);
        cellGos[pos].SetActive(true);

        //Debug.DrawLine(bounds.minBnd, bounds.maxBnd);

    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawSphere(bounds.minBnd,.5f);
        Gizmos.DrawSphere(bounds.maxBnd,.5f);
        Gizmos.DrawWireCube(gridCenter, new Vector3(width, height, depth));
    }

    public void UpdateClient(Client _client, Vector3 velocity)
    {
        _client.MoveClient(bounds);
        removeClient(_client);
        insertClient(_client);
    }

    public Client NewClient(Vector3 _position, Vector3 _size)
    {
        var client = new Client()
        {
            position = _position,
            size = _size,
            velocity = Random.insideUnitSphere*2,
            indicies = null,
            key = "0.0.0"
        };
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.transform.position = gridCenter + _position;
        clients.Add(client);
        clientGos.Add(go);
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

        var i1 = getCellIndex(xPos - width / 2f, yPos - height / 2f, zPos - depth / 2f);
        var i2 = getCellIndex(xPos + width / 2f, yPos + height / 2f, zPos + depth / 2f);

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
    public HashSet<Client> FindNearby(Vector3 _position, Vector3 _searchArea)
    {
        float xPos, yPos, zPos, width, height, depth;
        xPos = _position.x;
        yPos = _position.y;
        zPos = _position.z;
        width = _searchArea.x;
        height = _searchArea.y;
        depth = _searchArea.z;

        var i1 = getCellIndex(xPos - width / 2f, yPos - height / 2f, zPos - depth / 2f);
        var i2 = getCellIndex(xPos + width / 2f, yPos + height / 2f, zPos + depth / 2f);

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
