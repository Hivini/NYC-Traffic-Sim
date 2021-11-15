using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class NYCLoader : MonoBehaviour
{
    public GameObject locationPrefab;
    public GameObject pathManager;

    private const string LOCATIONS_FILE = "Assets/NYC Stuff/taxis_locations_and_rel_pos_in_map.txt";

    void Awake()
    {
        var renderer = GetComponent<SpriteRenderer>();
        var maxX = renderer.bounds.size.x;
        var maxY = renderer.bounds.size.y;
        var startX = renderer.sprite.bounds.min.x;
        var startY = renderer.sprite.bounds.max.y;
        var locations = ReadLocations(maxX, maxY, startX, startY);
        var locationsMap = new Dictionary<string, List<GameObject>>();
        foreach(var location in locations) {
            GameObject go = Instantiate(
                locationPrefab,
                location.worldLocation,
                transform.rotation);
            go.transform.parent = pathManager.transform;
            go.gameObject.name = location.name;
            var k = location.name.Split(',')[0];
            if (locationsMap.ContainsKey(k)) {
                locationsMap[k].Add(go);
            } else {
                var tmp = new List<GameObject>();
                tmp.Add(go);
                locationsMap[k] = tmp;
            }
        }

        GameObject previous = null;
        foreach(var key in locationsMap.Keys) {
            Debug.Log(key);
            foreach(var loc in locationsMap[key]) {
                if (previous == null) {
                    previous = loc;   
                } else {
                    previous.GetComponent<Location>().nextLocations.Add(loc);
                    loc.GetComponent<Location>().nextLocations.Add(previous);
                    previous = loc;
                }
            }
        }
    }

    NYCLocation[] ReadLocations(float maxX, float maxY, float startX, float startY)
    {
        StreamReader reader = new StreamReader(LOCATIONS_FILE);
        var content = reader.ReadToEnd();
        var lines = content.Split('\n');
        var locations = new List<NYCLocation>();
        foreach (var line in lines)
        {
            var elements = line.Split(';');
            if (elements.Length != 3)
            {
                continue;
            }
            var name = elements[0];
            var relX = float.Parse(elements[1]);
            var relY = float.Parse(elements[2]);
            locations.Add(
                new NYCLocation(
                    startX + maxX * relX,
                    startY - maxY * relY,
                    transform.position.z, name));
        }
        reader.Close();

        return locations.ToArray();
    }
}

internal class NYCLocation
{
    public Vector3 worldLocation;
    public string name;

    public NYCLocation(float x, float y, float z, string name)
    {
        this.worldLocation = new Vector3(x, y, z);
        this.name = name;
    }
}
