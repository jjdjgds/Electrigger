using UnityEngine;

public class PowerNode : MonoBehaviour
{
    public bool isBattery;
    public PowerNode owner; // ‚±‚Ìplug‚ª‘®‚·‚éƒ‚ƒjƒ^[‚Ü‚½‚Íbattery
    private bool powered;

    public bool IsPowered() => powered;
    public void SetPowered(bool value) => powered = value;
}