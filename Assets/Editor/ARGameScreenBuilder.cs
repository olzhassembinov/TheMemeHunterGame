using MemeHunter.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MemeHunter.Editor
{
    public static class ARGameScreenBuilder
    {
        const string ScenePath = "Assets/Scenes/SampleScene.unity";
        const string UiPrefabFolder = "Assets/Prefabs/UI";
        const string FontPath = "Assets/MobileARTemplateAssets/UI/Fonts/Inter-Regular_SDF.asset";
        const string ActionSpritePath = "Assets/mst_files/UI/Blue Buttons/Button.png";
        const string PlaceholderSpritePath = "Assets/mst_files/UI/Etc/Profile Photo.png";

        [MenuItem("Meme Hunter/AR/Build State-Driven AR Game UI")]
        public static void Build()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var existing = GameObject.Find("ARGame UI Overlay");
            if (existing != null)
                Object.DestroyImmediate(existing);

            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            var placeholder = AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderSpritePath);
            var overlay = new GameObject("ARGame UI Overlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = overlay.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 25;

            var scaler = overlay.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(498f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var uiRoot = MakeRect("AR Game Screen", overlay.transform, Vector2.zero);
            Stretch((RectTransform)uiRoot.transform);

            var scanPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(UiPrefabFolder + "/ARScanCentre.prefab");
            var scan = (GameObject)PrefabUtility.InstantiatePrefab(scanPrefab, uiRoot.transform);
            scan.name = "Scan Centre";
            PlaceCenter((RectTransform)scan.transform, Vector2.zero, new Vector2(203f, 206f));

            var primaryStatus = MakeText("Primary Status", uiRoot.transform, 45f, MemeHunterUiColors.DarkNavy, new Vector2(430f, 60f));
            PlaceTop((RectTransform)primaryStatus.transform, 0f, 256f, new Vector2(430f, 60f));
            primaryStatus.alignment = TextAlignmentOptions.Center;

            var secondaryStatus = MakeText("Secondary Status", uiRoot.transform, 45f, MemeHunterUiColors.DarkNavy, new Vector2(430f, 60f));
            PlaceTop((RectTransform)secondaryStatus.transform, 0f, 318f, new Vector2(430f, 60f));
            secondaryStatus.alignment = TextAlignmentOptions.Center;

            var sideBar = MakeRect("Freeze Thumbnail Sidebar", uiRoot.transform, new Vector2(100f, 550f));
            var sideBarRect = (RectTransform)sideBar.transform;
            sideBarRect.anchorMin = new Vector2(1f, 0.5f);
            sideBarRect.anchorMax = new Vector2(1f, 0.5f);
            sideBarRect.pivot = new Vector2(1f, 0.5f);
            sideBarRect.anchoredPosition = new Vector2(-10f, 0f);
            sideBarRect.sizeDelta = new Vector2(100f, 550f);

            var sideThumbnails = new Image[6];
            for (var index = 0; index < sideThumbnails.Length; index++)
            {
                var thumbnail = MakeImage("Meme Thumbnail " + (index + 1), sideBar.transform, placeholder, new Vector2(75f, 75f));
                var rect = (RectTransform)thumbnail.transform;
                rect.anchorMin = new Vector2(0.5f, 1f);
                rect.anchorMax = new Vector2(0.5f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.anchoredPosition = new Vector2(0f, -10f - index * 88f);
                rect.sizeDelta = new Vector2(75f, 75f);
                sideThumbnails[index] = thumbnail;
            }

            var stateThumbnail = MakeImage("State Meme Thumbnail", uiRoot.transform, placeholder, new Vector2(75f, 75f));
            PlaceTop((RectTransform)stateThumbnail.transform, 0f, 680f, new Vector2(75f, 75f));

            var buttonObject = MakeRect("AR Action Button", uiRoot.transform, new Vector2(185f, 58f));
            PlaceTop((RectTransform)buttonObject.transform, 0f, 908f, new Vector2(185f, 58f));
            var background = buttonObject.AddComponent<Image>();
            background.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ActionSpritePath);
            background.color = Color.white;
            background.preserveAspect = false;
            var actionButton = buttonObject.AddComponent<Button>();
            actionButton.targetGraphic = background;
            actionButton.transition = Selectable.Transition.None;
            actionButton.interactable = false;

            var actionLabel = MakeText("Action Label", buttonObject.transform, 35f, new Color32(128, 128, 128, 255), Vector2.zero);
            Stretch(actionLabel.rectTransform);
            actionLabel.alignment = TextAlignmentOptions.Center;
            actionLabel.text = "Not yet...";

            var controller = uiRoot.AddComponent<ARGameUIController>();
            SetReference(controller, "freezeSidebar", sideBar);
            SetReferences(controller, "sidebarThumbnails", sideThumbnails);
            SetReference(controller, "stateThumbnail", stateThumbnail);
            SetReference(controller, "primaryStatusText", primaryStatus);
            SetReference(controller, "secondaryStatusText", secondaryStatus);
            SetReference(controller, "actionButton", actionButton);
            SetReference(controller, "actionButtonLabel", actionLabel);
            SetReference(controller, "actionButtonBackground", background);
            SetEnum(controller, "initialState", ARGameUIState.Freeze);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Built one state-driven AR game UI overlay in SampleScene.unity.");
        }

        static GameObject MakeRect(string name, Transform parent, Vector2 size)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            root.transform.SetParent(parent, false);
            ((RectTransform)root.transform).sizeDelta = size;
            return root;
        }

        static Image MakeImage(string name, Transform parent, Sprite sprite, Vector2 size)
        {
            var root = MakeRect(name, parent, size);
            var image = root.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        static TextMeshProUGUI MakeText(string name, Transform parent, float size, Color color, Vector2 rectSize)
        {
            var root = MakeRect(name, parent, rectSize);
            var text = root.AddComponent<TextMeshProUGUI>();
            text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            text.fontSize = size;
            text.color = color;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            return text;
        }

        static void PlaceCenter(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        static void PlaceTop(RectTransform rect, float x, float top, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(x, -top);
            rect.sizeDelta = size;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static void SetReference(Object target, string propertyName, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetReferences<T>(Object target, string propertyName, T[] values) where T : Object
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(propertyName);
            property.arraySize = values.Length;
            for (var index = 0; index < values.Length; index++)
                property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetEnum(Object target, string propertyName, System.Enum value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).enumValueIndex = System.Convert.ToInt32(value);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
