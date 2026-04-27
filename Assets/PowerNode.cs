using UnityEngine;

public class PowerNode : MonoBehaviour
{
    public bool isBattery;
    public PowerNode owner;
    private bool powered;
    private battery poweredBy; // ‚Ç‚Ìbattery‚©‚ç“d—Í‚ð‚à‚ç‚Á‚Ä‚¢‚é‚©

    public bool IsPowered() => powered;
    public void SetPowered(bool value) => powered = value;

    public battery GetPoweredBy() => poweredBy;
    public void SetPoweredBy(battery bat) => poweredBy = bat;
}