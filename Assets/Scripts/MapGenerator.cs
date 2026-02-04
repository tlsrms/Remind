using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public static MapGenerator Instance;

    [Header("Map Settings")]
    public int mapRadius = 5;
    public float hexSize = 1f;
    [Range(0.8f, 2.0f)]
    public float spacing = 1.0f;
    [Range(0, 100)]
    public int mineCount = 20;
    [Range(0, 10)]
    public int maskCount = 2;
    [Range(0, 10)]
    public int detectorCount = 2;
    public GameObject tilePrefab;

    [Header("Objectives Prefabs")]
    public GameObject towerPrefab;
    public GameObject helipadPrefab;
    public GameObject radioPrefab;
    public GameObject maskPrefab;
    public GameObject detectorPrefab;

    private Dictionary<HexCoordinates, Tile> grid = new Dictionary<HexCoordinates, Tile>();
    private List<HexCoordinates> objectiveLocations = new List<HexCoordinates>();
    public HexCoordinates HelipadLocation { get; private set; }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public Dictionary<HexCoordinates, Tile> Generate()
    {
        GenerateGrid();
        GenerateObjectives();
        GenerateMinesAdvanced();
        return grid;
    }

    public Tile GetTile(HexCoordinates coords)
    {
        if (grid.TryGetValue(coords, out Tile tile)) return tile;
        return null;
    }

    public Dictionary<HexCoordinates, Tile> GetGrid()
    {
        return grid;
    }

    private void GenerateGrid()
    {
        foreach (var tile in grid.Values)
        {
            if (tile != null) Destroy(tile.gameObject);
        }
        grid.Clear();

        for (int q = -mapRadius; q <= mapRadius; q++)
        {
            int r1 = Mathf.Max(-mapRadius, -q - mapRadius);
            int r2 = Mathf.Min(mapRadius, -q + mapRadius);
            for (int r = r1; r <= r2; r++)
            {
                CreateTile(q, r);
            }
        }
    }

    private void CreateTile(int q, int r)
    {
        HexCoordinates coords = new HexCoordinates(q, r);
        Vector3 position = coords.ToPosition(hexSize * spacing);
        
        GameObject tileObj = Instantiate(tilePrefab, position, Quaternion.identity, transform);
        Tile tile = tileObj.GetComponent<Tile>();
        
        if (tile != null)
        {
            tile.Initialize(coords);
            grid.Add(coords, tile);
        }
    }

    private void GenerateObjectives()
    {
        objectiveLocations.Clear();
        List<HexCoordinates> candidates = new List<HexCoordinates>(grid.Keys);
        
        HexCoordinates start = new HexCoordinates(0, 0);
        candidates = candidates.Where(c => HexCoordinates.Distance(c, start) >= 3).ToList();
        
        // 1. Helipad (1)
        if (candidates.Count > 0) SpawnSingleObjective(candidates, Tile.StructureType.Helipad, helipadPrefab, true);

        // 2. Radio (1)
        if (candidates.Count > 0) SpawnSingleObjective(candidates, Tile.StructureType.Radio, radioPrefab);

        // 3. Masks
        for (int i = 0; i < maskCount && candidates.Count > 0; i++)
        {
            SpawnSingleObjective(candidates, Tile.StructureType.Mask, maskPrefab);
        }

        // 4. Detectors
        for (int i = 0; i < detectorCount && candidates.Count > 0; i++)
        {
            SpawnSingleObjective(candidates, Tile.StructureType.Detector, detectorPrefab);
        }

        // 5. Towers
        for (int i = 0; i < 3 && candidates.Count > 0; i++)
        {
            SpawnSingleObjective(candidates, Tile.StructureType.Tower, towerPrefab);
        }
    }

    private void SpawnSingleObjective(List<HexCoordinates> candidates, Tile.StructureType type, GameObject prefab, bool isHelipad = false)
    {
        int idx = UnityEngine.Random.Range(0, candidates.Count);
        HexCoordinates targetCoord = candidates[idx];
        objectiveLocations.Add(targetCoord);
        
        Tile targetTile = grid[targetCoord];
        targetTile.SetStructure(type);
        SpawnObjectivePrefab(targetTile, prefab);

        if (isHelipad) HelipadLocation = targetCoord;

        candidates.RemoveAt(idx);
    }

    private void SpawnObjectivePrefab(Tile tile, GameObject prefab)
    {
        if (prefab != null)
        {
            Instantiate(prefab, tile.transform.position, Quaternion.identity, tile.transform);
        }
    }

    private void GenerateMinesAdvanced()
    {
        int maxRetries = 10;
        int attempt = 0;

        while (attempt < maxRetries)
        {
            attempt++;
            if (TryPlaceMines()) return;
            
            foreach (var tile in grid.Values) tile.hasMine = false;
        }
        Debug.LogError("Failed to generate valid mine map.");
    }

    private bool TryPlaceMines()
    {
        HashSet<HexCoordinates> safeZone = new HashSet<HexCoordinates>();
        HexCoordinates start = new HexCoordinates(0, 0);

        safeZone.Add(start);

        List<Tile> startNeighbors = GetNeighbors(start);
        if (startNeighbors.Count > 0)
        {
            Tile randomSafeNeighbor = startNeighbors[UnityEngine.Random.Range(0, startNeighbors.Count)];
            safeZone.Add(randomSafeNeighbor.Coordinates);
        }

        foreach (var obj in objectiveLocations) safeZone.Add(obj);

        foreach (var obj in objectiveLocations)
        {
            var path = FindPath(start, obj);
            if (path != null)
            {
                foreach (var step in path) safeZone.Add(step);
            }
        }

        List<HexCoordinates> availableTiles = new List<HexCoordinates>();
        foreach (var kvp in grid)
        {
            if (!safeZone.Contains(kvp.Key)) availableTiles.Add(kvp.Key);
        }

        int placed = 0;
        while (placed < mineCount && availableTiles.Count > 0)
        {
            int idx = UnityEngine.Random.Range(0, availableTiles.Count);
            HexCoordinates target = availableTiles[idx];
            grid[target].hasMine = true;
            availableTiles.RemoveAt(idx);
            placed++;
        }

        EnsureMapConnectivity(start);
        FixEncircledTiles();

        return true;
    }

    private void EnsureMapConnectivity(HexCoordinates start)
    {
        while (true)
        {
            HashSet<HexCoordinates> reachable = GetReachableTiles(start);
            HexCoordinates? isolatedTile = null;
            
            foreach (var kvp in grid)
            {
                if (!kvp.Value.hasMine && !reachable.Contains(kvp.Key))
                {
                    isolatedTile = kvp.Key;
                    break; 
                }
            }

            if (isolatedTile == null) break;

            List<HexCoordinates> bridgePath = FindPathThroughMines(isolatedTile.Value, reachable);
            if (bridgePath != null)
            {
                foreach (var step in bridgePath)
                {
                    if (grid.ContainsKey(step) && grid[step].hasMine) grid[step].hasMine = false;
                }
            }
            else break;
        }
    }

    private HashSet<HexCoordinates> GetReachableTiles(HexCoordinates start)
    {
        HashSet<HexCoordinates> visited = new HashSet<HexCoordinates>();
        Queue<HexCoordinates> frontier = new Queue<HexCoordinates>();

        if (grid.ContainsKey(start) && !grid[start].hasMine)
        {
            frontier.Enqueue(start);
            visited.Add(start);
        }

        while (frontier.Count > 0)
        {
            HexCoordinates current = frontier.Dequeue();
            foreach (Tile neighbor in GetNeighbors(current))
            {
                if (!neighbor.hasMine && !visited.Contains(neighbor.Coordinates))
                {
                    visited.Add(neighbor.Coordinates);
                    frontier.Enqueue(neighbor.Coordinates);
                }
            }
        }
        return visited;
    }

    private List<HexCoordinates> FindPathThroughMines(HexCoordinates start, HashSet<HexCoordinates> targets)
    {
        Queue<HexCoordinates> frontier = new Queue<HexCoordinates>();
        Dictionary<HexCoordinates, HexCoordinates> cameFrom = new Dictionary<HexCoordinates, HexCoordinates>();
        HashSet<HexCoordinates> visited = new HashSet<HexCoordinates>();

        frontier.Enqueue(start);
        visited.Add(start);
        cameFrom[start] = start; 

        while (frontier.Count > 0)
        {
            HexCoordinates current = frontier.Dequeue();
            if (targets.Contains(current)) return ReconstructPath(cameFrom, start, current);

            foreach (Tile neighbor in GetNeighbors(current))
            {
                if (!visited.Contains(neighbor.Coordinates))
                {
                    visited.Add(neighbor.Coordinates);
                    frontier.Enqueue(neighbor.Coordinates);
                    cameFrom[neighbor.Coordinates] = current;
                }
            }
        }
        return null;
    }

    private void FixEncircledTiles()
    {
        foreach (var kvp in grid)
        {
            Tile tile = kvp.Value;
            if (tile.hasMine) continue;

            List<Tile> neighbors = GetNeighbors(tile.Coordinates);
            if (neighbors.Count == 0) continue;

            bool allMines = true;
            foreach (var n in neighbors)
            {
                if (!n.hasMine)
                {
                    allMines = false;
                    break;
                }
            }

            if (allMines)
            {
                Tile rescueTile = neighbors[UnityEngine.Random.Range(0, neighbors.Count)];
                rescueTile.hasMine = false;
            }
        }
    }

    private List<HexCoordinates> FindPath(HexCoordinates start, HexCoordinates end)
    {
        Queue<HexCoordinates> frontier = new Queue<HexCoordinates>();
        Dictionary<HexCoordinates, HexCoordinates> cameFrom = new Dictionary<HexCoordinates, HexCoordinates>();
        
        frontier.Enqueue(start);
        cameFrom[start] = start;

        while (frontier.Count > 0)
        {
            HexCoordinates current = frontier.Dequeue();
            if (current.Equals(end)) return ReconstructPath(cameFrom, start, end);

            foreach (Tile neighbor in GetNeighbors(current))
            {
                if (!cameFrom.ContainsKey(neighbor.Coordinates))
                {
                    frontier.Enqueue(neighbor.Coordinates);
                    cameFrom[neighbor.Coordinates] = current;
                }
            }
        }
        return null;
    }

    private List<HexCoordinates> ReconstructPath(Dictionary<HexCoordinates, HexCoordinates> cameFrom, HexCoordinates start, HexCoordinates end)
    {
        List<HexCoordinates> path = new List<HexCoordinates>();
        HexCoordinates current = end;
        while (!current.Equals(start))
        {
            path.Add(current);
            current = cameFrom[current];
        }
        path.Add(start);
        path.Reverse();
        return path;
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
            if(grid.TryGetValue(neighborCoords, out Tile tile))
            {
                neighbors.Add(tile);
            }
        }
        return neighbors;
    }    private void OnDrawGizmos()
    {
        if (grid == null || grid.Count == 0) return;

        foreach (var kvp in grid)
        {
            Tile tile = kvp.Value;
            Vector3 center = tile.transform.position;

            // 1. Mines (Red Sphere)
            if (tile.hasMine)
            {
                Gizmos.color = new Color(1f, 0f, 0f, 0.5f); // Semi-transparent Red
                Gizmos.DrawSphere(center, hexSize * 0.4f);
            }

            // 2. Structures
            if (tile.structure == Tile.StructureType.Tower)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawCube(center, Vector3.one * hexSize * 0.5f);
            }
            else if (tile.structure == Tile.StructureType.Helipad)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawCube(center, Vector3.one * hexSize * 0.5f);
            }
            else if (tile.structure == Tile.StructureType.Radio)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawSphere(center, hexSize * 0.3f);
            }
            else if (tile.structure == Tile.StructureType.Mask)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(center, hexSize * 0.2f);
            }
            else if (tile.structure == Tile.StructureType.Detector)
            {
                Gizmos.color = new Color(1f, 0.5f, 0f); // Orange
                Gizmos.DrawSphere(center, hexSize * 0.2f);
            }
        }
    }
}
