using UnityEngine;

public class Tile : MonoBehaviour
{
    public enum StructureType { None, Tower, Helipad, Radio, Mask, Detector }

    public HexCoordinates Coordinates { get; private set; }
    
    [Header("Resources")]
    public int oxygen = 3;
    public bool hasMine = false;
    public bool isMineRevealed = false; 

    [Header("Structure")]
    public StructureType structure = StructureType.None;
    public bool isCaptured = false;

    public enum FlagState { None, Safe, Danger }

    [Header("State")]
    public bool isRevealed = false;
    public bool isOccupied = false;
    public FlagState flagState = FlagState.None;

    [Header("Visual Settings")]
    public Color fullOxygenColor = new Color(0.6f, 0.4f, 0.2f); // Brownish
    public Color emptyOxygenColor = Color.gray;
    public Color fogColor = Color.black;
    public Color dangerFlagColor = new Color(1f, 0.2f, 0.2f); // Red
    public Color safeFlagColor = new Color(0.2f, 1f, 0.2f);   // Green
    
    // Structure Colors (Fallback if no prefab)
    public Color towerColor = Color.cyan;
    public Color helipadColor = Color.yellow;
    public Color radioColor = Color.magenta;
    public Color capturedColor = Color.blue;
    public Color maskColor = Color.green;     
    public Color detectorColor = new Color(1f, 0.5f, 0f); // Orange

    public void Initialize(HexCoordinates coords)
    {
        Coordinates = coords;
        gameObject.name = $"Tile {coords.ToString()}";
        isRevealed = false;
        structure = StructureType.None;
        UpdateVisuals();
    }

    public void SetStructure(StructureType type)
    {
        structure = type;
        UpdateVisuals();
    }

    public void Capture()
    {
        if (structure == StructureType.Tower && !isCaptured)
        {
            isCaptured = true;
            Debug.Log($"Tower at {Coordinates} Captured!");
            UpdateVisuals();
        }
    }

    public void ReduceOxygen()
    {
        if (oxygen > 0)
        {
            oxygen--;
            UpdateVisuals();
        }
    }

    public void ToggleFlag()
    {
        if (isRevealed)
        {
            // Cycle: None -> Safe -> Danger -> None
            if (flagState == FlagState.None) flagState = FlagState.Safe;
            else if (flagState == FlagState.Safe) flagState = FlagState.Danger;
            else flagState = FlagState.None;

            UpdateVisuals();
        }
    }

    private void UpdateVisuals()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            if (!isRevealed)
            {
                sr.color = fogColor;
            }
            else
            {
                // Flag logic on TOP of revealed tile
                if (flagState == FlagState.Safe)
                {
                    sr.color = safeFlagColor;
                    return;
                }
                else if (flagState == FlagState.Danger)
                {
                    sr.color = dangerFlagColor;
                    return;
                }

                // Special: Revealed Mine (by Detector)
                if (hasMine && isMineRevealed)
                {
                    sr.color = Color.red; 
                    return;
                }

                // Priority: Structure > Oxygen
                if (structure == StructureType.Tower)
                {
                    sr.color = isCaptured ? capturedColor : towerColor;
                }
                else if (structure == StructureType.Helipad)
                {
                    sr.color = helipadColor;
                }
                // HIDDEN ITEMS: Radio, Mask, Detector look like normal ground
                else
                {
                    float value = oxygen / 3f;
                    sr.color = Color.Lerp(emptyOxygenColor, fullOxygenColor, value);
                }
            }
        }
    }

    public void RevealMine()
    {
        if (hasMine)
        {
            isMineRevealed = true;
            UpdateVisuals();
        }
    }

    public void Reveal()
    {
        if (!isRevealed)
        {
            isRevealed = true;
            UpdateVisuals();
        }
    }
}