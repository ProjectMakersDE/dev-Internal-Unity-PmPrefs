using System;
using System.Collections.Generic;
using UnityEngine;

public class TestPmPrefs : MonoBehaviour
{
    public void Save()
    {
        PmPrefs.Save("int", 1337);
        PmPrefs.Save("float", 1.337);
        PmPrefs.Save("string", "Hello World");

        PmPrefs.Save("color", Color.cyan);
        PmPrefs.Save("vector2", Vector2.down);
        PmPrefs.Save("vector3", Vector3.back);
        PmPrefs.Save("quaternion", Quaternion.identity);

        PmPrefs.Save("Time", DateTime.Now);

        List<string> list = new List<string>();
        list.Add("First");
        list.Add("Second");
        list.Add("Last");

        PmPrefs.Save("List", list);

        Dictionary<string, int> dictionary = new Dictionary<string, int>();
        dictionary.Add("First", 1);
        dictionary.Add("Second", 2);
        dictionary.Add("Last", 3);

        PmPrefs.Save("Dictionary", dictionary);
    }

    public void Load()
    {
        print(PmPrefs.Load<int>("int"));
        print(PmPrefs.Load<float>("float"));
        print(PmPrefs.Load<string>("string"));

        print(PmPrefs.Load<Color>("color"));
        print(PmPrefs.Load<Vector2>("vector2"));
        print(PmPrefs.Load<Vector3>("vector3"));
        print(PmPrefs.Load<Quaternion>("quaternion"));
        print(PmPrefs.Load<DateTime>("Time"));

        print(PmPrefs.Load<List<string>>("List"));
        print(PmPrefs.Load<Dictionary<string, int>>("Dictionary"));
    }
}