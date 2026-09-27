using System.IO;
using MemeHunter.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace MemeHunter.Editor
{
    public static class PersistentChromeBuilder
    {
        const string ChromeRootName = "Persistent Bottom Chrome";
        const string PrefabPath = "Assets/Prefabs/UI/PersistentBottomDecoration.prefab";
        const string LineSpritePath = "Assets/mst_files/UI/Etc/Page Decoration Line.png";
        const string StartScenePath = "Assets/Scenes/WelcomeScreen.unity";
        const string ProfileScenePath = "Assets/Scenes/Profile.unity";

        [MenuItem("Meme Hunter/UI/Build Persistent Bottom Decoration")]
        public static void Build()
        {
            var prefab = BuildPrefab();
            BuildWelcomeReference(prefab);
            RemoveSceneCopy(ProfileScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Built one persistent bottom decoration prefab owned by AppUiRoot.");
        }

        static GameObject BuildPrefab()
        {
            Directory.CreateDirectory("Assets/Prefabs/UI");
            var rootName = "Persistent Bottom Decoration";
            var layoutRoot = MakeRect(rootName, null, new Vector2(250f, 1f));
            var lineSprite = AssetDatabase.LoadAssetAtPath<Sprite>(LineSpritePath);
            var leftLine = MakeLine("Left Decorative Line", layoutRoot.transform, lineSprite);
            var rightLine = MakeLine("Right Decorative Line", layoutRoot.transform, lineSprite);

            var decoration = layoutRoot.AddComponent<PersistentBottomDecoration>();
            SetReference(decoration, "layoutRoot", layoutRoot.transform);
            SetReference(decoration, "leftLineRect", leftLine.rectTransform);
            SetReference(decoration, "rightLineRect", rightLine.rectTransform);
            SetReference(decoration, "leftLine", leftLine);
            SetReference(decoration, "rightLine", rightLine);
            SetFloat(decoration, "lineWidth", 109f);
            SetFloat(decoration, "lineHeight", 1f);
            SetFloat(decoration, "centerGap", 32f);
            SetFloat(decoration, "bottomInset", 16f);
            SetColor(decoration, "lineColor", MemeHunterUiColors.DarkNavy);
            SetBool(decoration, "persistAcrossScenes", true);

            var prefab = PrefabUtility.SaveAsPrefabAsset(layoutRoot, PrefabPath);
            Object.DestroyImmediate(layoutRoot);
            return prefab;
        }

        static void BuildWelcomeReference(GameObject prefab)
        {
            var scene = EditorSceneManager.OpenScene(StartScenePath, OpenSceneMode.Single);
            var existing = GameObject.Find(ChromeRootName);
            if (existing != null)
                Object.DestroyImmediate(existing);

            var appUiRoot = Object.FindFirstObjectByType<AppUiRoot>();
            if (appUiRoot == null)
                throw new MissingReferenceException("Welcome scene AppUiRoot was not found.");
            SetReference(appUiRoot, "persistentBottomDecorationPrefab", prefab);

            var continueObject = GameObject.Find("Continue Button");
            if (continueObject != null)
            {
                var continueButton = continueObject.GetComponent<Button>();
                if (continueButton == null)
                    continueButton = continueObject.AddComponent<Button>();
                var targetGraphic = continueObject.GetComponent<Image>();
                if (targetGraphic != null)
                    continueButton.targetGraphic = targetGraphic;

                var sceneLoadButton = continueButton.GetComponent<SceneLoadButton>();
                if (sceneLoadButton == null)
                    sceneLoadButton = continueButton.gameObject.AddComponent<SceneLoadButton>();
                SetReference(sceneLoadButton, "button", continueButton);
                SetString(sceneLoadButton, "sceneName", "Profile");
            }
            else
            {
                Debug.LogWarning("Welcome Continue Button object was not found.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        static void RemoveSceneCopy(string scenePath)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var existing = GameObject.Find(ChromeRootName);
            if (existing != null)
                Object.DestroyImmediate(existing);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        static Image MakeLine(string objectName, Transform parent, Sprite sprite)
        {
            var line = MakeRect(objectName, parent, new Vector2(109f, 1f));
            var image = line.AddComponent<Image>();
            image.sprite = sprite;
            image.color = MemeHunterUiColors.DarkNavy;
            image.preserveAspect = false;
            image.raycastTarget = false;
            return image;
        }

        static GameObject MakeRect(string objectName, Transform parent, Vector2 size)
        {
            var root = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer));
            root.transform.SetParent(parent, false);
            ((RectTransform)root.transform).sizeDelta = size;
            return root;
        }

        static void SetReference(Object target, string propertyName, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetFloat(Object target, string propertyName, float value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetColor(Object target, string propertyName, Color value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).colorValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetBool(Object target, string propertyName, bool value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).boolValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetString(Object target, string propertyName, string value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
