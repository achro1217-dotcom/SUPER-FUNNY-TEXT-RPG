using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public sealed class DungeonToolControlPanelWindow : EditorWindow
{
    private static readonly DungeonCellType[] DrawableCellTypes =
    {
        DungeonCellType.Empty,
        DungeonCellType.Wall,
        DungeonCellType.EnemySpawner,
        DungeonCellType.Loot,
        DungeonCellType.Information,
        DungeonCellType.Rest,
        DungeonCellType.Event,
        DungeonCellType.Start,
        DungeonCellType.EscapeAnchor,
    };

    private static readonly string[] DrawableCellTypeLabels =
    {
        "Empty",
        "Wall",
        "Enemy",
        "Loot",
        "Info",
        "Rest",
        "Event",
        "Start",
        "Anchor",
    };

    [InitializeOnLoad]
    private static class AutoOpen
    {
        static AutoOpen()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();

                if (scene.name == "DungeonTool")
                {
                    Open();
                }
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                CloseIfOpen();
            }
        }
    }

    private const string WindowTitle = "Dungeon Tool Control Panel";
    private const string DefaultFileName = "dungeon-map";
    private const string FileExtension = "json";
    private const string UnsavedDocumentName = "Untitled";

    private int _newMapWidth = 10;
    private int _newMapHeight = 40;
    private string _currentFilePath;
    private bool _hasUnsavedChanges;
    private int _lastSavedRevision = -1;
    private DungeonPoint? _editingPoint;
    private DungeonCellType _editingCellType;
    private string _encounterDefId = "encounter.default";
    private DungeonEncounterTier _encounterTier = DungeonEncounterTier.Normal;
    private bool _encounterIsMobile;
    private string _lootDefId = "loot.default";
    private DungeonLootTier _lootTier = DungeonLootTier.Normal;
    private bool _lootRequiresEliteClear;
    private string _informationDefId = "information.default";
    private string _restDefId = "rest.default";
    private string _eventDefId = "event.default";

    public static void Open()
    {
        DungeonToolControlPanelWindow window = GetWindow<DungeonToolControlPanelWindow>();
        window.titleContent = new GUIContent(WindowTitle);
        window.Show();
    }

    public static void CloseIfOpen()
    {
        if (!HasOpenInstances<DungeonToolControlPanelWindow>())
        {
            return;
        }

        DungeonToolControlPanelWindow window = GetWindow<DungeonToolControlPanelWindow>();
        window.Close();
    }

    private void OnEnable()
    {
        DungeonTool.ToolStateChanged += HandleToolStateChanged;
    }

    private void OnDisable()
    {
        DungeonTool.ToolStateChanged -= HandleToolStateChanged;
    }

    private void OnGUI()
    {
        GUILayout.Label(WindowTitle, EditorStyles.boldLabel);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Play mode session is required.", MessageType.Info);
            return;
        }

        DungeonTool tool = DungeonTool.Instance;
        if (tool == null)
        {
            EditorGUILayout.HelpBox("DungeonTool instance was not found in scene.", MessageType.Warning);
            return;
        }

        SyncDirtyState(tool);
        DrawDocumentStatus();
        DrawDocumentActions(tool);
        DrawToolModeSection(tool);
    }

    private void DrawDocumentStatus()
    {
        string fileName = string.IsNullOrWhiteSpace(_currentFilePath)
            ? UnsavedDocumentName
            : Path.GetFileName(_currentFilePath);
        string dirtyMarker = _hasUnsavedChanges ? " *" : string.Empty;

        EditorGUILayout.LabelField("Document", $"{fileName}{dirtyMarker}");
        EditorGUILayout.LabelField("Path", string.IsNullOrWhiteSpace(_currentFilePath) ? "(not saved)" : _currentFilePath);
    }

    private void DrawDocumentActions(DungeonTool tool)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("New"))
            {
                CreateNewDocument(tool);
            }

            if (GUILayout.Button("Save"))
            {
                SaveDocument(tool);
            }

            if (GUILayout.Button("Load"))
            {
                LoadDocument(tool);
            }
        }
    }

    private void CreateNewDocument(DungeonTool tool)
    {
        if (!ConfirmDiscardUnsavedChanges())
        {
            return;
        }

        DungeonToolNewMapWindow.Open(_newMapWidth, _newMapHeight, (width, height) =>
        {
            _newMapWidth = width;
            _newMapHeight = height;
            tool.CreateNewDungeon(width, height);
            _currentFilePath = null;
            _hasUnsavedChanges = true;
            _lastSavedRevision = -1;
            Repaint();
        });
    }

    private void SaveDocument(DungeonTool tool)
    {
        if (tool.CurrentMap == null)
        {
            EditorUtility.DisplayDialog("Save Dungeon", "No dungeon map is loaded.", "OK");
            return;
        }

        string filePath = ResolveSavePath();
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        try
        {
            DungeonMapDataJsonSerializer.SaveToFile(filePath, tool.CurrentMap, indented: true);
            _currentFilePath = filePath;
            _lastSavedRevision = tool.MapRevision;
            _hasUnsavedChanges = false;
            EditorUtility.DisplayDialog("Save Dungeon", "Dungeon map was saved.", "OK");
        }
        catch (Exception ex)
        {
            EditorUtility.DisplayDialog("Save Dungeon", $"Failed to save dungeon map.\n{ex.Message}", "OK");
        }
    }

    private void LoadDocument(DungeonTool tool)
    {
        if (!ConfirmDiscardUnsavedChanges())
        {
            return;
        }

        string filePath = EditorUtility.OpenFilePanel("Load Dungeon Map", GetInitialDirectory(), FileExtension);
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        try
        {
            DungeonMapData mapData = DungeonMapDataJsonSerializer.LoadFromFile(filePath);
            tool.LoadDungeon(mapData);
            _newMapWidth = mapData.Width;
            _newMapHeight = mapData.Height;
            _currentFilePath = filePath;
            _lastSavedRevision = tool.MapRevision;
            _hasUnsavedChanges = false;
        }
        catch (Exception ex)
        {
            EditorUtility.DisplayDialog("Load Dungeon", $"Failed to load dungeon map.\n{ex.Message}", "OK");
        }
    }

    private string ResolveSavePath()
    {
        if (!string.IsNullOrWhiteSpace(_currentFilePath))
        {
            return _currentFilePath;
        }

        return EditorUtility.SaveFilePanel("Save Dungeon Map", GetInitialDirectory(), DefaultFileName, FileExtension);
    }

    private string GetInitialDirectory()
    {
        if (!string.IsNullOrWhiteSpace(_currentFilePath))
        {
            return Path.GetDirectoryName(_currentFilePath);
        }

        return Application.dataPath;
    }

    private bool ConfirmDiscardUnsavedChanges()
    {
        if (!_hasUnsavedChanges)
        {
            return true;
        }

        return EditorUtility.DisplayDialog(
            "Unsaved Changes",
            "There are unsaved dungeon changes. Discard and continue?",
            "Discard",
            "Cancel");
    }

    private void DrawToolModeSection(DungeonTool tool)
    {
        EditorGUILayout.Space();
        GUILayout.Label("Tool Mode", EditorStyles.boldLabel);

        DungeonToolMode selectedMode = (DungeonToolMode)GUILayout.Toolbar(
            (int)tool.Mode,
            new[] { "Draw", "Edit" });
        if (selectedMode != tool.Mode)
        {
            tool.SetMode(selectedMode);
        }

        if (tool.Mode == DungeonToolMode.Draw)
        {
            DrawModeEditor(tool);
            return;
        }

        DrawSelectionEditor(tool);
    }

    private void DrawModeEditor(DungeonTool tool)
    {
        EditorGUILayout.LabelField("Brush", EditorStyles.boldLabel);
        DungeonCellType brushCellType = tool.DrawBrushCellType;
        int selectedIndex = GetCellTypePaletteIndex(brushCellType);
        int nextIndex = GUILayout.SelectionGrid(selectedIndex, DrawableCellTypeLabels, 5);
        DungeonCellType nextCellType = GetCellTypeByPaletteIndex(nextIndex, brushCellType);
        if (nextCellType != brushCellType)
        {
            tool.SetDrawBrushCellType(nextCellType);
        }

        EditorGUILayout.LabelField("Current Brush", brushCellType.ToString());
        EditorGUILayout.HelpBox("Drag with left mouse button in Game view to paint cells.", MessageType.None);
    }

    private void DrawSelectionEditor(DungeonTool tool)
    {
        if (!tool.TryGetSelectedCell(out DungeonPoint point, out DungeonCellData cellData))
        {
            EditorGUILayout.HelpBox("Click a cell in Game view to select it.", MessageType.Info);
            return;
        }

        EnsureEditorState(point, cellData);

        EditorGUILayout.LabelField("Selected", point.ToString());
        EditorGUILayout.LabelField("Cell Type", cellData.CellType.ToString());

        if (!HasEditablePayload(cellData.CellType))
        {
            EditorGUILayout.HelpBox("This cell type has no editable payload data.", MessageType.None);
            return;
        }

        DrawPayloadEditorFields(cellData.CellType);

        if (GUILayout.Button("Apply Data"))
        {
            ApplyEditedPayload(tool, cellData.CellType);
        }
    }

    private void SyncDirtyState(DungeonTool tool)
    {
        if (tool.CurrentMap == null)
        {
            _hasUnsavedChanges = false;
            return;
        }

        if (_lastSavedRevision >= 0)
        {
            _hasUnsavedChanges = tool.MapRevision != _lastSavedRevision;
        }
    }

    private void HandleToolStateChanged()
    {
        Repaint();
    }

    private static int GetCellTypePaletteIndex(DungeonCellType cellType)
    {
        for (int i = 0; i < DrawableCellTypes.Length; i++)
        {
            if (DrawableCellTypes[i] == cellType)
            {
                return i;
            }
        }

        return 0;
    }

    private static DungeonCellType GetCellTypeByPaletteIndex(int index, DungeonCellType fallback)
    {
        if (index < 0 || index >= DrawableCellTypes.Length)
        {
            return fallback;
        }

        return DrawableCellTypes[index];
    }

    private void EnsureEditorState(DungeonPoint point, DungeonCellData cellData)
    {
        if (_editingPoint.HasValue && _editingPoint.Value == point && _editingCellType == cellData.CellType)
        {
            return;
        }

        _editingPoint = point;
        _editingCellType = cellData.CellType;
        LoadPayloadFromCell(cellData);
    }

    private void LoadPayloadFromCell(DungeonCellData cellData)
    {
        switch (cellData.CellType)
        {
            case DungeonCellType.EnemySpawner:
            {
                var encounterCell = cellData as DungeonEncounterCellData;
                if (encounterCell == null)
                {
                    return;
                }

                _encounterDefId = encounterCell.EncounterDefId;
                _encounterTier = encounterCell.Tier;
                _encounterIsMobile = encounterCell.IsMobile;
                return;
            }
            case DungeonCellType.Loot:
            {
                var lootCell = cellData as DungeonLootCellData;
                if (lootCell == null)
                {
                    return;
                }

                _lootDefId = lootCell.LootDefId;
                _lootTier = lootCell.LootTier;
                _lootRequiresEliteClear = lootCell.RequiresEliteClear;
                return;
            }
            case DungeonCellType.Information:
            {
                var informationCell = cellData as DungeonInformationCellData;
                if (informationCell == null)
                {
                    return;
                }

                _informationDefId = informationCell.InfoDefId;
                return;
            }
            case DungeonCellType.Rest:
            {
                var restCell = cellData as DungeonRestCellData;
                if (restCell == null)
                {
                    return;
                }

                _restDefId = restCell.RestDefId;
                return;
            }
            case DungeonCellType.Event:
            {
                var eventCell = cellData as DungeonEventCellData;
                if (eventCell == null)
                {
                    return;
                }

                _eventDefId = eventCell.EventDefId;
                return;
            }
            default:
                return;
        }
    }

    private void DrawPayloadEditorFields(DungeonCellType cellType)
    {
        switch (cellType)
        {
            case DungeonCellType.EnemySpawner:
                _encounterDefId = EditorGUILayout.TextField("Encounter Def Id", _encounterDefId);
                _encounterTier = (DungeonEncounterTier)EditorGUILayout.EnumPopup("Tier", _encounterTier);
                _encounterIsMobile = EditorGUILayout.Toggle("Is Mobile", _encounterIsMobile);
                return;
            case DungeonCellType.Loot:
                _lootDefId = EditorGUILayout.TextField("Loot Def Id", _lootDefId);
                _lootTier = (DungeonLootTier)EditorGUILayout.EnumPopup("Loot Tier", _lootTier);
                _lootRequiresEliteClear = EditorGUILayout.Toggle("Requires Elite Clear", _lootRequiresEliteClear);
                return;
            case DungeonCellType.Information:
                _informationDefId = EditorGUILayout.TextField("Information Def Id", _informationDefId);
                return;
            case DungeonCellType.Rest:
                _restDefId = EditorGUILayout.TextField("Rest Def Id", _restDefId);
                return;
            case DungeonCellType.Event:
                _eventDefId = EditorGUILayout.TextField("Event Def Id", _eventDefId);
                return;
            default:
                return;
        }
    }

    private void ApplyEditedPayload(DungeonTool tool, DungeonCellType cellType)
    {
        try
        {
            DungeonCellData editedCellData = BuildEditedCellData(cellType);
            if (editedCellData == null)
            {
                return;
            }

            if (tool.TrySetSelectedCellData(editedCellData))
            {
                _hasUnsavedChanges = true;
            }
        }
        catch (Exception ex)
        {
            EditorUtility.DisplayDialog("Edit Cell Data", $"Failed to apply cell data.\n{ex.Message}", "OK");
        }
    }

    private DungeonCellData BuildEditedCellData(DungeonCellType cellType)
    {
        switch (cellType)
        {
            case DungeonCellType.EnemySpawner:
                return new DungeonEncounterCellData(_encounterDefId, _encounterTier, _encounterIsMobile);
            case DungeonCellType.Loot:
                return new DungeonLootCellData(_lootDefId, _lootTier, _lootRequiresEliteClear);
            case DungeonCellType.Information:
                return new DungeonInformationCellData(_informationDefId);
            case DungeonCellType.Rest:
                return new DungeonRestCellData(_restDefId);
            case DungeonCellType.Event:
                return new DungeonEventCellData(_eventDefId);
            default:
                return null;
        }
    }

    private static bool HasEditablePayload(DungeonCellType cellType)
    {
        switch (cellType)
        {
            case DungeonCellType.EnemySpawner:
            case DungeonCellType.Loot:
            case DungeonCellType.Information:
            case DungeonCellType.Rest:
            case DungeonCellType.Event:
                return true;
            default:
                return false;
        }
    }
}
