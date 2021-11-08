using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Location : MonoBehaviour
{
    public List<GameObject> nextLocations;

    void OnDrawGizmos()
    {
        if (nextLocations != null && nextLocations.Count != 0)
        {
            Gizmos.color = Color.blue;
            foreach (var path in nextLocations)
            {
                Gizmos.DrawLine(transform.position, path.transform.position);
            }
        }
        Gizmos.color = Color.red;
        Gizmos.DrawCube(transform.position, new Vector3(0.2f, 0.2f, 0.2f));
    }
}
