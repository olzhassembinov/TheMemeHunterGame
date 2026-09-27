using System.IO;
using MemeHunter.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace MemeHunter.Editor
{
    public static class NotificationScreenBuilder
    {
        const string ScreenId = "Notifications";
        const string DataFolder = "Assets/Resources/Notifications";
        const string CollectionPath = DataFolder + "/SampleNotifications.asset";
        const string NotificationPath = DataFolder + "/SampleNotification.asset";
        const string PrefabPath = "Assets/Prefabs/UI/NotificationCard.prefab";
        const string FontPath = "Assets/MobileARTemplateAssets/UI/Fonts/Inter-Regular_SDF.asset";

        static TMP_FontAsset font;

        [MenuItem("Meme Hunter/Settings/Build Notifications Screen")]
        public static void Build()
        {
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            UiComponentPrefabBuilder.BuildNotificationItemPrefab();
            var collection = BuildSampleCollection();
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Profile.unity", OpenSceneMode.Single);
            var screens = GameObject.Find("Screens")?.transform;
            var persistent = GameObject.Find("Persistent UI")?.transform;
            if (screens == null || persistent == null)
                throw new MissingReferenceException("Profile scene must contain Screens and Persistent UI roots.");

            var profile = screens.Find("Profile");
            if (profile == null)
                throw new MissingReferenceException("Profile screen root was not found.");

            var existingScreen = screens.Find("Notifications");
            if (existingScreen != null)
                Object.DestroyImmediate(existingScreen.gameObject);

            var navigator = screens.GetComponent<ScreenNavigator>();
            if (navigator == null)
                navigator = screens.gameObject.AddComponent<ScreenNavigator>();

            var screen = MakeRect("Notifications", screens, Vector2.zero);
            Stretch((RectTransform)screen.transform);
            screen.SetActive(false);
            var screenView = screen.AddComponent<ScreenView>();
            SetString(screenView, "screenId", ScreenId);

            var back = AddBackButton(screen.transform, navigator);
            back.transform.SetAsLastSibling();

            var title = MakeText("Notifications Header", screen.transform, "NOTIFICATIONS", MemeHunterUiColors.DarkNavy, 45f, 34f, new Vector2(330f, 56f));
            ((RectTransform)title.transform).anchoredPosition += new Vector2(20f, 0f);
            var leftLine = AddPrefab("PageDecorationLine", screen.transform, "Notifications Line Left");
            var rightLine = AddPrefab("PageDecorationLine", screen.transform, "Notifications Line Right");
            SetTop((RectTransform)leftLine.transform, -199f, 94f, new Vector2(78f, 1f));
            SetTop((RectTransform)rightLine.transform, 199f, 94f, new Vector2(78f, 1f));
            leftLine.GetComponent<Image>().color = MemeHunterUiColors.DarkNavy;
            rightLine.GetComponent<Image>().color = MemeHunterUiColors.DarkNavy;

            var viewport = MakeRect("Notification Scroll Viewport", screen.transform, new Vector2(459f, 962f));
            SetTop((RectTransform)viewport.transform, 0f, 118f, new Vector2(459f, 962f));
            viewport.AddComponent<RectMask2D>();
            var scrollRect = viewport.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.inertia = true;

            var content = MakeRect("Notification Content", viewport.transform, new Vector2(458f, 980f));
            var contentRect = (RectTransform)content.transform;
            contentRect.anchorMin = new Vector2(0.5f, 1f);
            contentRect.anchorMax = new Vector2(0.5f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(458f, 980f);
            scrollRect.viewport = (RectTransform)viewport.transform;
            scrollRect.content = contentRect;

            var itemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (itemPrefab == null)
                throw new FileNotFoundException("Notification item prefab was not found: " + PrefabPath);
            var presenter = screen.AddComponent<NotificationListPresenter>();
            SetReference(presenter, "collection", collection);
            SetReference(presenter, "itemPrefab", itemPrefab.GetComponent<NotificationItemView>());
            SetReference(presenter, "contentRoot", contentRect);
            SetFloat(presenter, "itemSpacing", 24f);
            SetFloat(presenter, "trailingContentPadding", 64f);

            AddBottomFade(viewport.transform);
            AddProfileNotificationsButton(profile, navigator);

            var views = screens.GetComponentsInChildren<ScreenView>(true);
            System.Array.Sort(views, (left, right) => string.CompareOrdinal(left.ScreenId, right.ScreenId));
            SetReferences(navigator, "screens", views);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Built collection-backed Notifications screen in Profile.unity.");
        }

        static NotificationCollectionData BuildSampleCollection()
        {
            Directory.CreateDirectory(DataFolder);
            var thumbnail = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/mst_files/UI/Etc/Profile Photo.png");
            var notification = AssetDatabase.LoadAssetAtPath<NotificationData>(NotificationPath);
            if (notification == null)
            {
                notification = ScriptableObject.CreateInstance<NotificationData>();
                AssetDatabase.CreateAsset(notification, NotificationPath);
            }

            var notificationSerialized = new SerializedObject(notification);
            notificationSerialized.FindProperty("title").stringValue = "Some event going on";
            notificationSerialized.FindProperty("description").stringValue = "oumaigaaa what is that";
            notificationSerialized.FindProperty("timestamp").stringValue = "06:08";
            notificationSerialized.FindProperty("thumbnail").objectReferenceValue = thumbnail;
            notificationSerialized.FindProperty("unread").boolValue = false;
            notificationSerialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(notification);

            var collection = AssetDatabase.LoadAssetAtPath<NotificationCollectionData>(CollectionPath);
            if (collection == null)
            {
                collection = ScriptableObject.CreateInstance<NotificationCollectionData>();
                AssetDatabase.CreateAsset(collection, CollectionPath);
            }

            var collectionSerialized = new SerializedObject(collection);
            var entries = collectionSerialized.FindProperty("notifications");
            entries.arraySize = 10;
            for (var index = 0; index < entries.arraySize; index++)
                entries.GetArrayElementAtIndex(index).objectReferenceValue = notification;
            collectionSerialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(collection);
            return collection;
        }

        static void AddBottomFade(Transform viewport)
        {
            var fade = MakeRect("Notification Scroll Fade", viewport, new Vector2(459f, 72f));
            var rect = (RectTransform)fade.transform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, 72f);
            var image = fade.AddComponent<Image>();
            image.color = Color.white;
            image.raycastTarget = false;
            var gradient = fade.AddComponent<VerticalGradientEffect>();
            gradient.SetColors(new Color(0.765f, 0.961f, 1f, 0f), MemeHunterUiColors.LightCyanBackground);
        }

        static void AddProfileNotificationsButton(Transform profile, ScreenNavigator navigator)
        {
            var existing = profile.Find("Notifications Navigation");
            if (existing != null)
                Object.DestroyImmediate(existing.gameObject);

            var root = MakeRect("Notifications Navigation", profile, new Vector2(36f, 36f));
            SetTop((RectTransform)root.transform, 166f, 435f, new Vector2(36f, 36f));
            var image = root.AddComponent<Image>();
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/mst_files/UI/Blue Buttons/Notifications.png");
            image.preserveAspect = true;
            var button = root.AddComponent<Button>();
            button.targetGraphic = image;
            var action = root.AddComponent<ScreenNavigationButton>();
            SetReference(action, "button", button);
            SetReference(action, "navigator", navigator);
            SetString(action, "destinationScreenId", ScreenId);
        }

        static GameObject AddBackButton(Transform parent, ScreenNavigator navigator)
        {
            var root = AddPrefab("CircularActionButton", parent, "Notifications Back");
            SetTop((RectTransform)root.transform, -199f, 15f, new Vector2(63f, 63f));
            var icon = root.transform.Find("Icon Slot").GetComponent<Image>();
            icon.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/mst_files/UI/Blue Buttons/Discard button.png");
            icon.enabled = true;
            ((RectTransform)icon.transform).sizeDelta = new Vector2(42f, 42f);
            var action = root.AddComponent<ScreenNavigationButton>();
            SetReference(action, "circularButton", root.GetComponent<CircularActionButton>());
            SetReference(action, "navigator", navigator);
            SetString(action, "destinationScreenId", "Profile");
            return root;
        }

        static GameObject AddPrefab(string prefabName, Transform parent, string objectName)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/" + prefabName + ".prefab");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = objectName;
            return instance;
        }

        static TMP_Text MakeText(string name, Transform parent, string value, Color color, float size, float top, Vector2 dimensions)
        {
            var root = MakeRect(name, parent, dimensions);
            SetTop((RectTransform)root.transform, 0f, top, dimensions);
            var text = root.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            text.text = value;
            return text;
        }

        static GameObject MakeRect(string name, Transform parent, Vector2 size)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            root.transform.SetParent(parent, false);
            ((RectTransform)root.transform).sizeDelta = size;
            return root;
        }

        static void SetTop(RectTransform rect, float x, float top, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(x, -top);
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
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

        static void SetString(Object target, string propertyName, string value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetFloat(Object target, string propertyName, float value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
