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
    public float maxTaxiSpeed = 250f;
    public float realSecondsPerDay = 60f;
    public int maxStartTaxisPerLocation = 500;

    private float day;
    private DataManager dataManager;
    private GameObject[] locations;

    void Start()
    {
        dataManager = new DataManager(maxStartTaxisPerLocation);
        // TODO(hivini): Change to real locations.
        CreateChildLocations();
        locations = new GameObject[DataManager.NUM_OF_ELEMENTS];
        int c = 0;
        foreach (Transform child in transform)
        {
            locations[c] = child.gameObject;
            c++;
        }
        for (int l = 0; l < locations.Length; l++)
        {
            for (int i = 0; i < dataManager.rideOriginCount[l]; i++)
            {
                var startIndex = l;
                var transitionIndex = GetRandomWeightedIndex(dataManager.transitionMatrix[startIndex].ToArray());
                var speedIndex = GetRandomWeightedIndex(dataManager.speedHistogram);
                object[] taxiParams = new object[4] {
                locations[startIndex],
                locations[transitionIndex],
                transitionIndex,
                speedIndex };
                StartCoroutine("NewTaxi", taxiParams);
            }
        }
    }

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
        int speedIndex = (int)taxiParams[3];
        var speed = dataManager.speedCDF[speedIndex];
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
                transitionIndex = GetRandomWeightedIndex(
                    dataManager.transitionMatrix[transitionIndex].ToArray());
                speedIndex = GetRandomWeightedIndex(dataManager.speedHistogram);
                speed = dataManager.speedCDF[speedIndex];
                end = locations[transitionIndex];
                currentIndex = 0;
                path = DepthFirstSearch(start, end);
                destinationIndex = path.Count;
            }
            newTaxi.transform.position = Vector3.MoveTowards(
                newTaxi.transform.position,
                path[currentIndex].transform.position,
                Time.deltaTime * 1 / realSecondsPerDay * maxTaxiSpeed * speed);
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
    private const string RIDE_ORIGIN_FILE = "taxis_ride_origin_normalized.csv";
    private const string SPEED_FILE = "taxis_speed_histogram.csv";
    private const string TIME_HISTOGRAM_FILE = "taxis_time_histogram_window_of_10_minutes.csv";
    private const string TRANSITION_MATRIX_FILE = "taxis_transition_matrix.csv";

    public List<List<float>> transitionMatrix;
    public float[] speedHistogram;
    public float[] speedCDF;
    public float[] timeHistogram;
    public float[] rideOriginNormalized;
    public int[] rideOriginCount;

    public DataManager(int maxTaxisPerLocation)
    {
        transitionMatrix = loadCSVMatrix(TRANSITION_MATRIX_FILE);
        timeHistogram = loadCSVLineList(TIME_HISTOGRAM_FILE).ToArray();
        speedHistogram = loadCSVLineList(SPEED_FILE).ToArray();
        var cdf = new List<float>();
        float sum = 0;
        foreach (var e in speedHistogram)
        {
            sum += e;
            cdf.Add(sum);
        }
        speedCDF = cdf.ToArray();
        rideOriginNormalized = loadCSVLineList(RIDE_ORIGIN_FILE).ToArray();
        rideOriginCount = new int[rideOriginNormalized.Length];
        var current = 0;
        foreach (var e in rideOriginNormalized)
        {
            rideOriginCount[current] = Mathf.CeilToInt(maxTaxisPerLocation * e);
            current++;
        }

        var count = 0;
        foreach (var e in rideOriginCount)
        {
            count += e;
        }
        Debug.Log("Total Taxis: " + count);
    }

    private List<List<float>> loadCSVMatrix(string file)
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

    private List<float> loadCSVLineList(string file)
    {
        StreamReader reader = new StreamReader(DEFAULT_PATH + file);
        var content = reader.ReadToEnd();
        var line = content.Split('\n')[0];
        var elements = line.Split(',');
        var values = new List<float>();
        foreach (var e in elements)
        {
            values.Add(float.Parse(e));
        }
        reader.Close();
        return values;
    }
}