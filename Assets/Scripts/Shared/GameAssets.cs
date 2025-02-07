using UnityEngine;

public class GameAssets : MonoBehaviour
{
    public const int UNITS_LAYER = 6;
    public const int PATHFINDING_WALLS_LAYER = 7;
    public static GameAssets Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }
}
