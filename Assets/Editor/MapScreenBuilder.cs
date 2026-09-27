using System.IO;
using MemeHunter.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace MemeHunter.Editor
{
    public static class MapScreenBuilder
    {
        const string MapDataFolder = "Assets/Resources/Map";
        const string CollectionPath = MapDataFolder + "/SampleMapLocations.asset";
        const string FontPath = "Assets/MobileARTemplateAssets/UI/Fonts/Inter-Regular_SDF.asset";
        const string UiPrefabFolder = "Assets/Prefabs/UI";

        [MenuItem("Meme Hunter/Map/Build Map Screen")]
        public static void Build()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Profile.unity", OpenSceneMode.Single);
            var screens = GameObject.Find("Screens")?.transform;
            var profile = screens != null ? screens.Find("Profile") : null;
            if (screens == null || profile == null)
                throw new MissingReferenceException("Profile scene must contain Screens and Profile roots.");

            var navigator = screens.GetComponent<ScreenNavigator>();
            if (navigator == null)
                navigator = screens.gameObject.AddComponent<ScreenNavigator>();

            var existingScreen = screens.Find("Map");
            if (existingScreen != null)
                Object.DestroyImmediate(existingScreen.gameObject);

            var locations = BuildSampleLocations();
            var mapScreen = MakeRect("Map", screens, Vector2.zero);
            Stretch((RectTransform)mapScreen.transform);
            mapScreen.SetActive(false);
            var screenView = mapScreen.AddComponent<ScreenView>();
            SetString(screenView, "screenId", "Map");

            BuildPlaceholderMap(mapScreen.transform);
            BuildToolRail(mapScreen.transform);
            BuildLocationMarkers(mapScreen.transform, locations);
            AddMapBack(mapScreen.transform, navigator);
            AddProfileMapButton(profile, navigator);

            MakeScreenRegistry(screens, navigator);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Built map placeholder screen with five rarity-driven data markers.");
        }

        static MapLocationCollectionData BuildSampleLocations()
        {
            Directory.CreateDirectory(MapDataFolder);
            var common = AssetDatabase.LoadAssetAtPath<MemeData>("Assets/Resources/Memes/THIS-IS-FINE.asset");
            var uncommon = AssetDatabase.LoadAssetAtPath<MemeData>("Assets/Resources/Memes/GIGACHAD.asset");
            var data = AssetDatabase.LoadAssetAtPath<MapLocationCollectionData>(CollectionPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<MapLocationCollectionData>();
                AssetDatabase.CreateAsset(data, CollectionPath);
            }

            var serialized = new SerializedObject(data);
            var locations = serialized.FindProperty("locations");
            locations.arraySize = 5;
            var positions = new[]
            {
                new Vector2(0.15f, 0.77f),
                new Vector2(0.58f, 0.72f),
                new Vector2(0.34f, 0.51f),
                new Vector2(0.67f, 0.34f),
                new Vector2(0.21f, 0.23f)
            };

            for (var index = 0; index < locations.arraySize; index++)
            {
                var entry = locations.GetArrayElementAtIndex(index);
                entry.FindPropertyRelative("meme").objectReferenceValue = index == 3 ? uncommon : common;
                entry.FindPropertyRelative("normalizedPosition").vector2Value = positions[index];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
            return data;
        }

        static void BuildPlaceholderMap(Transform parent)
        {
            var baseMap = MakeImage("Map Placeholder Base", parent, null, new Vector2(498f, 1080f), new Color32(195, 223, 198, 255));
            Stretch((RectTransform)baseMap.transform);

            var mapArea = MakeRect("Placeholder Map Artwork", parent, Vector2.zero);
            Stretch((RectTransform)mapArea.transform);

            var parks = new[]
            {
                new Vector2(-112f, 225f), new Vector2(38f, 125f), new Vector2(-145f, -40f),
                new Vector2(64f, -212f), new Vector2(-82f, -345f)
            };
            var parkSizes = new[]
            {
                new Vector2(132f, 74f), new Vector2(96f, 112f), new Vector2(114f, 70f),
                new Vector2(144f, 88f), new Vector2(110f, 92f)
            };
            for (var index = 0; index < parks.Length; index++)
            {
                var park = MakeImage("Map Green Area " + (index + 1), mapArea.transform, null, parkSizes[index], new Color32(177, 216, 180, 255));
                PlaceCenter((RectTransform)park.transform, parks[index], parkSizes[index]);
            }

            var roads = new[]
            {
                (new Vector2(-24f, 310f), new Vector2(460f, 20f), -8f),
                (new Vector2(-50f, 155f), new Vector2(475f, 17f), 9f),
                (new Vector2(-18f, -15f), new Vector2(490f, 22f), -5f),
                (new Vector2(-30f, -180f), new Vector2(475f, 18f), 11f),
                (new Vector2(60f, -350f), new Vector2(470f, 20f), -7f),
                (new Vector2(-175f, 0f), new Vector2(20f, 1100f), 7f),
                (new Vector2(-16f, -4f), new Vector2(19f, 1100f), -6f),
                (new Vector2(145f, 4f), new Vector2(18f, 1080f), 8f)
            };
            for (var index = 0; index < roads.Length; index++)
            {
                var road = MakeImage("Map Road " + (index + 1), mapArea.transform, null, roads[index].Item2, new Color32(246, 250, 241, 255));
                PlaceCenter((RectTransform)road.transform, roads[index].Item1, roads[index].Item2);
                road.rectTransform.localEulerAngles = new Vector3(0f, 0f, roads[index].Item3);
            }

            var mapTitle = MakeText("Map Placeholder Label", mapArea.transform, "MAP", MemeHunterUiColors.DarkNavy, 22f, new Vector2(80f, 30f));
            var titleRect = mapTitle.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 0f);
            titleRect.anchorMax = new Vector2(0f, 0f);
            titleRect.pivot = Vector2.zero;
            titleRect.anchoredPosition = new Vector2(22f, 24f);
            titleRect.sizeDelta = new Vector2(80f, 30f);
            mapTitle.color = new Color(MemeHunterUiColors.DarkNavy.r, MemeHunterUiColors.DarkNavy.g, MemeHunterUiColors.DarkNavy.b, 0.58f);
        }

        static void BuildToolRail(Transform parent)
        {
            var rail = MakeRect("Map Tool Sidebar", parent, new Vector2(100f, 366f));
            var railRect = (RectTransform)rail.transform;
            railRect.anchorMin = new Vector2(1f, 0.5f);
            railRect.anchorMax = new Vector2(1f, 0.5f);
            railRect.pivot = new Vector2(1f, 0.5f);
            railRect.anchoredPosition = new Vector2(-8f, 0f);
            railRect.sizeDelta = new Vector2(100f, 366f);

            var backing = rail.AddComponent<RoundedRectGraphic>();
            backing.color = new Color(1f, 1f, 1f, 0.76f);
            var sprites = new[]
            {
                "Assets/mst_files/UI/Blue Buttons/Map.png",
                "Assets/mst_files/UI/Blue Buttons/Menu.png",
                "Assets/mst_files/UI/Blue Buttons/Profile.png",
                "Assets/mst_files/UI/Blue Buttons/MemeHunter.png"
            };

            for (var index = 0; index < sprites.Length; index++)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(sprites[index]);
                var image = MakeImage("Map Tool " + (index + 1), rail.transform, sprite, new Vector2(75f, 75f), Color.white);
                var rect = (RectTransform)image.transform;
                rect.anchorMin = new Vector2(0.5f, 1f);
                rect.anchorMax = new Vector2(0.5f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.anchoredPosition = new Vector2(0f, -9f - index * 88f);
                rect.sizeDelta = new Vector2(75f, 75f);
            }
        }

        static void BuildLocationMarkers(Transform parent, MapLocationCollectionData data)
        {
            var markerArea = MakeRect("Meme Location Marker Area", parent, Vector2.zero);
            var markerAreaRect = (RectTransform)markerArea.transform;
            markerAreaRect.anchorMin = Vector2.zero;
            markerAreaRect.anchorMax = new Vector2(0.79f, 1f);
            markerAreaRect.offsetMin = Vector2.zero;
            markerAreaRect.offsetMax = Vector2.zero;

            var presenter = parent.gameObject.AddComponent<MapScreenPresenter>();
            var dotPrefab = MakeLocationDotPrefab();
            SetReference(presenter, "locationData", data);
            SetReference(presenter, "markerPrefab", dotPrefab.GetComponent<MemeLocationDot>());
            SetReference(presenter, "markerArea", markerAreaRect);

            var profileFolder = "Assets/Prefabs/UI";
            PrefabUtility.SaveAsPrefabAsset(dotPrefab, profileFolder + "/MemeLocationDot.prefab");
            Object.DestroyImmediate(dotPrefab);
            AssetDatabase.ImportAsset(profileFolder + "/MemeLocationDot.prefab");
            var savedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(profileFolder + "/MemeLocationDot.prefab");
            SetReference(presenter, "markerPrefab", savedPrefab.GetComponent<MemeLocationDot>());
        }

        static GameObject MakeLocationDotPrefab()
        {
            var dot = MakeRect("Meme Location Dot", null, new Vector2(27f, 27f));
            var graphic = dot.AddComponent<RoundedRectGraphic>();
            graphic.color = MemeHunterUiColors.CommonGrey;
            graphic.raycastTarget = true;
            dot.AddComponent<Button>();
            var marker = dot.AddComponent<MemeLocationDot>();
            SetReference(marker, "dotGraphic", graphic);
            return dot;
        }

        static void AddMapBack(Transform parent, ScreenNavigator navigator)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(UiPrefabFolder + "/CircularActionButton.prefab");
            var back = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            back.name = "Map Back";
            PlaceTop((RectTransform)back.transform, -199f, 15f, new Vector2(63f, 63f));
            var icon = back.transform.Find("Icon Slot").GetComponent<Image>();
            icon.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/mst_files/UI/Blue Buttons/Discard button.png");
            icon.enabled = true;
            ((RectTransform)icon.transform).sizeDelta = new Vector2(42f, 42f);
            var action = back.AddComponent<ScreenNavigationButton>();
            SetReference(action, "circularButton", back.GetComponent<CircularActionButton>());
            SetReference(action, "navigator", navigator);
            SetString(action, "destinationScreenId", "Profile");
        }

        static void MakeScreenRegistry(Transform screens, ScreenNavigator navigator)
        {
            var views = screens.GetComponentsInChildren<ScreenView>(true);
            SetReferences(navigator, "screens", views);
        }

        static void AddProfileMapButton(Transform profile, ScreenNavigator navigator)
        {
            var existing = profile.Find("Map Navigation");
            if (existing != null)
                Object.DestroyImmediate(existing.gameObject);

            var buttonRoot = MakeRect("Map Navigation", profile, new Vector2(36f, 36f));
            PlaceTop((RectTransform)buttonRoot.transform, 118f, 435f, new Vector2(36f, 36f));
            var image = buttonRoot.AddComponent<Image>();
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/mst_files/UI/Blue Buttons/Map.png");
            image.preserveAspect = true;
            var button = buttonRoot.AddComponent<Button>();
            button.targetGraphic = image;
            var action = buttonRoot.AddComponent<ScreenNavigationButton>();
            SetReference(action, "button", button);
            SetReference(action, "navigator", navigator);
            SetString(action, "destinationScreenId", "Map");
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

        static GameObject MakeRect(string name, Transform parent, Vector2 size)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            root.transform.SetParent(parent, false);
            ((RectTransform)root.transform).sizeDelta = size;
            return root;
        }

        static Image MakeImage(string name, Transform parent, Sprite sprite, Vector2 size, Color color)
        {
            var root = MakeRect(name, parent, size);
            var image = root.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        static TextMeshProUGUI MakeText(string name, Transform parent, string value, Color color, float size, Vector2 dimensions)
        {
            var root = MakeRect(name, parent, dimensions);
            var text = root.AddComponent<TextMeshProUGUI>();
            text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            return text;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
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
    }
}
