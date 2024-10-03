using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

[System.Serializable]
public class LevelData
{
    [TextArea(5, 15)]
    public string layout;
}

public class SokobanManager : MonoBehaviour
{
    public Tilemap floorTilemap;
    public Tilemap wallTilemap;
    
    public TileBase floorTile;
    public TileBase wallTile;

    public GameObject playerPrefab;
    public GameObject boxPrefab;

    public LevelData[] levels;

    private GameObject playerObject;
    private Vector3Int playerPosition;
    private Dictionary<Vector3Int, GameObject> boxes = new Dictionary<Vector3Int, GameObject>();

    private int currentLevel = 0;

    void Start()
    {
        LoadLevel(currentLevel);
    }

    void LoadLevel(int levelIndex)
    {
        if (levelIndex < 0 || levelIndex >= levels.Length)
        {
            Debug.LogError("Invalid level index!");
            return;
        }

        ClearLevel();

        LevelData level = levels[levelIndex];
        string[] rows = level.layout.Split('\n');
        int height = rows.Length;
        int width = rows[0].Length;

        for (int y = 0; y < height; y++)
        {
            string row = rows[y].Trim(); // Remove any leading/trailing whitespace
            for (int x = 0; x < width; x++)
            {
                if (x >= row.Length) continue; // Skip if the row is shorter than expected

                Vector3Int pos = new Vector3Int(x, height - 1 - y, 0);
                char tile = row[x];

                switch (tile)
                {
                    case '#':
                        wallTilemap.SetTile(pos, wallTile);
                        break;
                    case '.':
                    case '@':
                    case '$':
                    case ' ':
                        floorTilemap.SetTile(pos, floorTile);
                        break;
                }

                if (tile == '@')
                {
                    playerPosition = pos;
                }
                else if (tile == '$')
                {
                    PlaceBox(pos);
                }
            }
        }

        playerObject = Instantiate(playerPrefab, GetWorldPosition(playerPosition), Quaternion.identity);
    }

    void ClearLevel()
    {
        floorTilemap.ClearAllTiles();
        wallTilemap.ClearAllTiles();

        foreach (var box in boxes.Values)
        {
            Destroy(box);
        }
        boxes.Clear();

        if (playerObject != null)
        {
            Destroy(playerObject);
        }
    }

    Vector3 GetWorldPosition(Vector3Int gridPosition)
    {
        return floorTilemap.CellToWorld(gridPosition) + floorTilemap.cellSize / 2;
    }

    void PlaceBox(Vector3Int position)
    {
        Vector3 worldPosition = GetWorldPosition(position);
        GameObject boxObject = Instantiate(boxPrefab, worldPosition, Quaternion.identity);
        boxes[position] = boxObject;
    }

    void Update()
    {
        HandleInput();
    }

    void HandleInput()
    {
        Vector3Int movement = Vector3Int.zero;

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            movement = Vector3Int.up;
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            movement = Vector3Int.down;
        else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            movement = Vector3Int.left;
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            movement = Vector3Int.right;
        else if (Input.GetKeyDown(KeyCode.R))
            LoadLevel(currentLevel); // Reload current level
        else if (Input.GetKeyDown(KeyCode.N))
            LoadNextLevel();

        if (movement != Vector3Int.zero)
        {
            TryMove(movement);
        }
    }

    void LoadNextLevel()
    {
        currentLevel++;
        if (currentLevel >= levels.Length)
        {
            Debug.Log("All levels completed!");
            currentLevel = 0;
        }
        LoadLevel(currentLevel);
    }

    void TryMove(Vector3Int direction)
    {
        Vector3Int newPosition = playerPosition + direction;

        if (wallTilemap.GetTile(newPosition) == null) // Not a wall
        {
            if (boxes.TryGetValue(newPosition, out GameObject boxObject))
            {
                Vector3Int pushPosition = newPosition + direction;
                if (wallTilemap.GetTile(pushPosition) == null && !boxes.ContainsKey(pushPosition))
                {
                    // Move the box
                    boxes.Remove(newPosition);
                    boxes[pushPosition] = boxObject;
                    boxObject.transform.position = GetWorldPosition(pushPosition);

                    // Move the player
                    MovePlayer(newPosition);
                }
            }
            else
            {
                // Move the player
                MovePlayer(newPosition);
            }
        }
    }

    void MovePlayer(Vector3Int newPosition)
    {
        playerPosition = newPosition;
        playerObject.transform.position = GetWorldPosition(playerPosition);
    }
}