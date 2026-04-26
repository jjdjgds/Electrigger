using UnityEngine;

public class battery : MonoBehaviour
{
    [SerializeField] public GameObject plugPrefab;
    [SerializeField] private PlugColor plugColor;
    [SerializeField] private float angle;
    [SerializeField] private GameObject spawnPosition;
    [SerializeField] public int maxCharge = 3;
    [SerializeField] private int _currentCharge;
    public int currentCharge => _currentCharge;

    void Start()
    {
        _currentCharge = maxCharge;
        Debug.Log($"[Battery] {_currentCharge}/{maxCharge}");
        //Createplug();
    }

    // ConnectionManager‚©‚çŒÄ‚Î‚ê‚é
    public void SetCharge(int value)
    {
        _currentCharge = Mathf.Clamp(value, 0, maxCharge);
        Debug.Log($"[Battery] {_currentCharge}/{maxCharge}");
    }

    void Createplug()
    {
        if (spawnPosition == null || plugPrefab == null) return;
        Quaternion rotation = Quaternion.Euler(0, 0, angle);
        GameObject obj = Instantiate(plugPrefab, spawnPosition.transform.position, rotation, this.transform);
        plugColor c = obj.GetComponent<plugColor>();
       
        if (c != null) c.SetColor(plugColor);
    }
}