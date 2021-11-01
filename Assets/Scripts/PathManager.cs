using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PathManager : MonoBehaviour
{
    public Text clockText;
    public GameObject car;
    public GameObject startLocation;
    public GameObject destinationLocation;
    public float taxiSpeed = 250f;
    public float realSecondsPerDay = 60f;

    private List<GameObject> path;
    private int currentIndex;
    private float day;

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
        day = 0;
    }

    // Update is called once per frame
    void Update()
    {
        day += Time.deltaTime / realSecondsPerDay;
        float dayNormalized = day % 1f;

        string hoursString = Mathf.Floor(dayNormalized * 24f).ToString("00");
        string minutesString = Mathf.Floor(((dayNormalized * 24) % 1f) * 60f).ToString("00");

        clockText.text = $"Time {hoursString}:{minutesString}";

        if (currentIndex != path.Count)
        {
            car.transform.position = Vector3.MoveTowards(
                car.transform.position,
                path[currentIndex].transform.position,
                Time.deltaTime * 1 / realSecondsPerDay * taxiSpeed);
            // Probably the float numbers will cause an issue here if it's not close enough (?).
            if (car.transform.position == path[currentIndex].transform.position) {
                currentIndex++;
            }
        } else {
            currentIndex = 0;
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
