using UnityEngine;
using System.Collections;

public class socketCollision : MonoBehaviour
{
    //private bool isRechecking = false;
    private Quaternion lastRotation;

    void Awake()
    {
        lastRotation = transform.rotation;
    }

    //void Update()
    //{
    //    if (Quaternion.Angle(transform.rotation, lastRotation) > 0.1f)
    //    {
    //        lastRotation = transform.rotation;
    //        StartCoroutine(RecheckAfterFrame());
    //    }
    //}

    //IEnumerator RecheckAfterFrame()
    //{
    //    yield return null;
    //    RecheckConnections();
    //}

    //public void RecheckConnections()
    //{
    //    if (isRechecking) return;
    //    isRechecking = true;

    //    Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 0.3f);
    //    foreach (Collider2D hit in hits)
    //    {
    //        plugCollision plug = hit.GetComponent<plugCollision>();
    //        if (plug != null)
    //            plug.RecheckConnections();
    //    }

    //    isRechecking = false;
    //}
}