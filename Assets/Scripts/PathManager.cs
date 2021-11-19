using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;

public class PathManager : MonoBehaviour
{
    public Text clockText;
    public Text taxisNumberText;
    public GameObject taxiPrefab;
    public GameObject locationPrefab;
    public Button speedButton;
    public TextMeshProUGUI speedButtonText;
    public float maxTaxiSpeed = 250f;
    public float realSecondsPerDay = 120f;
    public int maxStartTaxisPerLocation = 500;
    public int maxTaxisNumber = 4000;

    private float day;
    private float currentRealSecondsPerDay;
    private DataManager dataManager;
    private GameObject[] locations;
    private int targetScreenTaxis;
    private readonly object currentTaxisNumLock = new object();
    private readonly object neededTaxisLock = new object();
    private int currentTaxisNum;
    private int neededTaxis;

    private int previousMinute;
    private int currentMinute;
    private int buttonSpeedIndex = 0;
    private float[] buttonSpeeds = { 1f, 2f, 4f, 0.25f, 0.5f };


    void Start()
    {
        dataManager = new DataManager(maxStartTaxisPerLocation);
        currentTaxisNum = 0;
        targetScreenTaxis = 0;
        neededTaxis = 0;
        // Define the minute index, 1 unit represents 10 minutes.
        previousMinute = -1;
        currentMinute = 0;
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
                CreateNewTaxi(i);
                targetScreenTaxis += 1;
            }
        }
        currentRealSecondsPerDay = realSecondsPerDay;
        speedButton.onClick.AddListener(ChangeSpeed);
        speedButtonText.text = $"x{buttonSpeeds[buttonSpeedIndex]}";
    }

    void Update()
    {
        day += Time.deltaTime / currentRealSecondsPerDay;
        float dayNormalized = day % 1f;
        currentMinute = Mathf.FloorToInt((((dayNormalized * 24) % 24f) * 60f) / 10);

        string hoursString = Mathf.Floor(dayNormalized * 24f).ToString("00");
        string minutesString = Mathf.Floor(((dayNormalized * 24) % 1f) * 60f).ToString("00");

        clockText.text = $"Time {hoursString}:{minutesString}";
        taxisNumberText.text = $"Current Taxis: {currentTaxisNum}";

        if (currentMinute != previousMinute)
        {
            int previousMinute = currentMinute;
            lock (currentTaxisNumLock)
            {
                UpdateTaxisNeeded(
                    Mathf.RoundToInt(
                        dataManager.timeHistogram[currentMinute] * maxTaxisNumber) - targetScreenTaxis);
            }
        }
        lock (neededTaxisLock)
        {
            if (neededTaxis > 0)
            {
                // This might seem silly but it is needed if the time is fast.
                // We can delete this or adjust, I just put an arbitrary number for now
                if (neededTaxis >= 5)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        CreateNewTaxi(Random.Range(0, 265));
                        neededTaxis--;
                    }
                }
                CreateNewTaxi(Random.Range(0, 265));
                neededTaxis--;
            }
        }
    }

    public void ChangeSpeed()
    {
        buttonSpeedIndex = (buttonSpeedIndex + 1) % buttonSpeeds.Length;
        speedButtonText.text = $"x{buttonSpeeds[buttonSpeedIndex]}";
        currentRealSecondsPerDay = realSecondsPerDay / buttonSpeeds[buttonSpeedIndex];
    }

    private void CreateNewTaxi(int startIndex)
    {
        var transitionIndex = GetRandomWeightedIndex(dataManager.transitionMatrix[startIndex].ToArray());
        var speedIndex = GetRandomWeightedIndex(dataManager.speedHistogram) + 1;
        object[] taxiParams = new object[4] {
                locations[startIndex],
                locations[transitionIndex],
                transitionIndex,
                speedIndex };
        StartCoroutine("NewTaxi", taxiParams);
    }

    private void UpdateTaxisNeeded(int difference)
    {
        lock (currentTaxisNumLock) lock (neededTaxisLock)
            {
                targetScreenTaxis += difference;
                neededTaxis = targetScreenTaxis - currentTaxisNum;
            }
    }

    IEnumerator NewTaxi(object[] taxiParams)
    {
        GameObject start = (GameObject)taxiParams[0];
        GameObject end = (GameObject)taxiParams[1];
        int transitionIndex = (int)taxiParams[2];
        int speedIndex = (int)taxiParams[3];
        var speed = speedIndex;
        var path = DepthFirstSearch(start, end);
        int currentIndex = 0;
        int destinationIndex = path.Count;
        var newTaxi = Instantiate(taxiPrefab, start.transform.position, taxiPrefab.transform.rotation);
        lock (currentTaxisNumLock)
        {
            currentTaxisNum++;
        }
        while (true)
        {
            if (currentIndex >= destinationIndex)
            {
                lock (neededTaxisLock) lock (currentTaxisNumLock)
                    {
                        if (neededTaxis < 0)
                        {
                            Destroy(newTaxi);
                            neededTaxis++;
                            currentTaxisNum--;
                            break;
                        }
                    }
                // Has arrived to destination
                start = end;
                transitionIndex = GetRandomWeightedIndex(
                    dataManager.transitionMatrix[transitionIndex].ToArray());
                speed = GetRandomWeightedIndex(dataManager.speedHistogram) + 1;
                end = locations[transitionIndex];
                currentIndex = 0;
                path = DepthFirstSearch(start, end);
                destinationIndex = path.Count;
            }
            newTaxi.transform.position = Vector3.MoveTowards(
                newTaxi.transform.position,
                path[currentIndex].transform.position,
                Time.deltaTime * 1 / currentRealSecondsPerDay * maxTaxiSpeed * speed);
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
    private TextAsset RIDE_ORIGIN_FILE = Resources.Load<TextAsset>("Data/taxis_ride_origin_normalized");
    private TextAsset SPEED_FILE = Resources.Load<TextAsset>("Data/taxis_speed_histogram");
    private TextAsset TIME_HISTOGRAM_FILE = Resources.Load<TextAsset>("Data/taxis_time_histogram_window_of_10_minutes");
    private TextAsset TRANSITION_MATRIX_FILE = Resources.Load<TextAsset>("Data/taxis_transition_matrix");
    public List<List<float>> transitionMatrix;
    public float[] speedHistogram;
    public float[] timeHistogram;
    public float[] rideOriginNormalized;
    public int[] rideOriginCount;

    public DataManager(int maxTaxisPerLocation)
    {
        transitionMatrix = loadCSVMatrix(TRANSITION_MATRIX_FILE);
        timeHistogram = loadCSVLineList(TIME_HISTOGRAM_FILE).ToArray();
        speedHistogram = loadCSVLineList(SPEED_FILE).ToArray();
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
        Debug.Log("Total Start Taxis: " + count);
    }

    private List<List<float>> loadCSVMatrix(TextAsset textFile)
    {
        var content = textFile.text;
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
        return values;
    }

    private List<float> loadCSVLineList(TextAsset textFile)
    {
        var content = textFile.text;
        var line = content.Split('\n')[0];
        var elements = line.Split(',');
        var values = new List<float>();
        foreach (var e in elements)
        {
            values.Add(float.Parse(e));
        }
        return values;
    }
}