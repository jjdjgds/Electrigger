using UnityEngine;

[CreateAssetMenu(fileName = "TilescriptObj", menuName = "TilescriptObj")]
public class TilescriptObj : ScriptableObject
{
    public const int width = 17;
    public const int height = 17;
    public bool[] tiles = new bool[289];
}
