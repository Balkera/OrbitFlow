using SquareFlow.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SquareFlow.Editor
{
    public static class SquareFlowSceneBuilder
    {
        private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";

        [MenuItem("Square Flow/Rebuild Scene")]
        public static void RebuildScene()
        {
            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.GetActiveScene();
            if (scene.path != SampleScenePath)
                scene = EditorSceneManager.OpenScene(SampleScenePath);

            if (!scene.IsValid() || scene.path != SampleScenePath)
            {
                Debug.LogError("Square Flow scene rebuild failed because SampleScene could not be opened at " + SampleScenePath + ".");
                return;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = roots.Length - 1; i >= 0; i--)
                Object.DestroyImmediate(roots[i]);

            CreateMainCamera();
            CreateEventSystem();
            GameObject root = CreateSquareFlowRoot();

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                Debug.LogError("Square Flow scene rebuild failed because SampleScene could not be saved at " + SampleScenePath + ".");
                return;
            }

            Selection.activeGameObject = root;
        }

        private static void CreateMainCamera()
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(14, 19, 32, 255);

            cameraObject.AddComponent<AudioListener>();
        }

        private static void CreateEventSystem()
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();

#if ENABLE_INPUT_SYSTEM
            System.Type inputModuleType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputModuleType != null)
            {
                eventSystemObject.AddComponent(inputModuleType);
                return;
            }

            Debug.LogWarning("Square Flow scene was rebuilt without InputSystemUIInputModule because Unity.InputSystem is not available to this editor assembly.");
#elif ENABLE_LEGACY_INPUT_MANAGER
            eventSystemObject.AddComponent<StandaloneInputModule>();
#else
            Debug.LogWarning("Square Flow scene was rebuilt without an EventSystem input module because no supported input backend is enabled.");
#endif
        }

        private static GameObject CreateSquareFlowRoot()
        {
            GameObject root = new GameObject("SquareFlowRoot");
            root.AddComponent<SquareFlowGameController>();
            return root;
        }
    }
}
