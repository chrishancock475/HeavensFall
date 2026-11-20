using UnityEngine;

/// <summary>
/// One room of the single-scene map. Turns its Cinemachine camera on while the player is inside
/// (same idea as CameraSelect.cs, but counts the player's two colliders so neighbours don't flicker).
/// </summary>
public class RoomZone : MonoBehaviour
{
    public static RoomZone Current { get; private set; }

    public string roomId = "R1";
    public string roomName = "Room";
    [TextArea] public string hint = "";
    public GameObject virtualCamera;

    int inside;

    void Awake()
    {
        if (virtualCamera != null) virtualCamera.SetActive(false);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (PlayerFilter.Party(other) == null) return;
        inside++;
        if (inside == 1)
        {
            // Re-enabling makes this the most recent camera, so Cinemachine blends to it.
            if (virtualCamera != null) { virtualCamera.SetActive(false); virtualCamera.SetActive(true); }
            Current = this;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (PlayerFilter.Party(other) == null) return;
        inside = Mathf.Max(0, inside - 1);
        if (inside == 0 && virtualCamera != null)
        {
            virtualCamera.SetActive(false);
            if (Current == this) Current = null;
        }
    }

    void OnDestroy()
    {
        if (Current == this) Current = null;
    }
}
