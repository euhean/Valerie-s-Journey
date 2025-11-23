using UnityEngine;

public static class JsonHelper
{
    [System.Serializable]
    private class Wrapper<T>
    {
        public T[] items;
    }

    public static T[] FromJson<T>(string json)
    {
        string wrapped = "{\"items\":" + json + "}";
        Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(wrapped);
        return wrapper.items;
    }
}
