using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Linq;

[System.Serializable]
public class LevelData
{
    [TextArea(5, 15)]
    public string layout;
}


public class SokobanManager : MonoBehaviour
{
    private SpriteRenderer playerSpriteRenderer;

    public Tilemap floorTilemap;
    public Tilemap wallTilemap;
    
    public TileBase floorTile;
    public TileBase wallTile;
    public TileBase goalTile;  // New variable for goal tile

    private HashSet<Vector3Int> goalPositions = new HashSet<Vector3Int>();  // To store goal positions

    public GameObject playerPrefab;
    public GameObject boxPrefab;

    public LevelData[] levels;

    private GameObject playerObject;
    private Vector3Int playerPosition;
    private Dictionary<Vector3Int, GameObject> boxes = new Dictionary<Vector3Int, GameObject>();

    private Animator playerAnimator;
    private int currentLevel = 0;
    private float moveSpeed = 5f;  // Speed of movement

    private bool isMoving = false;
    private float moveTimer = 0f;
    private float moveDelay = 0.1f;


    public Canvas titleScreenCanvas;
    public Button playButton;
    public Canvas endScreenCanvas;
    public Button restartButton;

    private enum GameState
    {
        TitleScreen,
        Playing,
        EndScreen
    }    
    private GameState currentState = GameState.TitleScreen;
    void Start()
    {
        SetupTitleScreen();
        SetupEndScreen();
    }

    void SetupTitleScreen()
    {

        if (playButton != null)
        {
            playButton.onClick.AddListener(StartGame);
        }

        ShowTitleScreen(true);
    }

    void SetupEndScreen()
    {

        if (restartButton != null)
        {
            restartButton.onClick.AddListener(RestartGame);
        }

        ShowEndScreen(false);
    }

        void StartGame()
    {
        currentState = GameState.Playing;
        ShowTitleScreen(false);
        ShowEndScreen(false);
        currentLevel = 0;
        LoadLevel(currentLevel);
        playerSpriteRenderer = playerObject.GetComponent<SpriteRenderer>();
    }

    void RestartGame()
    {
        StartGame();
    }

    void ShowTitleScreen(bool show)
    {
        if(show){
            currentState = GameState.EndScreen;
        }
        if (titleScreenCanvas != null)
        {
            titleScreenCanvas.gameObject.SetActive(show);
        }
    }

    void ShowEndScreen(bool show)
    {   
        if(show){
            currentState = GameState.EndScreen;
        }
        if (endScreenCanvas != null)
        {
            endScreenCanvas.gameObject.SetActive(show);
        }
    }

    void Update()
    {
        if (currentState == GameState.Playing)
        {
            HandleInput();
        }
    }


    void LoadLevel(int levelIndex)
    {
        ClearLevel();

        LevelData level = levels[levelIndex];
        string[] rows = level.layout.Split('\n');
        int height = rows.Length;
        int width = rows[0].Length;

        for (int y = 0; y < height; y++)
        {
            // string row = rows[y].Trim();
            string row = rows[y];
            for (int x = 0; x < width; x++)
            {
                if (x >= row.Length) continue;

                Vector3Int pos = new Vector3Int(x, height - 1 - y, 0);
                char tile = row[x];

                switch (tile)
                {
                    case '#':
                        wallTilemap.SetTile(pos, wallTile);
                        break;
                    case '.': // normal tile
                        floorTilemap.SetTile(pos, floorTile);
                        break;
                    case '@':
                    case '+':  // Player on goal
                        floorTilemap.SetTile(pos, floorTile);
                        if (tile == '+')
                        {
                            floorTilemap.SetTile(pos, goalTile);
                            goalPositions.Add(pos);
                        }
                        playerPosition = pos;
                        break;
                    case '$':
                    case '*':  // Box on goal
                        floorTilemap.SetTile(pos, floorTile);
                        if (tile == '*')
                        {
                            floorTilemap.SetTile(pos, goalTile);
                            goalPositions.Add(pos);
                        }
                        PlaceBox(pos);
                        break;
                    case '~':  // Goal tile
                        floorTilemap.SetTile(pos, floorTile);
                        floorTilemap.SetTile(pos, goalTile);  // Set goal tile
                        goalPositions.Add(pos);  // Add to goal positions
                        break;
                    case ' ':  // Empty space
                        break;
                }
            }
        }

        playerObject = Instantiate(playerPrefab, GetWorldPosition(playerPosition), Quaternion.identity);
        playerAnimator = playerObject.GetComponent<Animator>();
        
        if (playerAnimator == null)
        {
            Debug.LogError("Animator component not found on player prefab!");
        }
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
        goalPositions.Clear();  // Clear goal positions


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


    void HandleInput()
    {
        Vector3Int movement = Vector3Int.zero;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            movement = Vector3Int.up;
        else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            movement = Vector3Int.down;
        else if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) {
            movement = Vector3Int.left;
            FlipSprite(true);
        }
        else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) {
            movement = Vector3Int.right;
            FlipSprite(false);
        }

        if (Input.GetKeyDown(KeyCode.R))
            LoadLevel(currentLevel); // Reload current level
        else if (Input.GetKeyDown(KeyCode.N))
            LoadNextLevel();

        if (movement != Vector3Int.zero)
        {
            moveTimer += Time.deltaTime;
            if (moveTimer >= moveDelay)
            {
                TryMove(movement);
                moveTimer = 0f;
            }
        }
        else
        {
            moveTimer = moveDelay; // Reset timer when no key is pressed
        }
    }

    void LoadNextLevel()
    {
        if (currentLevel >= levels.Length)
        {
            ClearLevel();
            ShowEndScreen(true);
            currentLevel = 0;
        } else {
            currentLevel++;
            LoadLevel(currentLevel);
        }
    }

    void TryMove(Vector3Int direction)
    {
        if (isMoving) return;

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
                    if(!CheckWinCondition()){
                        // Move the player
                        StartCoroutine(MoveObject(boxObject, pushPosition));
                        StartCoroutine(MovePlayer(newPosition));
                    }
                }
            }
            else
            {
                if(!CheckWinCondition()){
                  StartCoroutine(MovePlayer(newPosition));
                }
            }
        }


    }

    IEnumerator<Coroutine> MovePlayer(Vector3Int newPosition)
    {
        isMoving = true;
        Vector3 startPosition = playerObject.transform.position;
        Vector3 endPosition = GetWorldPosition(newPosition);
        float journeyLength = Vector3.Distance(startPosition, endPosition);
        float startTime = Time.time;

        UpdatePlayerAnimation(newPosition - playerPosition);

        while (playerObject.transform.position != endPosition)
        {
            float distanceCovered = (Time.time - startTime) * moveSpeed;
            float fractionOfJourney = distanceCovered / journeyLength;
            playerObject.transform.position = Vector3.Lerp(startPosition, endPosition, fractionOfJourney);
            yield return null;
        }
        playerPosition = newPosition;
        isMoving = false;
        UpdatePlayerAnimation(Vector3Int.zero);
    }

    IEnumerator<Coroutine> MoveObject(GameObject obj, Vector3Int newPosition)
    {
        Vector3 startPosition = obj.transform.position;
        Vector3 endPosition = GetWorldPosition(newPosition);
        float journeyLength = Vector3.Distance(startPosition, endPosition);
        float startTime = Time.time;

        while (obj.transform.position != endPosition)
        {
            float distanceCovered = (Time.time - startTime) * moveSpeed;
            float fractionOfJourney = distanceCovered / journeyLength;
            obj.transform.position = Vector3.Lerp(startPosition, endPosition, fractionOfJourney);
            yield return null;
        }
    }

    void FlipSprite(bool flipLeft)
    {
        if (playerSpriteRenderer != null)
        {
            playerSpriteRenderer.flipX = flipLeft;
        }
    }

    void UpdatePlayerAnimation(Vector3Int movement)
    {
        if (playerAnimator != null && playerAnimator.runtimeAnimatorController != null)
        {
            if (movement != Vector3Int.zero)
            {
                playerAnimator.SetBool("IsMoving", true);
                playerAnimator.SetFloat("Horizontal", movement.x);
                playerAnimator.SetFloat("Vertical", movement.y);

                if(movement.x < 0)
                {
                    FlipSprite(true);
                }
                else if (movement.x > 0)
                {
                    FlipSprite(false);
                }
            }
            else
            {
                playerAnimator.SetBool("IsMoving", false);
                playerAnimator.SetFloat("Horizontal", 0);
                playerAnimator.SetFloat("Vertical", 0);
            }
        }
    }

    bool CheckWinCondition()
    {
        bool allBoxesOnGoals = boxes.Keys.All(boxPos => goalPositions.Contains(boxPos));
        
        if (allBoxesOnGoals)
        {
            if(currentLevel >= levels.Length - 1)
            {
                ClearLevel();
                ShowEndScreen(true);
                currentLevel = 0;
                return true;
            }
            LoadNextLevel();
            return true;
        }
        return false;
    }

}