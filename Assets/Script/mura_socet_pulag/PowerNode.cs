using UnityEngine;

public class PowerNode : MonoBehaviour
{
    public bool isBattery;
    public PowerNode owner;
    private bool powered;
    private bool lastPowered;
    private battery poweredBy;
    private int depth;

    public AngleType? connectedSide = null;
    public PowerNode connectedNode = null;
    [Header("Sound")]
    public AudioClip powerOnSE;
    private AudioSource audioSource;

    //モニター本体のMonitor_Dragを参照
    private Monitor_Drag monitorDrag;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        //ownerがあればそちら、なければ自身から取得
        monitorDrag = owner != null
            ? owner.GetComponent<Monitor_Drag>()
            : GetComponent<Monitor_Drag>();
    }

    public bool IsPowered() => powered;
    public void SetPowered(bool value) => powered = value;
    public battery GetPoweredBy() => poweredBy;
    public void SetPoweredBy(battery bat) => poweredBy = bat;
    public int GetDepth() => depth;
    public void SetDepth(int d) => depth = d;

    public void NotifyPoweredResult()
    {
        if (!isBattery && !lastPowered && powered)
        {
            // モニターの場合はMonitor_Drag側で状態を見て鳴らすのでここでは鳴らさない
            if (monitorDrag == null)
            {
                PlayPowerOnSE();
            }
        }
        lastPowered = powered;
    }

    public void PlayPowerOnSE()
    {
        if (powerOnSE != null && audioSource != null)
        {
            audioSource.PlayOneShot(powerOnSE);
        }
    }
}