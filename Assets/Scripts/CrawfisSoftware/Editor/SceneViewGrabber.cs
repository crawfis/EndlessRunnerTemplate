using UnityEditor;
using UnityEngine;
using System;
using System.IO;

public class SceneViewGrabber : EditorWindow
{
    string pendingPath;
    static bool hideGizmos = true; // Default to hiding gizmos
    static string path;

    [MenuItem("Crawfis/Scene View → Capture (Current Size)")]
    static void Open() => GetWindow<SceneViewGrabber>("SV Capture");

    void OnGUI()
    {
        hideGizmos = EditorGUILayout.Toggle("Hide Gizmos/Handles", hideGizmos);

        if (GUILayout.Button("Capture & Save…"))
        {
            var sv = SceneView.lastActiveSceneView;
            if (sv == null || sv.camera == null)
            {
                EditorUtility.DisplayDialog("SceneView Capture", "Open a Scene view first.", "OK");
                return;
            }
            pendingPath = EditorUtility.SaveFilePanel("Save SceneView Capture", "", "SceneView.png", "png");
            if (!string.IsNullOrEmpty(pendingPath))
            {
                // Pass the current values as local variables to the handler
                SceneView.duringSceneGui += OnSceneViewGui_CaptureOnce;
                sv.Repaint();
            }
        }
    }

    private void SceneView_duringSceneGui(SceneView obj)
    {
        throw new NotImplementedException();
    }

    static void OnSceneViewGui_CaptureOnce(SceneView sv)
    {
        if (Event.current.type != EventType.Repaint || string.IsNullOrEmpty(path))
            return;
        try
        {
            var srcCam = sv.camera;
            if (srcCam == null)
            {
                SceneView.duringSceneGui -= OnSceneViewGui_CaptureOnce;
                return;
            }

            int pw = Mathf.Max(1, srcCam.pixelWidth);
            int ph = Mathf.Max(1, srcCam.pixelHeight);
            var rect = new Rect(0, 0, pw, ph);

            var mode = sv.cameraMode.drawMode;
            if (!Enum.IsDefined(typeof(DrawCameraMode), mode) || mode == 0)
                mode = DrawCameraMode.Textured;

            Handles.DrawCamera(rect, srcCam, mode, !hideGizmos);

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

                File.WriteAllBytes(path, tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);
                AssetDatabase.Refresh();
                Debug.Log($"[SceneView Capture] Saved to: {path}");
            }
        }
        finally
        {
            SceneView.duringSceneGui -= OnSceneViewGui_CaptureOnce;
        }
    }
}