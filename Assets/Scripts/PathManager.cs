using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class PathManager : MonoBehaviour
{
    public Text clockText;
    public GameObject taxiPrefab;
    public float taxiSpeed = 250f;
    public float realSecondsPerDay = 60f;
    private float day;

    private DataManager dataManager;
    private GameObject[] locations;

    void Start() {
        dataManager = new DataManager();
        locations = new GameObject[4];
        int c = 0;
        foreach (Transform child in transform)
        {
            locations[c] = child.gameObject;
            c++;
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

        if (Input.GetKeyDown(KeyCode.A)) {
            var index = Random.Range(0, DataManager.NUM_OF_ELEMENTS);
            var startIndex = GetLocationIndex(index);
            var transitionIndex = GetRandomWeightedIndex(dataManager.transitionMatrix[index].ToArray());
            var endIndex = GetLocationIndex(transitionIndex);
            Debug.Log(endIndex);
            object[] taxiParams = new object[3]{locations[2], locations[3], transitionIndex};
            StartCoroutine("NewTaxi", taxiParams);
        }
    }

    private int GetLocationIndex(float transitionIndex) {
        var index = Mathf.CeilToInt(transitionIndex / 67f);
        return index == 0 ? 0 : index - 1;
    }

    IEnumerator NewTaxi(object[] taxiParams) 
    {
        GameObject start = (GameObject) taxiParams[0];
        GameObject end = (GameObject) taxiParams[1];
        int transitionIndex = (int) taxiParams[2];
        var path = DepthFirstSearch(start, end);
        foreach(var n in path) {
            Debug.Log(n);
        }
        int currentIndex = 0;
        int destinationIndex = path.Count;
        var newTaxi = Instantiate(taxiPrefab, start.transform.position, taxiPrefab.transform.rotation);
        while (true) {
            if (currentIndex >= destinationIndex) {
                // Has arrived to destination
                start = end;
                transitionIndex = GetRandomWeightedIndex(dataManager.transitionMatrix[transitionIndex].ToArray());
                var endIndex = GetLocationIndex(transitionIndex);
                Debug.Log(endIndex);
                end = locations[endIndex];
                currentIndex = 0;
                Debug.Log(start);
                Debug.Log(end);
                path = DepthFirstSearch(start, end);
                destinationIndex = path.Count;
            }
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
        Debug.Log("Start " + current.name);
        Debug.Log("End " + dest.name);
        visited.Add(current);
        path.Add(current);
        Debug.Log("Path " + path.Count);
        Debug.Log("-----");
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
    // TODO(hivini): Talk about existential pain.
    public List<float> rideOrigin;

    public DataManager() {
        transitionMatrix = loadCSV(TRANSITION_MATRIX_FILE);
        speedHistogram = loadCSV(SPEED_FILE);
        timeHistogram = loadCSV(TIME_HISTOGRAM_FILE);
    }

    private List<List<float>> loadCSV(string file) {
        StreamReader reader = new StreamReader(DEFAULT_PATH + file); 
        var content = reader.ReadToEnd();
        var lines = content.Split('\n');
        var values = new List<List<float>>();
        foreach (var line in lines) {
            var elements = line.Split(',');
            if (elements.Length != NUM_OF_ELEMENTS) {
                continue;
            }
            var lineValues = new List<float>();
            foreach(var e in elements) {
                lineValues.Add(float.Parse(e));
            }
            values.Add(lineValues);
        }
        Debug.Log(values.Count);
        reader.Close();
        return values;
    }
}