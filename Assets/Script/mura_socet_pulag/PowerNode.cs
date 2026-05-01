using UnityEngine;

public class PowerNode : MonoBehaviour
{
    public bool isBattery;
    public PowerNode owner;
    private bool powered;
    private battery poweredBy;
    private int depth;

    public AngleType? connectedSide = null;
    public PowerNode connectedNode = null;

    public bool IsPowered() => powered;
    public void SetPowered(bool value) => powered = value;

    public battery GetPoweredBy() => poweredBy;
    public void SetPoweredBy(battery bat) => poweredBy = bat;

    public int GetDepth() => depth;
    public void SetDepth(int d) => depth = d;
}