using UnityEngine;

public class plugCollision : MonoBehaviour
{
    private plugColor myPlugColor;

    void Awake()
    {
       // Debug.Log("同じ色のコンセントに当たった！");

        myPlugColor = GetComponent<plugColor>();
        if (myPlugColor == null)
            Debug.LogWarning("plugColorコンポーネントが見つかりません");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("electricaloutlet")) return;

        plugColor otherPlugColor = other.GetComponent<plugColor>();
        if (otherPlugColor == null) return;
        
        if (myPlugColor.GetPlugColor() == otherPlugColor.GetPlugColor())
        {
            Debug.Log("同じ色のコンセントに当たった！");
            // ここに処理を書く
        }
    }
}
