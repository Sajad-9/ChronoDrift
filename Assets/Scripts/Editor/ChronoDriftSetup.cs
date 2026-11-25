using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ChronoDrift.Audio;
using ChronoDrift.Combat;
using ChronoDrift.Core;
using ChronoDrift.Player;
using ChronoDrift.Progression;
using ChronoDrift.UI;

namespace ChronoDrift.Editor
{
    /// <summary>
    /// Editor automation for ChronoDrift: registers custom tags, builds Main.unity,
    /// and compiles a standalone Windows executable.
    /// </summary>
    public static class ChronoDriftSetup
    {
        private const string ScenePath = "Assets/Scenes/Main.unity";

        [MenuItem("ChronoDrift/Build Main Scene")]
        public static void BuildSceneMenu()
        {
            BuildScene();
        }

        public static void BuildAll()
        {
            BuildScene();
            BuildWindowsPlayer();
        }

        public static void BuildScene()
        {
            EnsureTags(new[] { "Wall", "Obstacle", "EnemyShield" });

            if (!Directory.Exists("Assets/Scenes"))
                Directory.CreateDirectory("Assets/Scenes");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Render & ambient lighting
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.14f, 0.18f, 0.28f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.02f, 0.03f, 0.06f);
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 35f;
            RenderSettings.fogEndDistance = 70f;

            // Main Camera
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.03f, 0.06f);
            cam.fieldOfView = 54f;
            camGo.AddComponent<AudioListener>();
            var camCtrl = camGo.AddComponent<CameraController>();
            camGo.transform.position = new Vector3(0f, 23f, -11.5f);
            camGo.transform.rotation = Quaternion.Euler(63f, 0f, 0f);

            // Directional Light
            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.82f, 0.92f, 1f);
            light.intensity = 1.15f;
            lightGo.transform.rotation = Quaternion.Euler(55f, -30f, 0f);

            // Player
            var playerGo = new GameObject("Player");
            playerGo.transform.position = new Vector3(0f, 0.6f, 0f);
            playerGo.AddComponent<PlayerController>();
            camCtrl.target = playerGo.transform;

            // Core Game Systems
            var sysGo = new GameObject("GameSystems");
            sysGo.AddComponent<GameManager>();
            sysGo.AddComponent<TimeDilationManager>();
            sysGo.AddComponent<WaveManager>();
            sysGo.AddComponent<ProceduralAudio>();
            sysGo.AddComponent<GameUI>();
            sysGo.AddComponent<AchievementSystem>();

            // Screen Effects on Camera
            camGo.AddComponent<ScreenEffects>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ChronoDrift] Built Main.unity successfully.");
        }

        [MenuItem("ChronoDrift/Build Windows Executable")]
        public static void BuildWindowsPlayer()
        {
            string buildDir = "Builds";
            if (!Directory.Exists(buildDir))
                Directory.CreateDirectory(buildDir);

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = Path.Combine(buildDir, "ChronoDrift.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.LogError($"[ChronoDrift] Build failed: {report.summary.result} ({report.summary.totalErrors} errors)");
                EditorApplication.Exit(1);
            }
            else
            {
                Debug.Log($"[ChronoDrift] Windows build succeeded: {options.locationPathName} ({report.summary.totalSize / (1024 * 1024)} MB)");
            }
        }

        private static void EnsureTags(string[] requiredTags)
        {
            var tagManagerAsset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (tagManagerAsset == null || tagManagerAsset.Length == 0) return;

            var so = new SerializedObject(tagManagerAsset[0]);
            var tagsProp = so.FindProperty("tags");

            foreach (string tag in requiredTags)
            {
                bool exists = false;
                for (int i = 0; i < tagsProp.arraySize; i++)
                {
                    if (tagsProp.GetArrayElementAtIndex(i).stringValue == tag)
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
                    tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
                }
            }

            so.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
        }
    }
}
