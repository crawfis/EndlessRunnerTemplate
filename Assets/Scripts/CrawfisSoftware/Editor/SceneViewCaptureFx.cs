// Editor/SceneViewCapture_ApplyRestore.cs
// Unity 6000.x. Editor-only. No URP/HDRP compile deps.
// Features:
// - Apply settings directly to the active SceneView (gizmos, solid background color, disable post FX, global fog).
// - Capture at current SceneView size/state.
// - Restore everything exactly.
//
// Usage: Window → Scene View → Apply/Restore + Capture

using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using System.Reflection;

public class SceneViewCapture_ApplyRestore : EditorWindow
{
    // --- User knobs (APPLY mutates the SceneView immediately) ---
    public bool showGizmos = true;
    public bool solidBackground = false;
    public Color backgroundColor = Color.black;
    public bool disablePostFX = true;   // URP/HDRP/PPSv2 via reflection (if present)
    public bool disableGlobalFog = false;  // Built-in RP global fog

    // --- Saved state for RESTORE ---
    static bool s_HasSaved;
    static bool s_PrevShowGizmos;
    static CameraClearFlags s_PrevClearFlags;
    static Color s_PrevBgColor;
    static bool s_PrevGlobalFog;
    static bool s_PrevSkyboxEnabled;

    // Per-pipeline post FX state (reflection)
    struct PostFXState
    {
        public Component ppLayerComp; public bool ppLayerPrevEnabled; public int ppLayerPrevLayerMask;
        public Component urpCamDataComp; public bool urpPrevRenderPost; public int urpPrevVolumeMask;
        public Component hdrpCamDataComp; public int hdrpPrevVolumeMask;
    }
    static PostFXState s_PPState;

    [MenuItem("Window/Scene View → Apply/Restore + Capture")]
    static void Open() => GetWindow<SceneViewCapture_ApplyRestore>("SV Apply/Restore");

    void OnGUI()
    {
        GUILayout.Label("Settings to APPLY to SceneView", EditorStyles.boldLabel);
        showGizmos = EditorGUILayout.Toggle("Show Gizmos/Handles", showGizmos);
        solidBackground = EditorGUILayout.Toggle("Use Solid Background", solidBackground);
        //using (new EditorGUI.DisabledScope(!solidBackground))
        backgroundColor = EditorGUILayout.ColorField("Background Color", backgroundColor);
        disablePostFX = EditorGUILayout.Toggle("Disable Post-Processing", disablePostFX);
        disableGlobalFog = EditorGUILayout.Toggle("Disable Global Fog (Built-in)", disableGlobalFog);

        GUILayout.Space(8);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Apply Settings"))
                ApplySettings();

            if (GUILayout.Button("Capture & Save…"))
                CaptureCurrentSceneView();

            using (new EditorGUI.DisabledScope(!s_HasSaved))
            {
                if (GUILayout.Button("Restore Settings"))
                    RestoreSettings();
            }
        }
    }

    // ----------------------- APPLY -----------------------
    void ApplySettings()
    {
        var sv = SceneView.lastActiveSceneView;
        if (sv == null || sv.camera == null)
        {
            EditorUtility.DisplayDialog("SceneView", "Open a Scene view first.", "OK");
            return;
        }

        var cam = sv.camera;

        // Save once (for Restore)
        if (!s_HasSaved)
        {
            s_PrevShowGizmos = sv.drawGizmos;
            s_PrevClearFlags = cam.clearFlags;
            s_PrevBgColor = cam.backgroundColor;
            s_PrevGlobalFog = RenderSettings.fog;
            TryGetSkyboxEnabled(sv, out s_PrevSkyboxEnabled);
            SavePostFXState(cam, ref s_PPState);
            s_HasSaved = true;
        }

        // Gizmos
        sv.drawGizmos = showGizmos;

        // Background: disable SceneView Skybox, force solid color clear
        if (solidBackground)
        {
            TrySetSkyboxEnabled(sv, false);                // KEY: stop skybox clear
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = backgroundColor;
        }

        // Fog (Built-in)
        if (disableGlobalFog) RenderSettings.fog = false;

        // Post FX off on SceneView camera (URP/HDRP/PPSv2 if present)
        if (disablePostFX) DisableAllPostFX(cam);

        // Double-repaint to guarantee SceneView applies flags before you capture
        sv.Repaint();
        EditorApplication.delayCall += () => sv.Repaint();
    }

    // ----------------------- RESTORE -----------------------
    void RestoreSettings()
    {
        var sv = SceneView.lastActiveSceneView;
        if (!s_HasSaved || sv == null || sv.camera == null) return;

        var cam = sv.camera;

        sv.drawGizmos = s_PrevShowGizmos;
        cam.clearFlags = s_PrevClearFlags;
        cam.backgroundColor = s_PrevBgColor;
        RenderSettings.fog = s_PrevGlobalFog;
        TrySetSkyboxEnabled(sv, s_PrevSkyboxEnabled);
        RestoreAllPostFX(cam, in s_PPState);

        s_HasSaved = false;
        sv.Repaint();
    }

    // ----------------------- CAPTURE -----------------------
    string _pendingPath;

    void CaptureCurrentSceneView()
    {
        var sv = SceneView.lastActiveSceneView;
        if (sv == null || sv.camera == null)
        {
            EditorUtility.DisplayDialog("SceneView", "Open a Scene view first.", "OK");
            return;
        }
        _pendingPath = EditorUtility.SaveFilePanel("Save SceneView Capture", "", "SceneView.png", "png");
        if (string.IsNullOrEmpty(_pendingPath)) return;

        SceneView.duringSceneGui += OnSceneViewRepaintOnce;
        sv.Repaint();
    }

    void OnSceneViewRepaintOnce(SceneView sv)
    {
        if (Event.current.type != EventType.Repaint || string.IsNullOrEmpty(_pendingPath))
            return;

        try
        {
            var cam = sv.camera;
            if (cam == null) return;

            // Use current SceneView pixel size
            int pw = Mathf.Max(1, cam.pixelWidth);
            int ph = Mathf.Max(1, cam.pixelHeight);
            var rect = new Rect(0, 0, pw, ph);

            // Draw with current draw mode & current gizmo state (no extra toggles here)
            var mode = sv.cameraMode.drawMode;
            if (!Enum.IsDefined(typeof(DrawCameraMode), mode) || mode == 0)
                mode = DrawCameraMode.Textured;

            Handles.DrawCamera(rect, cam, mode, sv.drawGizmos);

            // Read back (normalize GUI→screen)
            var tl = HandleUtility.GUIPointToScreenPixelCoordinate(rect.min);
            var br = HandleUtility.GUIPointToScreenPixelCoordinate(rect.max);
            int px = Mathf.RoundToInt(Mathf.Min(tl.x, br.x));
            int py = Mathf.RoundToInt(Mathf.Min(tl.y, br.y));
            int rw = Mathf.RoundToInt(Mathf.Abs(br.x - tl.x));
            int rh = Mathf.RoundToInt(Mathf.Abs(br.y - tl.y));
            if (rw <= 0 || rh <= 0) return;

            var tex = new Texture2D(rw, rh, TextureFormat.RGBA32, false, false);
            var prev = RenderTexture.active; // null = backbuffer
            RenderTexture.active = null;
            tex.ReadPixels(new Rect(px, py, rw, rh), 0, 0);
            tex.Apply(false, false);
            RenderTexture.active = prev;

            File.WriteAllBytes(_pendingPath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            AssetDatabase.Refresh();
            Debug.Log($"[SceneView Capture] Saved to: {_pendingPath}");
        }
        finally
        {
            SceneView.duringSceneGui -= OnSceneViewRepaintOnce;
            _pendingPath = null;
        }
    }

    // ----------------------- SceneView Skybox helpers -----------------------
    static bool TryGetSkyboxEnabled(SceneView sv, out bool value)
    {
        value = false;
        var p = typeof(SceneView).GetProperty("sceneViewState",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (p == null) return false;
        var state = p.GetValue(sv, null);
        if (state == null) return false;

        var t = state.GetType();
        var f = t.GetField("skyboxEnabled",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
         ?? t.GetField("showSkybox",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (f == null) return false;

        value = (bool)f.GetValue(state);
        return true;
    }

    static bool TrySetSkyboxEnabled(SceneView sv, bool on)
    {
        var p = typeof(SceneView).GetProperty("sceneViewState",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (p == null) return false;
        var state = p.GetValue(sv, null);
        if (state == null) return false;

        var t = state.GetType();
        var f = t.GetField("skyboxEnabled",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
         ?? t.GetField("showSkybox",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (f == null) return false;

        f.SetValue(state, on);
        return true;
    }

    // ------------------ Post FX helpers (reflection; no URP/HDRP deps) ------------------
    static void SavePostFXState(Camera cam, ref PostFXState st)
    {
        // PPS v2
        var ppType = Type.GetType("UnityEngine.Rendering.PostProcessing.PostProcessLayer, Unity.Postprocessing.Runtime");
        if (ppType != null)
        {
            var comp = cam.GetComponent(ppType);
            if (comp != null)
            {
                st.ppLayerComp = comp;
                var enabledProp = ppType.GetProperty("enabled");
                if (enabledProp != null) st.ppLayerPrevEnabled = (bool)enabledProp.GetValue(comp, null);
                var volField = ppType.GetField("volumeLayer", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (volField != null) st.ppLayerPrevLayerMask = ((LayerMask)volField.GetValue(comp)).value;
            }
        }

        // URP
        var urpType = Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
        if (urpType != null)
        {
            var comp = cam.GetComponent(urpType);
            if (comp != null)
            {
                st.urpCamDataComp = comp;
                var rppProp = urpType.GetProperty("renderPostProcessing", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var maskProp = urpType.GetProperty("volumeLayerMask", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (rppProp != null) st.urpPrevRenderPost = (bool)rppProp.GetValue(comp, null);
                if (maskProp != null) st.urpPrevVolumeMask = (int)maskProp.GetValue(comp, null);
            }
        }

        // HDRP
        var hdrpType = Type.GetType("UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData, Unity.RenderPipelines.HighDefinition.Runtime");
        if (hdrpType != null)
        {
            var comp = cam.GetComponent(hdrpType);
            if (comp != null)
            {
                st.hdrpCamDataComp = comp;
                var maskProp = hdrpType.GetProperty("volumeLayerMask", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (maskProp != null) st.hdrpPrevVolumeMask = (int)maskProp.GetValue(comp, null);
            }
        }
    }

    static void DisableAllPostFX(Camera cam)
    {
        // PPS v2
        var ppType = Type.GetType("UnityEngine.Rendering.PostProcessing.PostProcessLayer, Unity.Postprocessing.Runtime");
        if (ppType != null)
        {
            var comp = cam.GetComponent(ppType);
            if (comp != null)
            {
                var enabledProp = ppType.GetProperty("enabled");
                if (enabledProp != null) enabledProp.SetValue(comp, false, null);
                var volField = ppType.GetField("volumeLayer", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (volField != null) volField.SetValue(comp, (LayerMask)0);
            }
        }

        // URP
        var urpType = Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
        if (urpType != null)
        {
            var comp = cam.GetComponent(urpType);
            if (comp != null)
            {
                var rppProp = urpType.GetProperty("renderPostProcessing", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var maskProp = urpType.GetProperty("volumeLayerMask", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (rppProp != null) rppProp.SetValue(comp, false, null);
                if (maskProp != null) maskProp.SetValue(comp, 0, null);
            }
        }

        // HDRP
        var hdrpType = Type.GetType("UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData, Unity.RenderPipelines.HighDefinition.Runtime");
        if (hdrpType != null)
        {
            var comp = cam.GetComponent(hdrpType);
            if (comp != null)
            {
                var maskProp = hdrpType.GetProperty("volumeLayerMask", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (maskProp != null) maskProp.SetValue(comp, 0, null);
            }
        }
    }

    static void RestoreAllPostFX(Camera cam, in PostFXState st)
    {
        // PPS v2
        if (st.ppLayerComp != null)
        {
            var t = st.ppLayerComp.GetType();
            var enabledProp = t.GetProperty("enabled");
            if (enabledProp != null) enabledProp.SetValue(st.ppLayerComp, st.ppLayerPrevEnabled, null);
            var volField = t.GetField("volumeLayer", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (volField != null) volField.SetValue(st.ppLayerComp, (LayerMask)st.ppLayerPrevLayerMask);
        }

        // URP
        if (st.urpCamDataComp != null)
        {
            var t = st.urpCamDataComp.GetType();
            var rppProp = t.GetProperty("renderPostProcessing", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var maskProp = t.GetProperty("volumeLayerMask", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (rppProp != null) rppProp.SetValue(st.urpCamDataComp, st.urpPrevRenderPost, null);
            if (maskProp != null) maskProp.SetValue(st.urpCamDataComp, st.urpPrevVolumeMask, null);
        }

        // HDRP
        if (st.hdrpCamDataComp != null)
        {
            var t = st.hdrpCamDataComp.GetType();
            var maskProp = t.GetProperty("volumeLayerMask", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (maskProp != null) maskProp.SetValue(st.hdrpCamDataComp, st.hdrpPrevVolumeMask, null);
        }
    }
}
