using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    void Update()
    {
        // Game State Check
        if (GameManager.Instance != null && !GameManager.Instance.IsGameActive) return;

        // Key Inputs
        if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
        {
            if (MapManager.Instance != null) MapManager.Instance.RevealAllMap();
        }

        // Mouse Inputs
        if (Mouse.current != null)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                HandleLeftClick();
            }
            else if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                HandleRightClick();
            }
        }
    }

    private void HandleLeftClick()
    {
        Tile tile = RaycastTile();
        if (tile != null && MapManager.Instance != null)
        {
            MapManager.Instance.ProcessAction(tile);
        }
    }

    private void HandleRightClick()
    {
        Tile tile = RaycastTile();
        if (tile != null)
        {
            tile.ToggleFlag();
        }
    }

    private Tile RaycastTile()
    {
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
        Vector2 mousePos2D = new Vector2(mouseWorldPos.x, mouseWorldPos.y);
        
        RaycastHit2D hit = Physics2D.Raycast(mousePos2D, Vector2.zero);
        if (hit.collider != null)
        {
            return hit.collider.GetComponent<Tile>();
        }
        return null;
    }
}
