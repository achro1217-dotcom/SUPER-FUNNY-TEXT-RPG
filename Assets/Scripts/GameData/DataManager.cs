using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

public sealed class DataManager
{
    private const string EnemyJsonPath = "enemies";
    private const string TextLineJsonPath = "text-lines";
    private const string TextTriggerBindingJsonPath = "text-trigger-bindings";

    public EnemyListData Enemies { get; }
    public TextLineListData TextLines { get; }
    public TextTriggerBindingListData TextTriggerBindings { get; }

    public DataManager()
    {
        List<Enemy> enemies = LoadJsonList<Enemy>(EnemyJsonPath);
        List<TextLine> textLines = LoadJsonList<TextLine>(TextLineJsonPath);
        List<TextTriggerBinding> textTriggerBindings = LoadJsonList<TextTriggerBinding>(TextTriggerBindingJsonPath);

        TextLineConditionPreprocessor.BuildCaches(textLines);

        Enemies = new EnemyListData(enemies);
        TextLines = new TextLineListData(textLines);
        TextTriggerBindings = new TextTriggerBindingListData(textTriggerBindings);
    }

    private static List<T> LoadJsonList<T>(string resourcePath)
    {
        if (string.IsNullOrWhiteSpace(resourcePath))
        {
            throw new ArgumentException("Resource path is required.", nameof(resourcePath));
        }

        TextAsset textAsset = Resources.Load<TextAsset>(resourcePath);
        if (textAsset == null)
        {
            throw new InvalidOperationException($"Game data resource not found. path=Assets/GameData/Resources/{resourcePath}.json");
        }

        string json = textAsset.text;
        List<T> items = JsonConvert.DeserializeObject<List<T>>(json);
        if (items == null)
        {
            throw new InvalidOperationException($"Failed to deserialize game data resource: {resourcePath}");
        }

        return items;
    }
}
