using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathManager : MonoBehaviour
{
    public GameObject car;
    public GameObject startLocation;
    public GameObject destinationLocation;
    public float speed = 5f;

    private List<GameObject> path;
    private int currentIndex;

    // Start is called before the first frame update
    void Start()
    {
        path = BreadthFirstSearch();
        Debug.Log("PATH");
        foreach (var p in path)
        {
            Debug.Log(p.name);
        }
        currentIndex = 0;
    }

    // Update is called once per frame
    void Update()
    {
        if (currentIndex != path.Count)
        {
            car.transform.position = Vector3.MoveTowards(
                car.transform.position,
                path[currentIndex].transform.position,
                Time.deltaTime * speed);
            // Probably the float numbers will cause an issue here if it's not close enough (?).
            if (car.transform.position == path[currentIndex].transform.position) {
                currentIndex++;
            }
        }
    }

    List<GameObject> BreadthFirstSearch()
    {
        List<GameObject> path = new List<GameObject>();
        List<GameObject> visited = new List<GameObject>();
        path = BFSHelper(path, visited, startLocation);
        if (path == null)
        {
            throw new System.Exception("There is no valid path from start to end location");
        }
        return path;
    }

    List<GameObject> BFSHelper(List<GameObject> path, List<GameObject> visited, GameObject current)
    {
        visited.Add(current);
        path.Add(current);
        if (current == destinationLocation)
        {
            return path;
        }
        List<GameObject> locations = current.GetComponent<Location>().nextLocations;
        foreach (var location in locations)
        {
            if (!visited.Contains(location))
            {
                path = BFSHelper(path, visited, location);
                if (path != null)
                {
                    return path;
                }
            }
        }
        return null;
    }
}
