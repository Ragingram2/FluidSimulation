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
    public GameObject gameObject;
    public string key;

    public void MoveClient((Vector3 minBnd, Vector3 maxBnd) bounds)
    {
        Vector3 tempPosition = position + (velocity * Time.deltaTime);
        if (tempPosition.x < bounds.minBnd.x || tempPosition.x > bounds.maxBnd.x)
        {
            velocity.x *= -1;
        }
        if (tempPosition.y < bounds.minBnd.y || tempPosition.y > bounds.maxBnd.y)
        {
            velocity.y *= -1;
        }
        if (tempPosition.z < bounds.minBnd.z || tempPosition.z > bounds.maxBnd.z)
        {
            velocity.z *= -1;
        }
        position += velocity * Time.deltaTime;
        gameObject.transform.position = position;
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
    [SerializeField]
    private int particleCount = 25;
    Vector3 cellSize;
    private float width;
    private float height;
    private float depth;
    private Dictionary<string, HashSet<Client>> cells = new Dictionary<string, HashSet<Client>>();
    private Client[] clients;
    private Client target;
    private HashSet<Client> nearby;

    private void Start()
    {
        clients = new Client[particleCount];
        bounds = (-Vector3.one, Vector3.one );
        bounds.minBnd.x *= dimensions.x/2;
        bounds.minBnd.y *= dimensions.y/2;
        bounds.minBnd.z *= dimensions.z/2;
        bounds.minBnd += gridCenter;

        bounds.maxBnd.x *= dimensions.x/2;
        bounds.maxBnd.y *= dimensions.y/2;
        bounds.maxBnd.z *= dimensions.z/2;
        bounds.maxBnd += gridCenter;

        width = bounds.maxBnd.x - bounds.minBnd.x;
        height = bounds.maxBnd.y - bounds.minBnd.y;
        depth = bounds.maxBnd.z - bounds.minBnd.z;
        Debug.Log(width);
        Debug.Log(height);
        Debug.Log(depth);
        cellSize = new Vector3(width / dimensions.x, height / dimensions.y, depth / dimensions.z);

        Vector3 randPos;
        for (int i = 0; i < particleCount; i++)
        {
            randPos = Random.insideUnitSphere;
            randPos.x *= bounds.maxBnd.x;
            randPos.y *= bounds.maxBnd.y;
            randPos.z *= bounds.maxBnd.z;
            var client = NewClient(i, randPos, Vector3.one);
            if (i == 0)
            {
                target = client;
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
            UpdateClient(client);
        }
        nearby = FindNearby(target.position, searchSize);
    }

    private void OnDrawGizmos()
    {
        float totalCellCount = dimensions.x * dimensions.y * dimensions.z;
        int x = 0;
        int y = 0;
        int z = 0;
        Gizmos.DrawSphere(bounds.minBnd, .5f);
        Gizmos.DrawSphere(bounds.maxBnd, .5f);
        for (int i = 0; i < totalCellCount - 1; i++)
        {
            if (x > 0 &&  x % dimensions.x == 0)
            {
                x = 0;
                y++;
                if (y % dimensions.y == 0)
                {
                    y = 0;
                    z++;
                }
            }
            
            Gizmos.DrawWireCube(gridCenter + new Vector3(x * cellSize.x, y * cellSize.y, z * cellSize.z) + bounds.minBnd+new Vector3(.5f,.5f,.5f), cellSize);
            x++;
        }
        Gizmos.DrawWireCube(gridCenter, cellSize);
    }

    public void UpdateClient(Client _client)
    {
        _client.MoveClient(bounds);
        removeClient(_client);
        insertClient(_client);
    }

    public Client NewClient(int index, Vector3 _position, Vector3 _size)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.transform.position = gridCenter + _position;
        var client = new Client()
        {
            position = _position,
            size = _size,
            velocity = Random.insideUnitSphere * 2,
            gameObject = go,
            indicies = null,
            key = "0.0.0"
        };
        clients[index] = client;
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
