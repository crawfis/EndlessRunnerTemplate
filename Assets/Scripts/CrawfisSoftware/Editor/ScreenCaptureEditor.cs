using System;
using System.IO;

using UnityEditor;

using UnityEngine;

namespace CrawfisSoftware.Utility
{
    public class ScreenCaptureEditor : EditorWindow
    {
        // --- Static Fields ---
        private static string directory = "Screenshots/Capture/";
        private static string sPath;
        private static string latestScreenshotPath = string.Empty;
        private static bool sTempHideGizmos = true;
        private static Color cleanCameraBackgroundColor = new Color(0.6f, 0.8f, 0.2f, 1f); // Default similar to yellowGreen

        // --- Instance Fields ---
        private bool hideGizmos = true; // Default to hiding gizmos
        private bool initDone = false;
        private GUIStyle bigText;

        // --- Initialization ---
        private void InitStyles()
        {
            initDone = true;
            bigText = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold
            };
        }

        // --- GUI ---
        private void OnGUI()
        {
            if (!initDone)
            {
                InitStyles();
            }

            GUILayout.Label("Game Screen Capture", bigText);
            if (GUILayout.Button("Game screenshot"))
            {
                TakeGameScreenshot();
            }
            GUILayout.Label("Resolution: " + GetResolution());

            EditorGUILayout.Space();

            GUILayout.Label("Scene Screen Capture", bigText);
            hideGizmos = EditorGUILayout.Toggle("Hide Gizmos/Handles", hideGizmos);
            cleanCameraBackgroundColor = EditorGUILayout.ColorField("Clean Camera BG Color", cleanCameraBackgroundColor);
            if (GUILayout.Button("Scene screenshot (Current View)"))
            {
                SceneScreenshotGizmos(hideGizmos);
            }
            if (GUILayout.Button("Scene screenshot (Clean Camera)"))
            {
                SceneScreenshotCleanCamera();
            }
            GUILayout.Label("Resolution: " + GetResolution());

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            if (GUILayout.Button("Reveal in Explorer"))
            {
                ShowFolder();
            }
            GUILayout.Label("Directory: " + directory);
        }

        // --- Menu Items ---
        [MenuItem("Crawfis/Screenshots/Open Screenshot Window", priority = 1)]
        public static void ShowWindow()
        {
            GetWindow(typeof(ScreenCaptureEditor));
        }

        [MenuItem("Crawfis/Screenshots/Reveal in Explorer", priority = 4)]
        private static void ShowFolder()
        {
            if (File.Exists(latestScreenshotPath))
            {
                EditorUtility.RevealInFinder(latestScreenshotPath);
                return;
            }
            EnsureDirectoryExists();
            EditorUtility.RevealInFinder(directory);
        }

        [MenuItem("Crawfis/Screenshots/Game View", priority = 2)]
        private static void TakeGameScreenshot()
        {
            EnsureDirectoryExists();
            var path = GenerateScreenshotPath("Game");
            ScreenCapture.CaptureScreenshot(path);
            latestScreenshotPath = path;
            Debug.Log($"Screenshot saved: <b>{path}</b> with resolution <b>{GetResolution()}</b>");
        }

        [MenuItem("Crawfis/Screenshots/Scene View", priority = 3)]
        private static void TakeSceneScreenshot()
        {
            SceneScreenshotGizmos(true);
        }

        // --- Screenshot Methods ---
        private static void SceneScreenshotGizmos(bool hideGizmos)
        {
            sTempHideGizmos = hideGizmos;
            EnsureDirectoryExists();
            var path = GenerateScreenshotPath("Scene");
            sPath = path;
            SceneView.duringSceneGui += OnSceneViewGuiCaptureOnce;
            if (SceneView.lastActiveSceneView != null)
                SceneView.lastActiveSceneView.Repaint();
            latestScreenshotPath = path;
            Debug.Log($"Screenshot saved: <b>{path}</b> with resolution <b>{GetResolution()}</b>");
        }

        private static void SceneScreenshotCleanCamera()
        {
            EnsureDirectoryExists();
            var path = GenerateScreenshotPath("Scene_Clean");
            var sv = SceneView.lastActiveSceneView;
            if (sv == null || sv.camera == null)
            {
                Debug.LogWarning("No active SceneView camera found.");
                return;
            }
            Camera sceneCam = sv.camera;
            GameObject tempGO = new GameObject("TempSceneCaptureCamera");
            Camera tempCam = tempGO.AddComponent<Camera>();
            tempCam.CopyFrom(sceneCam);
            tempCam.clearFlags = CameraClearFlags.SolidColor;
            tempCam.backgroundColor = cleanCameraBackgroundColor;
            tempCam.cullingMask = sceneCam.cullingMask;
            tempCam.transform.position = sceneCam.transform.position;
            tempCam.transform.rotation = sceneCam.transform.rotation;
            tempCam.orthographic = sceneCam.orthographic;
            tempCam.orthographicSize = sceneCam.orthographicSize;
            tempCam.fieldOfView = sceneCam.fieldOfView;
            int width = Mathf.Max(1, sceneCam.pixelWidth);
            int height = Mathf.Max(1, sceneCam.pixelHeight);
            RenderTexture rt = new RenderTexture(width, height, 24);
            tempCam.targetTexture = rt;
            tempCam.Render();
            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            RenderTexture.active = null;
            tempCam.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(rt);
            UnityEngine.Object.DestroyImmediate(tex);
            UnityEngine.Object.DestroyImmediate(tempGO);
            AssetDatabase.Refresh();
            latestScreenshotPath = path;
            Debug.Log($"[SceneView Clean Camera Capture] Saved to: {path}");
        }

        // --- Helper Methods ---
        private static void EnsureDirectoryExists()
        {
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
        }

        private static string GenerateScreenshotPath(string prefix)
        {
            var currentTime = DateTime.Now;
            var filename = $"{prefix}_" + currentTime.ToString().Replace('/', '-').Replace(':', '_') + ".png";
            return directory + filename;
        }

        private static string GetResolution()
        {
            Vector2 size = Handles.GetMainGameViewSize();
            Vector2Int sizeInt = new Vector2Int((int)size.x, (int)size.y);
            return $"{sizeInt.x}x{sizeInt.y}";
        }

        private static void OnSceneViewGuiCaptureOnce(SceneView sv)
        {
            if (Event.current.type != EventType.Repaint || string.IsNullOrEmpty(sPath))
                return;
            try
            {
                var srcCam = sv.camera;
                if (srcCam == null)
                {
                    SceneView.duringSceneGui -= OnSceneViewGuiCaptureOnce;
                    return;
                }
                int pw = Mathf.Max(1, srcCam.pixelWidth);
                int ph = Mathf.Max(1, srcCam.pixelHeight);
                var rect = new Rect(0, 0, pw, ph);
                var mode = sv.cameraMode.drawMode;
                if (!Enum.IsDefined(typeof(DrawCameraMode), mode) || mode == 0)
                    mode = DrawCameraMode.Textured;
                Handles.DrawCamera(rect, srcCam, mode, !sTempHideGizmos);
                Handles.DrawCamera(rect, srcCam, mode, false);
                var tl = HandleUtility.GUIPointToScreenPixelCoordinate(rect.min);
                var br = HandleUtility.GUIPointToScreenPixelCoordinate(rect.max);
                int px = Mathf.RoundToInt(Mathf.Min(tl.x, br.x));
                int py = Mathf.RoundToInt(Mathf.Min(tl.y, br.y));
                int rw = Mathf.RoundToInt(Mathf.Abs(br.x - tl.x));
                int rh = Mathf.RoundToInt(Mathf.Abs(br.y - tl.y));
                if (rw > 0 && rh > 0)
                {
                    var tex = new Texture2D(rw, rh, TextureFormat.RGBA32, false, false);
                    var prev = RenderTexture.active;
                    RenderTexture.active = null;
                    tex.ReadPixels(new Rect(px, py, rw, rh), 0, 0);
                    tex.Apply(false, false);
                    RenderTexture.active = prev;
                    File.WriteAllBytes(sPath, tex.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(tex);
                    AssetDatabase.Refresh();
                    Debug.Log($"[SceneView Capture] Saved to: {sPath}");
                }
            }
            finally
            {
                SceneView.duringSceneGui -= OnSceneViewGuiCaptureOnce;
            }
        }
    }
}