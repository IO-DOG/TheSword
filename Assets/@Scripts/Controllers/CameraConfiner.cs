using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraConfiner : MonoBehaviour
{
    void Start()
    {
        SetConfinerCollider();
    }

    void SetConfinerCollider()
    {
        Bounds combineBounds = new Bounds(Vector3.zero, Vector3.zero);
        bool boundsInitialized = false;

        foreach(Transform child in transform.parent.Find("Tiles"))
        {
            BoxCollider collider = child.GetComponent<BoxCollider>();
            if(collider != null)
            {
                if(!boundsInitialized)
                {
                    combineBounds = collider.bounds;
                    boundsInitialized = true;
                }
                else
                {
                    combineBounds.Encapsulate(collider.bounds);
                }
            }
        }
        gameObject.transform.rotation = Quaternion.Euler(new Vector3(90f, 0f, 0f));
        gameObject.transform.localScale = new Vector3(1 / 0.33f, 1 / 0.33f, 1 / 0.33f);

        gameObject.GetComponent<BoxCollider>().size = new Vector3(combineBounds.size.x - (Define.TILE_SIZE / 1.5f), combineBounds.size.z - (Define.TILE_SIZE) - (4.6f / Mathf.Sqrt(3)), 100f);
        gameObject.GetComponent<BoxCollider>().center = new Vector3(combineBounds.center.x - GetMapCount() * 100, combineBounds.center.z - (Define.TILE_SIZE / 1.5f), -5f);
    }

    int GetMapCount()
    {
        string[] parts = transform.parent.name.Split('_');
        string lastPart = parts[parts.Length - 1];

        return int.Parse(lastPart);
    }
}
