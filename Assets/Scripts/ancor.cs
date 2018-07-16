using UnityEngine;



public class ancor : MonoBehaviour
{

    public string ObjectAnchorStoreName;

    UnityEngine.XR.WSA.Persistence.WorldAnchorStore anchorStore;

    bool Placing = false;
    // Use this for initialization
    void Start()
    {
        UnityEngine.XR.WSA.Persistence.WorldAnchorStore.GetAsync(AnchorStoreReady);
    }

    void AnchorStoreReady(UnityEngine.XR.WSA.Persistence.WorldAnchorStore store)
    {
        anchorStore = store;
        Placing = false;

        Debug.Log("looking for " + ObjectAnchorStoreName);
        string[] ids = anchorStore.GetAllIds();
        for (int index = 0; index < ids.Length; index++)
        {
            Debug.Log(ids[index]);
            if (ids[index] == ObjectAnchorStoreName)
            {
                UnityEngine.XR.WSA.WorldAnchor wa = anchorStore.Load(ids[index], gameObject);
                Placing = false;
                break;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Placing)
        {
            gameObject.transform.position = Camera.main.transform.position + Camera.main.transform.forward * 2;
        }
    }

    void OnSelect()
    {
        if (anchorStore == null)
        {
            return;
        }

        if (Placing)
        {
            UnityEngine.XR.WSA.WorldAnchor attachingAnchor = gameObject.AddComponent<UnityEngine.XR.WSA.WorldAnchor>();
            if (attachingAnchor.isLocated)
            {
                Debug.Log("Saving persisted position immediately");
                bool saved = anchorStore.Save(ObjectAnchorStoreName, attachingAnchor);
                Debug.Log("saved: " + saved);
            }
            else
            {
                attachingAnchor.OnTrackingChanged += AttachingAnchor_OnTrackingChanged;
            }
        }
        else
        {
            UnityEngine.XR.WSA.WorldAnchor anchor = gameObject.GetComponent<UnityEngine.XR.WSA.WorldAnchor>();
            if (anchor != null)
            {
                DestroyImmediate(anchor);
            }

            string[] ids = anchorStore.GetAllIds();
            for (int index = 0; index < ids.Length; index++)
            {
                Debug.Log(ids[index]);
                if (ids[index] == ObjectAnchorStoreName)
                {
                    bool deleted = anchorStore.Delete(ids[index]);
                    Debug.Log("deleted: " + deleted);
                    break;
                }
            }
        }

        Placing = !Placing;
    }

    private void AttachingAnchor_OnTrackingChanged(UnityEngine.XR.WSA.WorldAnchor self, bool located)
    {
        if (located)
        {
            Debug.Log("Saving persisted position in callback");
            bool saved = anchorStore.Save(ObjectAnchorStoreName, self);
            Debug.Log("saved: " + saved);
            self.OnTrackingChanged -= AttachingAnchor_OnTrackingChanged;
        }
    }
}