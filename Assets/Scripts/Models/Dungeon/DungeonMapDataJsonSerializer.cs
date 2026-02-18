using System;
using System.IO;
using Newtonsoft.Json;

public static class DungeonMapDataJsonSerializer
{
    public static string ToJson(DungeonMapData mapData, bool indented = true)
    {
        if (mapData == null)
        {
            throw new ArgumentNullException(nameof(mapData));
        }

        return JsonConvert.SerializeObject(mapData, CreateSettings(indented));
    }

    public static DungeonMapData FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("Json is required.", nameof(json));
        }

        var mapData = JsonConvert.DeserializeObject<DungeonMapData>(json, CreateSettings(false));
        if (mapData == null)
        {
            throw new JsonSerializationException("Failed to deserialize dungeon map data.");
        }

        return mapData;
    }

    public static void SaveToFile(string filePath, DungeonMapData mapData, bool indented = true)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path is required.", nameof(filePath));
        }

        string json = ToJson(mapData, indented);
        File.WriteAllText(filePath, json);
    }

    public static DungeonMapData LoadFromFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path is required.", nameof(filePath));
        }

        string json = File.ReadAllText(filePath);
        return FromJson(json);
    }

    private static JsonSerializerSettings CreateSettings(bool indented)
    {
        return new JsonSerializerSettings
        {
            Formatting = indented ? Formatting.Indented : Formatting.None,
            TypeNameHandling = TypeNameHandling.Auto,
            MetadataPropertyHandling = MetadataPropertyHandling.ReadAhead,
        };
    }
}
