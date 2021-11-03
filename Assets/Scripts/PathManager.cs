using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PathManager : MonoBehaviour
{
    public Text clockText;
    public GameObject taxiPrefab;
    public GameObject startLocation;
    public GameObject destinationLocation;
    public float taxiSpeed = 250f;
    public float realSecondsPerDay = 60f;
    private float day;

    // Update is called once per frame
    void Update()
    {
        day += Time.deltaTime / realSecondsPerDay;
        float dayNormalized = day % 1f;

        string hoursString = Mathf.Floor(dayNormalized * 24f).ToString("00");
        string minutesString = Mathf.Floor(((dayNormalized * 24) % 1f) * 60f).ToString("00");

        clockText.text = $"Time {hoursString}:{minutesString}";

        if (Input.GetKeyDown(KeyCode.A)) {
            object[] taxiParams = new object[2]{startLocation, destinationLocation};
            StartCoroutine("NewTaxi", taxiParams);
        }

        if (Input.GetKeyDown(KeyCode.B)) {
            object[] taxiParams = new object[2]{destinationLocation, startLocation};
            StartCoroutine("NewTaxi", taxiParams);
        }
    }

    IEnumerator NewTaxi(object[] taxiParams) 
    {
        GameObject start = (GameObject) taxiParams[0];
        GameObject end = (GameObject) taxiParams[1];
        var path = DepthFirstSearch(start, end);
        foreach(var n in path) {
            Debug.Log(n);
        }
        int currentIndex = 0;
        int destinationIndex = path.Count;
        var newTaxi = Instantiate(taxiPrefab, start.transform.position, taxiPrefab.transform.rotation);
        while (currentIndex < destinationIndex) {
            newTaxi.transform.position = Vector3.MoveTowards(
                newTaxi.transform.position,
                path[currentIndex].transform.position,
                Time.deltaTime * 1 / realSecondsPerDay * taxiSpeed);
            // Probably the float numbers will cause an issue here if it's not close enough (?).
            if (newTaxi.transform.position == path[currentIndex].transform.position) {
                currentIndex++;
            }
            yield return null;
        }
        Destroy(newTaxi);
        Debug.Log("Destroyed");
    }

    List<GameObject> DepthFirstSearch(GameObject st, GameObject dest)
    {
        List<GameObject> path = new List<GameObject>();
        List<GameObject> visited = new List<GameObject>();
        path = DFSHelper(path, visited, st, dest);
        if (path == null)
        {
            throw new System.Exception("There is no valid path from start to end location");
        }
        return path;
    }

    List<GameObject> DFSHelper(List<GameObject> path, List<GameObject> visited, GameObject current, GameObject dest)
    {
        visited.Add(current);
        path.Add(current);
        if (current == dest)
        {
            return path;
        }
        List<GameObject> locations = current.GetComponent<Location>().nextLocations;
        foreach (var location in locations)
        {
            if (!visited.Contains(location))
            {
                path = DFSHelper(path, visited, location, dest);
                if (path != null)
                {
                    return path;
                }
            }
        }
        return null;
    }
}
