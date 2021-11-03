using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class PathManager : MonoBehaviour
{
    public Text clockText;
    public GameObject taxiPrefab;
    public GameObject locationPrefab;
    public float taxiSpeed = 250f;
    public float realSecondsPerDay = 60f;
    private float day;

    private DataManager dataManager;
    private GameObject[] locations;

    void Start()
    {
        dataManager = new DataManager();
        CreateChildLocations();
        locations = new GameObject[DataManager.NUM_OF_ELEMENTS];
        Debug.Log(transform.childCount);
        int c = 0;
        foreach (Transform child in transform)
        {
            locations[c] = child.gameObject;
            c++;
        }
        for (int i = 0; i < 1000; i++) {
            var startIndex = Random.Range(0, DataManager.NUM_OF_ELEMENTS);
            var transitionIndex = GetRandomWeightedIndex(dataManager.transitionMatrix[startIndex].ToArray());
            object[] taxiParams = new object[3] { locations[startIndex], locations[transitionIndex], transitionIndex };
            StartCoroutine("NewTaxi", taxiParams);
        }
    }

    // Update is called once per frame
    void Update()
    {
        day += Time.deltaTime / realSecondsPerDay;
        float dayNormalized = day % 1f;

        string hoursString = Mathf.Floor(dayNormalized * 24f).ToString("00");
        string minutesString = Mathf.Floor(((dayNormalized * 24) % 1f) * 60f).ToString("00");

        clockText.text = $"Time {hoursString}:{minutesString}";
    }

    private void CreateChildLocations()
    {
        GameObject parent = Instantiate(
            locationPrefab,
            transform.position + new Vector3(0, 1, 0),
            transform.rotation);
        parent.transform.parent = transform;
        parent.gameObject.name = $"Location PADRE";

        GameObject previous1 = parent;
        GameObject previous2 = parent;
        bool connect = false;
        for (int i = 0; i < 264; i += 2)
        {
            GameObject child1 = Instantiate(
                locationPrefab,
                transform.position + new Vector3(-5, -i, 0),
                transform.rotation);
            child1.transform.parent = transform;
            child1.gameObject.name = $"Location {i + 1}";
            GameObject child2 = Instantiate(
                locationPrefab,
                transform.position + new Vector3(5, -i, 0),
                transform.rotation);
            child2.transform.parent = transform;
            child2.gameObject.name = $"Location {i + 2}";

            if (previous1 != null)
            {
                previous1.GetComponent<Location>().nextLocations.Add(child1);
                child1.GetComponent<Location>().nextLocations.Add(previous1);
            }
            previous1 = child1;
            if (previous2 != null)
            {
                previous2.GetComponent<Location>().nextLocations.Add(child2);
                child2.GetComponent<Location>().nextLocations.Add(previous2);
            }
            previous2 = child2;

            if (connect)
            {
                child1.GetComponent<Location>().nextLocations.Add(child2);
                child2.GetComponent<Location>().nextLocations.Add(child1);
            }
            connect = !connect;
        }
    }

    IEnumerator NewTaxi(object[] taxiParams)
    {
        GameObject start = (GameObject)taxiParams[0];
        GameObject end = (GameObject)taxiParams[1];
        int transitionIndex = (int)taxiParams[2];
        var path = DepthFirstSearch(start, end);
        int currentIndex = 0;
        int destinationIndex = path.Count;
        var newTaxi = Instantiate(taxiPrefab, start.transform.position, taxiPrefab.transform.rotation);
        while (true)
        {
            if (currentIndex >= destinationIndex)
            {
                // Has arrived to destination
                start = end;
                transitionIndex = GetRandomWeightedIndex(dataManager.transitionMatrix[transitionIndex].ToArray());
                end = locations[transitionIndex];
                currentIndex = 0;
                path = DepthFirstSearch(start, end);
                destinationIndex = path.Count;
            }
            newTaxi.transform.position = Vector3.MoveTowards(
                newTaxi.transform.position,
                path[currentIndex].transform.position,
                Time.deltaTime * 1 / realSecondsPerDay * taxiSpeed);
            // Probably the float numbers will cause an issue here if it's not close enough (?).
            if (newTaxi.transform.position == path[currentIndex].transform.position)
            {
                currentIndex++;
            }
            yield return null;
        }
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
                var newPath = new List<GameObject>(path);
                var lPath = DFSHelper(newPath, visited, location, dest);
                if (lPath != null)
                {
                    return lPath;
                }
            }
        }
        return null;
    }

    // Code taken from: https://forum.unity.com/threads/random-numbers-with-a-weighted-chance.442190/
    // since Unity does not have a function like numpy to do this.
    public int GetRandomWeightedIndex(float[] weights)
    {
        if (weights == null || weights.Length == 0) return -1;

        float w;
        float t = 0;
        int i;
        for (i = 0; i < weights.Length; i++)
        {
            w = weights[i];

            if (float.IsPositiveInfinity(w))
            {
                return i;
            }
            else if (w >= 0f && !float.IsNaN(w))
            {
                t += weights[i];
            }
        }

        float r = Random.value;
        float s = 0f;

        for (i = 0; i < weights.Length; i++)
        {
            w = weights[i];
            if (float.IsNaN(w) || w <= 0f) continue;

            s += w / t;
            if (s >= r) return i;
        }

        return -1;
    }
}

internal class DataManager
{
    public const int NUM_OF_ELEMENTS = 265;
    private const string DEFAULT_PATH = "Assets/Data/";
    private const string RIDE_ORIGIN_FILE = "taxis_ride_origin_counting.csv";
    private const string SPEED_FILE = "taxis_speed_histogram.csv";
    private const string TIME_HISTOGRAM_FILE = "taxis_time_histogram_window_of_10_minutes.csv";
    private const string TRANSITION_MATRIX_FILE = "taxis_transition_matrix.csv";

    public List<List<float>> transitionMatrix;
    public List<List<float>> speedHistogram;
    public List<List<float>> timeHistogram;
    public List<float> rideOrigin;

    public DataManager()
    {
        transitionMatrix = loadCSV(TRANSITION_MATRIX_FILE);
        speedHistogram = loadCSV(SPEED_FILE);
        timeHistogram = loadCSV(TIME_HISTOGRAM_FILE);
    }

    private List<List<float>> loadCSV(string file)
    {
        StreamReader reader = new StreamReader(DEFAULT_PATH + file);
        var content = reader.ReadToEnd();
        var lines = content.Split('\n');
        var values = new List<List<float>>();
        foreach (var line in lines)
        {
            var elements = line.Split(',');
            if (elements.Length != NUM_OF_ELEMENTS)
            {
                continue;
            }
            var lineValues = new List<float>();
            foreach (var e in elements)
            {
                lineValues.Add(float.Parse(e));
            }
            values.Add(lineValues);
        }
        reader.Close();
        return values;
    }
}