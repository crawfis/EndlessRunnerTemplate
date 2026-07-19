using System.Collections.Generic;
using System.IO;
using System.Linq;
using CrawfisSoftware.TempleRun;
using UnityEditor;
using UnityEngine;

namespace CrawfisSoftware.TempleRun.Editor
{
    /// <summary>
    /// Unity Editor Window for creating and editing TrackLevel JSON files.
    ///
    /// Menu: CrawfisSoftware > Track Level Editor
    ///
    /// Three-panel layout:
    ///   Left  — level file list with create/duplicate/delete
    ///   Centre — level property editor (name, difficulty, lanes, active tags)
    ///   Right  — live preview of segments included by the current tag filter
    /// </summary>
    public class TrackLevelEditorWindow : EditorWindow
    {
        // -----------------------------------------------------------------------
        // Constants
        // -----------------------------------------------------------------------

        private const string RegistryFile = "TrackSegments_Registry";

        private static string ResourcesPath =>
            Path.Combine(Application.dataPath, "TempleRun", "Resources").Replace('\\', '/');

        private static string ToAssetPath(string fullPath)
        {
            string dataPath = Application.dataPath.Replace('\\', '/');
            return fullPath.StartsWith(dataPath)
                ? "Assets" + fullPath.Substring(dataPath.Length)
                : fullPath;
        }

        // -----------------------------------------------------------------------
        // State
        // -----------------------------------------------------------------------

        private List<string> _levelPaths = new List<string>();   // full absolute paths
        private int          _selectedIndex = -1;

        private TrackSegmentLibraryDefinition  _level;
        private TrackSegmentRegistryDefinition _registry;
        private bool _isDirty;

        // Derived from registry
        private List<string>                 _allTags        = new List<string>();
        private List<TrackSegmentDefinition> _activeSegments = new List<TrackSegmentDefinition>();

        // Scroll positions
        private Vector2 _listScroll, _propsScroll, _previewScroll;

        // Styles (lazy-initialised in OnGUI)
        private GUIStyle _sectionHeader;

        // -----------------------------------------------------------------------
        // Menu entry
        // -----------------------------------------------------------------------

        [MenuItem("CrawfisSoftware/Track Level Editor")]
        public static void ShowWindow()
        {
            var w = GetWindow<TrackLevelEditorWindow>("Track Level Editor");
            w.minSize = new Vector2(940, 540);
            w.Show();
        }

        // -----------------------------------------------------------------------
        // Unity callbacks
        // -----------------------------------------------------------------------

        private void OnEnable()
        {
            RefreshAll();
        }

        private void OnGUI()
        {
            EnsureStyles();
            DrawToolbar();

            EditorGUILayout.BeginHorizontal();
            {
                DrawLevelList();
                DrawSeparator();
                DrawProperties();
                DrawSeparator();
                DrawPreview();
            }
            EditorGUILayout.EndHorizontal();
        }

        // -----------------------------------------------------------------------
        // Toolbar
        // -----------------------------------------------------------------------

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
                    RefreshAll();

                if (_isDirty)
                {
                    var prev = GUI.color;
                    GUI.color = new Color(1f, 0.85f, 0.3f);
                    if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(50)))
                        SaveLevel();
                    GUI.color = prev;
                    GUILayout.Label("● Unsaved changes", EditorStyles.miniLabel);
                }

                GUILayout.FlexibleSpace();

                if (_level != null)
                    GUILayout.Label($"  {_level.LevelName}", EditorStyles.miniLabel);
            }
            EditorGUILayout.EndHorizontal();
        }

        // -----------------------------------------------------------------------
        // Left panel — level list
        // -----------------------------------------------------------------------

        private void DrawLevelList()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(190));
            {
                EditorGUILayout.LabelField("LEVELS", _sectionHeader);

                if (GUILayout.Button("+ New Level", GUILayout.Height(22)))
                    CreateNewLevel();

                EditorGUILayout.Space(4);

                _listScroll = EditorGUILayout.BeginScrollView(_listScroll);
                {
                    for (int i = 0; i < _levelPaths.Count; i++)
                    {
                        string name    = Path.GetFileNameWithoutExtension(_levelPaths[i]);
                        bool   selected = i == _selectedIndex;

                        var prevBg = GUI.backgroundColor;
                        if (selected)
                            GUI.backgroundColor = new Color(0.25f, 0.55f, 1f, 0.85f);

                        if (GUILayout.Button(name, GUILayout.Height(20)) && !selected)
                        {
                            if (_isDirty && !ConfirmDiscard())
                            {
                                GUI.backgroundColor = prevBg;
                                continue;
                            }
                            SelectLevel(i);
                        }

                        GUI.backgroundColor = prevBg;
                    }
                }
                EditorGUILayout.EndScrollView();
            }
            EditorGUILayout.EndVertical();
        }

        // -----------------------------------------------------------------------
        // Centre panel — level properties
        // -----------------------------------------------------------------------

        private void DrawProperties()
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            {
                EditorGUILayout.LabelField("LEVEL PROPERTIES", _sectionHeader);

                if (_level == null)
                {
                    EditorGUILayout.HelpBox(
                        "Select a level from the list on the left, or click '+ New Level' to create one.",
                        MessageType.Info);
                    EditorGUILayout.EndVertical();
                    return;
                }

                _propsScroll = EditorGUILayout.BeginScrollView(_propsScroll);
                {
                    EditorGUI.BeginChangeCheck();

                    // -- Basic Info --
                    SectionLabel("Basic Info");
                    _level.LevelName      = EditorGUILayout.TextField("Level Name",       _level.LevelName);
                    _level.LevelNumber    = EditorGUILayout.IntField ("Level Number",     _level.LevelNumber);
                    _level.DifficultyRating = EditorGUILayout.Slider("Difficulty Rating", _level.DifficultyRating, 0f, 10f);
                    Spacer();

                    // -- Lane Settings --
                    SectionLabel("Lane Settings");
                    _level.LaneCount = EditorGUILayout.IntSlider("Lane Count", _level.LaneCount, 1, 5);
                    _level.LaneWidth = EditorGUILayout.Slider   ("Lane Width", _level.LaneWidth, 0.5f, 6f);
                    Spacer();

                    // -- Segment Source --
                    SectionLabel("Segment Source");
                    _level.SegmentRegistryFile = EditorGUILayout.TextField("Registry File",    _level.SegmentRegistryFile);
                    _level.StartSegmentId      = EditorGUILayout.TextField("Start Segment ID", _level.StartSegmentId);
                    Spacer();

                    // -- Active Tags --
                    SectionLabel("Active Tags  (filter segments from registry)");
                    DrawTagCheckboxes();

                    if (EditorGUI.EndChangeCheck())
                    {
                        _isDirty = true;
                        RefreshActiveSegments();
                    }

                    Spacer();

                    // -- Buttons --
                    EditorGUILayout.BeginHorizontal();
                    {
                        GUI.enabled = _isDirty;
                        if (GUILayout.Button("Save",   GUILayout.Height(24))) SaveLevel();
                        if (GUILayout.Button("Revert", GUILayout.Height(24)) && ConfirmDiscard())
                            SelectLevel(_selectedIndex);
                        GUI.enabled = true;

                        GUILayout.FlexibleSpace();

                        if (GUILayout.Button("Duplicate", GUILayout.Height(24))) DuplicateLevel();

                        var prev = GUI.color;
                        GUI.color = new Color(1f, 0.45f, 0.45f);
                        if (GUILayout.Button("Delete", GUILayout.Height(24))) DeleteLevel();
                        GUI.color = prev;
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndScrollView();
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawTagCheckboxes()
        {
            if (_allTags.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    $"No tags found. Ensure '{RegistryFile}.json' exists in Resources.",
                    MessageType.Warning);
                return;
            }

            if (_level.ActiveSegmentTags == null)
                _level.ActiveSegmentTags = new List<string>();

            foreach (string tag in _allTags)
            {
                int  count = _registry?.Segments.Count(s => s.Tags != null && s.Tags.Contains(tag)) ?? 0;
                bool was   = _level.ActiveSegmentTags.Contains(tag);
                bool now   = EditorGUILayout.ToggleLeft($"  {tag}  ({count} segment{(count == 1 ? "" : "s")})", was);

                if (now == was) continue;

                if (now)  _level.ActiveSegmentTags.Add(tag);
                else      _level.ActiveSegmentTags.Remove(tag);

                _isDirty = true;
                RefreshActiveSegments();
            }
        }

        // -----------------------------------------------------------------------
        // Right panel — segment preview
        // -----------------------------------------------------------------------

        private void DrawPreview()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(250));
            {
                EditorGUILayout.LabelField("SEGMENT PREVIEW", _sectionHeader);

                if (_level == null || _registry == null)
                {
                    EditorGUILayout.HelpBox("Select a level to see its segments.", MessageType.Info);
                    EditorGUILayout.EndVertical();
                    return;
                }

                EditorGUILayout.LabelField(
                    $"Active: {_activeSegments.Count} / {_registry.Segments.Count} total",
                    EditorStyles.miniLabel);
                EditorGUILayout.Space(2);

                DrawDifficultyBar();
                EditorGUILayout.Space(4);

                // Summary stats
                DrawPreviewStats();
                EditorGUILayout.Space(4);

                _previewScroll = EditorGUILayout.BeginScrollView(_previewScroll);
                {
                    foreach (var seg in _activeSegments.OrderBy(s => s.DifficultyRating))
                    {
                        float t     = Mathf.Clamp01(seg.DifficultyRating / 10f);
                        var   color = Color.Lerp(new Color(0.35f, 1f, 0.35f), new Color(1f, 0.3f, 0.3f), t);

                        var prevColor = GUI.color;
                        GUI.color = color;

                        string dir  = seg.Direction.ToString();
                        string mode = !string.IsNullOrEmpty(seg.SpawnMode) && seg.SpawnMode != SpawnModes.Procedural
                            ? $" [{seg.SpawnMode[0]}]" : "";
                        string role = !string.IsNullOrEmpty(seg.Role) && seg.Role != SegmentRoles.Normal
                            ? $" ({seg.Role})" : "";
                        int slotCount = seg.SpawnSlots?.Count ?? 0;
                        string slots = slotCount > 0 ? $" S={slotCount}" : "";

                        EditorGUILayout.LabelField(
                            $"[{dir}] {seg.Id,-12}  d={seg.DifficultyRating:F1}  L={seg.Length,4}{mode}{role}{slots}",
                            EditorStyles.miniLabel);

                        GUI.color = prevColor;
                    }
                }
                EditorGUILayout.EndScrollView();
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawDifficultyBar()
        {
            Rect  barRect = GUILayoutUtility.GetRect(0, 16, GUILayout.ExpandWidth(true));
            const int buckets = 10;
            float bucketW = barRect.width / buckets;

            var counts = new int[buckets];
            foreach (var seg in _activeSegments)
                counts[Mathf.Clamp((int)seg.DifficultyRating, 0, buckets - 1)]++;

            int max = counts.Max();

            for (int i = 0; i < buckets; i++)
            {
                float t   = (float)i / (buckets - 1);
                var   col = Color.Lerp(new Color(0.15f, 0.8f, 0.15f), new Color(0.9f, 0.15f, 0.15f), t);
                col.a = counts[i] > 0
                    ? 0.35f + 0.65f * (max > 0 ? (float)counts[i] / max : 0f)
                    : 0.12f;

                EditorGUI.DrawRect(new Rect(barRect.x + i * bucketW, barRect.y, bucketW - 1f, 16), col);

                if (counts[i] > 0)
                {
                    GUI.Label(
                        new Rect(barRect.x + i * bucketW, barRect.y, bucketW, 16),
                        counts[i].ToString(),
                        EditorStyles.centeredGreyMiniLabel);
                }
            }
        }

        private void DrawPreviewStats()
        {
            if (_activeSegments.Count == 0) return;

            int leftCount  = _activeSegments.Count(s => s.Direction == Direction.Left);
            int rightCount = _activeSegments.Count(s => s.Direction == Direction.Right);
            int eitherCount = _activeSegments.Count(s => s.Direction == Direction.Either);
            int straightCount = _activeSegments.Count(s => s.Direction == Direction.Straight);
            EditorGUILayout.LabelField($"L={leftCount}  R={rightCount}  B={eitherCount}  S={straightCount}", EditorStyles.miniLabel);

            int presetCount = _activeSegments.Count(s => s.SpawnMode == SpawnModes.Preset || s.SpawnMode == SpawnModes.Hybrid);
            int slottedCount = _activeSegments.Count(s => s.SpawnSlots != null && s.SpawnSlots.Count > 0);
            if (presetCount > 0 || slottedCount > 0)
                EditorGUILayout.LabelField($"Preset/Hybrid: {presetCount}  With slots: {slottedCount}", EditorStyles.miniLabel);

            var roles = _activeSegments
                .Where(s => !string.IsNullOrEmpty(s.Role) && s.Role != SegmentRoles.Normal)
                .GroupBy(s => s.Role)
                .Select(g => $"{g.Key}:{g.Count()}");
            string roleStr = string.Join("  ", roles);
            if (!string.IsNullOrEmpty(roleStr))
                EditorGUILayout.LabelField($"Roles: {roleStr}", EditorStyles.miniLabel);
        }

        // -----------------------------------------------------------------------
        // File operations
        // -----------------------------------------------------------------------

        private void RefreshAll()
        {
            RefreshLevelList();
            LoadRegistry();
            if (_selectedIndex >= 0 && _selectedIndex < _levelPaths.Count)
                SelectLevel(_selectedIndex);
        }

        private void RefreshLevelList()
        {
            _levelPaths.Clear();
            if (!Directory.Exists(ResourcesPath)) return;

            _levelPaths = Directory
                .GetFiles(ResourcesPath, "TrackLevel_*.json")
                .Select(p => p.Replace('\\', '/'))
                .OrderBy(p => p)
                .ToList();
        }

        private void LoadRegistry()
        {
            string path = Path.Combine(ResourcesPath, RegistryFile + ".json").Replace('\\', '/');
            if (!File.Exists(path)) return;

            _registry = JsonUtility.FromJson<TrackSegmentRegistryDefinition>(File.ReadAllText(path));

            _allTags = _registry.Segments
                .Where(s => s.Tags != null)
                .SelectMany(s => s.Tags)
                .Distinct()
                .OrderBy(t => t)
                .ToList();
        }

        private void SelectLevel(int index)
        {
            _selectedIndex = index;
            if (index < 0 || index >= _levelPaths.Count) { _level = null; return; }

            _level   = JsonUtility.FromJson<TrackSegmentLibraryDefinition>(File.ReadAllText(_levelPaths[index]));
            _isDirty = false;
            RefreshActiveSegments();
        }

        private void RefreshActiveSegments()
        {
            _activeSegments.Clear();
            if (_level == null || _registry == null) return;

            var tags = new HashSet<string>(_level.ActiveSegmentTags ?? new List<string>());
            var ids  = new HashSet<string>(_level.ActiveSegmentIds  ?? new List<string>());

            foreach (var seg in _registry.Segments)
            {
                if (seg.Id == _level.StartSegmentId)
                    { _activeSegments.Add(seg); continue; }

                bool byTag = tags.Count > 0 && seg.Tags != null && seg.Tags.Any(t => tags.Contains(t));
                bool byId  = ids.Contains(seg.Id);

                if (byTag || byId)
                    _activeSegments.Add(seg);
            }
        }

        private void SaveLevel()
        {
            if (_level == null || _selectedIndex < 0) return;

            _level.Version = "2.0";
            File.WriteAllText(_levelPaths[_selectedIndex], JsonUtility.ToJson(_level, true));
            _isDirty = false;
            AssetDatabase.Refresh();
            Debug.Log($"[TrackLevelEditor] Saved: {ToAssetPath(_levelPaths[_selectedIndex])}");
        }

        private void CreateNewLevel()
        {
            if (_isDirty && !ConfirmDiscard()) return;

            Directory.CreateDirectory(ResourcesPath);

            // Pick a file name that doesn't collide
            int    num  = _levelPaths.Count + 1;
            string path = Path.Combine(ResourcesPath, $"TrackLevel_{num:D2}_New.json").Replace('\\', '/');
            while (File.Exists(path))
                path = Path.Combine(ResourcesPath, $"TrackLevel_{++num:D2}_New.json").Replace('\\', '/');

            var newLevel = new TrackSegmentLibraryDefinition
            {
                Version            = "2.0",
                LevelName          = $"Level {num}",
                LevelNumber        = num,
                DifficultyRating   = 5f,
                LaneCount          = 3,
                LaneWidth          = 2f,
                StartSegmentId     = "start",
                SegmentRegistryFile = RegistryFile,
                ActiveSegmentTags  = new List<string> { "opening" },
                ActiveSegmentIds   = new List<string>(),
                Segments           = new List<TrackSegmentDefinition>(),
                Connections        = new List<TrackSegmentConnection>()
            };

            File.WriteAllText(path, JsonUtility.ToJson(newLevel, true));
            AssetDatabase.Refresh();

            RefreshLevelList();

            int idx = _levelPaths.FindIndex(p => string.Equals(p, path, System.StringComparison.OrdinalIgnoreCase));
            _level    = newLevel;
            _selectedIndex = idx >= 0 ? idx : _levelPaths.Count - 1;
            _isDirty  = false;
            RefreshActiveSegments();
        }

        private void DuplicateLevel()
        {
            if (_level == null || _selectedIndex < 0) return;

            int    num      = _levelPaths.Count + 1;
            string safeName = (_level.LevelName ?? "Level").Replace(" ", "_");
            string path     = Path.Combine(ResourcesPath, $"TrackLevel_{num:D2}_{safeName}_Copy.json").Replace('\\', '/');

            var copy = new TrackSegmentLibraryDefinition
            {
                Version             = "2.0",
                LevelName           = _level.LevelName + " Copy",
                LevelNumber         = num,
                DifficultyRating    = _level.DifficultyRating,
                LaneCount           = _level.LaneCount,
                LaneWidth           = _level.LaneWidth,
                StartSegmentId      = _level.StartSegmentId,
                SegmentRegistryFile = _level.SegmentRegistryFile,
                ActiveSegmentTags   = new List<string>(_level.ActiveSegmentTags ?? new List<string>()),
                ActiveSegmentIds    = new List<string>(_level.ActiveSegmentIds  ?? new List<string>()),
                Segments            = new List<TrackSegmentDefinition>(_level.Segments    ?? new List<TrackSegmentDefinition>()),
                Connections         = new List<TrackSegmentConnection>(_level.Connections ?? new List<TrackSegmentConnection>())
            };

            File.WriteAllText(path, JsonUtility.ToJson(copy, true));
            AssetDatabase.Refresh();

            RefreshLevelList();

            int idx = _levelPaths.FindIndex(p => string.Equals(p, path, System.StringComparison.OrdinalIgnoreCase));
            if (idx >= 0) SelectLevel(idx);
        }

        private void DeleteLevel()
        {
            if (_selectedIndex < 0) return;

            string path = _levelPaths[_selectedIndex];
            string name = Path.GetFileName(path);

            if (!EditorUtility.DisplayDialog("Delete Level",
                    $"Delete '{name}'?\n\nThis cannot be undone.", "Delete", "Cancel"))
                return;

            AssetDatabase.DeleteAsset(ToAssetPath(path));
            if (File.Exists(path)) File.Delete(path);

            _level         = null;
            _selectedIndex = -1;
            _isDirty       = false;
            RefreshLevelList();
            AssetDatabase.Refresh();
        }

        // -----------------------------------------------------------------------
        // UI helpers
        // -----------------------------------------------------------------------

        private void SectionLabel(string label)
        {
            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
        }

        private static void Spacer() => EditorGUILayout.Space(10);

        private static void DrawSeparator()
        {
            var rect = EditorGUILayout.GetControlRect(false, GUILayout.Width(1), GUILayout.ExpandHeight(true));
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.35f));
        }

        private void EnsureStyles()
        {
            if (_sectionHeader != null) return;
            _sectionHeader = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 10,
                normal   = { textColor = new Color(0.65f, 0.88f, 1f) }
            };
        }

        private static bool ConfirmDiscard() =>
            EditorUtility.DisplayDialog(
                "Unsaved Changes",
                "You have unsaved changes. Discard them?",
                "Discard", "Keep Editing");
    }
}
