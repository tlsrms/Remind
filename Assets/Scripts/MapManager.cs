using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MapManager : MonoBehaviour
{
    public static MapManager Instance;

    public enum ActionMode { None, Move, Recon, Hold }

    [Header("Unit Settings")]
    public Unit playerUnit;

    [Header("Current Action Mode")]
    public ActionMode currentMode = ActionMode.None;

    private Dictionary<HexCoordinates, Tile> grid;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (MapGenerator.Instance != null)
        {
            grid = MapGenerator.Instance.Generate();
        }
        else
        {
            Debug.LogError("MapGenerator Instance not found!");
            return;
        }

        if (GameManager.Instance != null) GameManager.Instance.StartGame();
        InitializePlayer();
    }

    // Called by InputManager
    public void ProcessAction(Tile targetTile)
    {
        if (currentMode == ActionMode.None) return;
        if (playerUnit == null) return;

        // Common Logic: Calculate Distance
        int distance = HexCoordinates.Distance(playerUnit.Location, targetTile.Coordinates);
        int maxRange = 1; // Both Move and Recon are range 1

        if (distance > maxRange)
        {
            Debug.Log($"Invalid Action: Distance is {distance} (Max {maxRange})");
            return;
        }

        // Action Cost Logic (Pay Oxygen)
        Tile currentTile = GetTile(playerUnit.Location);
        bool punishHP = false;

        if (currentTile != null)
        {
            if (currentTile.oxygen <= 0) punishHP = true;
            currentTile.ReduceOxygen();
        }

        if (punishHP)
        {
            if (playerUnit.hasMask)
            {
                playerUnit.hasMask = false;
                Debug.Log("Oxygen Mask Used! Prevented HP loss.");
                // Refresh UI
                if (UIManager.Instance != null) UIManager.Instance.UpdateMineAlert(GetNeighborMineCount(playerUnit.Location));
            }
            else
            {
                playerUnit.TakeDamage(1);
            }
        }

        // Execute specific action
        switch (currentMode)
        {
            case ActionMode.Move:
                MovePlayerTo(targetTile.Coordinates);
                Debug.Log("Action Completed: Move");
                break;
            case ActionMode.Recon:
                PerformRecon(targetTile);
                break;
        }

        currentMode = ActionMode.None;
    }

    private void MovePlayerTo(HexCoordinates targetCoords)
    {
        Tile targetTile = GetTile(targetCoords);
        if (targetTile != null)
        {
            // Safety Check: Flagged Tile (Optional, currently allowed)
            // if (targetTile.isFlagged) return; 

            Tile currentTile = GetTile(playerUnit.Location);
            if(currentTile != null) currentTile.isOccupied = false;

            targetTile.isOccupied = true;
            
            Vector3 targetPos = targetTile.transform.position;
            playerUnit.MoveTo(targetCoords, targetPos);

            if (targetTile.hasMine)
            {
                TriggerMine(targetTile);
            }

            RevealSurroundings(targetCoords);
            CheckInteraction(targetTile);

            int nearbyMines = GetNeighborMineCount(targetCoords);
            if (UIManager.Instance != null) UIManager.Instance.UpdateMineAlert(nearbyMines);
        }
    }

    private void PerformRecon(Tile targetTile)
    {
        Debug.Log($"Recon success on tile {targetTile.Coordinates}");
        targetTile.Reveal();
        
        string result = "Safe Ground";
        if (targetTile.hasMine) result = "<color=red>DANGER: MINE DETECTED!</color>";
        else if (targetTile.structure == Tile.StructureType.Mask) result = "Supply: Oxygen Mask";
        else if (targetTile.structure == Tile.StructureType.Detector) result = "Supply: Mine Detector";
        else if (targetTile.structure == Tile.StructureType.Radio) result = "Objective: Radio";
        else if (targetTile.structure == Tile.StructureType.Helipad) result = "Objective: Helipad";
        else if (targetTile.structure == Tile.StructureType.Tower) result = "Objective: Tower";
        
        if (UIManager.Instance != null) UIManager.Instance.ShowReconInfo(result);
    }

    private void CheckInteraction(Tile tile)
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsGameActive) return;

        if (tile.structure == Tile.StructureType.Tower)
        {
            if (!tile.isCaptured)
            {
                tile.Capture();
                if (GameManager.Instance != null) GameManager.Instance.OnTowerCaptured();
            }
        }
        else if (tile.structure == Tile.StructureType.Radio)
        {
            if (!playerUnit.hasRadio)
            {
                playerUnit.hasRadio = true;
                Debug.Log("Item Acquired: Radio! Helipad location revealed.");
                tile.SetStructure(Tile.StructureType.None);
                
                if (MapGenerator.Instance != null)
                {
                    HexCoordinates helipadCoords = MapGenerator.Instance.HelipadLocation;
                    Tile helipadTile = GetTile(helipadCoords);
                    if (helipadTile != null)
                    {
                        helipadTile.Reveal();
                        Debug.Log($"Helipad is at {helipadCoords}");
                    }
                }
            }
        }
        else if (tile.structure == Tile.StructureType.Mask)
        {
            if (!playerUnit.hasMask)
            {
                playerUnit.hasMask = true;
                Debug.Log("Item Acquired: Oxygen Mask!");
                tile.SetStructure(Tile.StructureType.None);
            }
        }
        else if (tile.structure == Tile.StructureType.Detector)
        {
            Debug.Log("Item Used: Mine Detector! Revealing nearby mines (Range 2).");
            tile.SetStructure(Tile.StructureType.None);
            
            List<HexCoordinates> range2Tiles = GetTilesInRange(playerUnit.Location, 2);
            int minesFound = 0;
            foreach (var coords in range2Tiles)
            {
                Tile t = GetTile(coords);
                if (t != null && t.hasMine)
                {
                    t.RevealMine();
                    minesFound++;
                }
            }
            Debug.Log($"Detector found {minesFound} mines.");
        }
        else if (tile.structure == Tile.StructureType.Helipad)
        {
            if (GameManager.Instance != null) GameManager.Instance.OnHelipadReached(playerUnit.hasRadio);
        }
    }

    private void TriggerMine(Tile tile)
    {
        Debug.Log($"BOOM! Stepped on a mine at {tile.Coordinates}");
        
        if (UIManager.Instance != null && playerUnit != null)
        {
            UIManager.Instance.ShowDirectionAlert(playerUnit.transform.position, tile.transform.position, Color.red);
        }

        playerUnit.TakeDamage(1); 
        tile.hasMine = false; 
    }

    private void InitializePlayer()
    {
        if (playerUnit == null) return;

        HexCoordinates startCoords = new HexCoordinates(0, 0);
        Tile startTile = GetTile(startCoords);
        
        if (startTile != null)
        {
            Vector3 startPos = startTile.transform.position;
            playerUnit.Initialize(startCoords, startPos);
            startTile.isOccupied = true;
            
            RevealSurroundings(startCoords);
            
            int nearbyMines = GetNeighborMineCount(startCoords);
            if (UIManager.Instance != null) UIManager.Instance.UpdateMineAlert(nearbyMines);
        }
    }

    public void RevealAllMap()
    {
        Debug.Log("Debug: Revealing all tiles!");
        if (grid == null) return;
        foreach (var tile in grid.Values)
        {
            tile.Reveal();
        }
    }

    public void SelectMoveAction()
    {
        currentMode = ActionMode.Move;
        Debug.Log("Mode: Move - Select a tile to move.");
    }

    public void SelectReconAction()
    {
        currentMode = ActionMode.Recon;
        Debug.Log("Mode: Recon - Select a tile to scout.");
    }

    public void SelectHoldAction()
    {
        Debug.Log("Action: Hold - Staying put.");
        
        Tile currentTile = GetTile(playerUnit.Location);
        bool punishHP = false;

        if (currentTile != null)
        {
            if (currentTile.oxygen <= 0) punishHP = true;
            currentTile.ReduceOxygen();
        }

        if (punishHP)
        {
            if (playerUnit.hasMask)
            {
                playerUnit.hasMask = false;
                Debug.Log("Oxygen Mask Used!");
                if (UIManager.Instance != null) UIManager.Instance.UpdateMineAlert(GetNeighborMineCount(playerUnit.Location));
            }
            else
            {
                playerUnit.TakeDamage(1);
            }
        }
        currentMode = ActionMode.None;
    }

    // Helper Methods
    public Tile GetTile(HexCoordinates coords)
    {
        if (grid != null && grid.TryGetValue(coords, out Tile tile)) return tile;
        return null;
    }

    public int GetNeighborMineCount(HexCoordinates center)
    {
        int count = 0;
        foreach (Tile neighbor in GetNeighbors(center))
        {
            if (neighbor.hasMine) count++;
        }
        return count;
    }

    private List<Tile> GetNeighbors(HexCoordinates center)
    {
        List<Tile> neighbors = new List<Tile>();
        int[][] directions = new int[][] 
        { 
            new int[] {1, 0}, new int[] {1, -1}, new int[] {0, -1}, 
            new int[] {-1, 0}, new int[] {-1, 1}, new int[] {0, 1} 
        };

        foreach (var dir in directions)
        {
            HexCoordinates neighborCoords = new HexCoordinates(center.Q + dir[0], center.R + dir[1]);
            Tile tile = GetTile(neighborCoords);
            if (tile != null) neighbors.Add(tile);
        }
        return neighbors;
    }

    private void RevealSurroundings(HexCoordinates center)
    {
        Tile centerTile = GetTile(center);
        if (centerTile != null) centerTile.Reveal();

        foreach (var tile in GetNeighbors(center))
        {
            tile.Reveal();
        }
    }

    private List<HexCoordinates> GetTilesInRange(HexCoordinates center, int range)
    {
        List<HexCoordinates> results = new List<HexCoordinates>();
        for (int q = -range; q <= range; q++)
        {
            int r1 = Mathf.Max(-range, -q - range);
            int r2 = Mathf.Min(range, -q + range);
            for (int r = r1; r <= r2; r++)
            {
                results.Add(new HexCoordinates(center.Q + q, center.R + r));
            }
        }
        return results;
    }
}