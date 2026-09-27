using System.IO;
using MemeHunter.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MemeHunter.Editor
{
    public static class MemeScreensBuilder
    {
        const string UiPrefabFolder = "Assets/Prefabs/UI";
        const string MemeFolder = "Assets/Resources/Memes";
        const string FontPath = "Assets/MobileARTemplateAssets/UI/Fonts/Inter-Regular_SDF.asset";
        const string Description = "Bunny hopping (bhopping) is a popular technique from CS:source, which started in 1996 as an accidental movement glitch in Quake when speedrunners discovered that air acceleration and jump-chaining bypassed ground speed caps. This bug was constantly used by a legendary player Phoon, who was both accused of being cheater and being professional CS source player.";

        static TMP_FontAsset font;

        [MenuItem("Meme Hunter/Profile/Build Meme Card and Collections Screens")]
        public static void BuildScreens()
        {
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            var memes = BuildMemeDataAssets();
            var collection = BuildCollectionAsset(memes);
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Profile.unity", OpenSceneMode.Single);
            var screensRoot = GameObject.Find("Screens")?.transform;
            var persistentRoot = GameObject.Find("Persistent UI")?.transform;
            if (screensRoot == null || persistentRoot == null)
                throw new MissingReferenceException("Profile scene must contain Screens and Persistent UI roots.");

            var profileScreen = screensRoot.Find("Profile");
            if (profileScreen == null)
                throw new MissingReferenceException("Profile screen root was not found.");

            DestroyChildIfPresent(screensRoot, "Meme Card");
            DestroyChildIfPresent(screensRoot, "Collections");

            var navigator = screensRoot.GetComponent<ScreenNavigator>();
            if (navigator == null)
                navigator = screensRoot.gameObject.AddComponent<ScreenNavigator>();

            var profileView = profileScreen.GetComponent<ScreenView>();
            if (profileView == null)
                profileView = profileScreen.gameObject.AddComponent<ScreenView>();
            SetString(profileView, "screenId", "Profile");
            profileScreen.gameObject.SetActive(true);

            var memeCardScreen = MakeScreen("Meme Card", screensRoot, "MemeCard", false);
            var collectionsScreen = MakeScreen("Collections", screensRoot, "Collections", false);

            var detailPresenter = BuildMemeCardScreen(memeCardScreen, persistentRoot, navigator, memes[0]);
            var collectionPresenter = BuildCollectionsScreen(collectionsScreen, persistentRoot, navigator, detailPresenter, collection);

            SetReferences(navigator, "screens", screensRoot.GetComponentsInChildren<ScreenView>(true));
            SetString(navigator, "initialScreenId", "Profile");

            var profilePresenter = profileScreen.GetComponent<ProfileScreenPresenter>();
            if (profilePresenter != null)
                SetString(profilePresenter, "backSceneName", "WelcomeScreen");

            var profileCardTransform = profileScreen.Find("Meme Card Area/Card Content/B-HOPPING");
            if (profileCardTransform != null)
            {
                var cardButton = profileCardTransform.GetComponent<Button>();
                if (cardButton == null)
                    cardButton = profileCardTransform.gameObject.AddComponent<Button>();
                cardButton.targetGraphic = profileCardTransform.GetComponentInChildren<Image>();
                var previousDetailOpen = profileCardTransform.GetComponent<MemeDetailOpenButton>();
                if (previousDetailOpen != null)
                    Object.DestroyImmediate(previousDetailOpen);
                var detailOpen = profileCardTransform.gameObject.AddComponent<MemeDetailOpenButton>();
                SetReference(detailOpen, "button", cardButton);
                SetReference(detailOpen, "memeData", memes[0]);
                SetReference(detailOpen, "detailPresenter", detailPresenter);
                SetReference(detailOpen, "navigator", navigator);
            }

            AddProfileCollectionsButton(profileScreen, navigator);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Built data-driven Meme Card and Collections screens in Profile.unity.");
        }

        static MemeData[] BuildMemeDataAssets()
        {
            Directory.CreateDirectory(MemeFolder);
            return new[]
            {
                CreateMeme("B-HOPPING", "B-HOPPING", MemeRarity.Legendary, 37, Description),
                CreateMeme("DOGE", "DOGE", MemeRarity.Rare, 0, string.Empty),
                CreateMeme("THIS-IS-FINE", "THIS IS FINE.", MemeRarity.Common, 0, string.Empty),
                CreateMeme("GIGACHAD", "GIGACHAD", MemeRarity.Uncommon, 0, string.Empty)
            };
        }

        static MemeData CreateMeme(string id, string title, MemeRarity rarity, int price, string description)
        {
            var path = MemeFolder + "/" + id + ".asset";
            var data = AssetDatabase.LoadAssetAtPath<MemeData>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<MemeData>();
                AssetDatabase.CreateAsset(data, path);
            }

            var serialized = new SerializedObject(data);
            serialized.FindProperty("memeId").stringValue = id;
            serialized.FindProperty("displayName").stringValue = title;
            serialized.FindProperty("rarity").enumValueIndex = (int)rarity;
            serialized.FindProperty("price").intValue = price;
            serialized.FindProperty("description").stringValue = description;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
            return data;
        }

        static MemeCollectionData BuildCollectionAsset(MemeData[] memes)
        {
            const string path = MemeFolder + "/PlayerCollection.asset";
            var collection = AssetDatabase.LoadAssetAtPath<MemeCollectionData>(path);
            if (collection == null)
            {
                collection = ScriptableObject.CreateInstance<MemeCollectionData>();
                AssetDatabase.CreateAsset(collection, path);
            }

            var serialized = new SerializedObject(collection);
            var memeArray = serialized.FindProperty("memes");
            memeArray.arraySize = memes.Length;
            for (var index = 0; index < memes.Length; index++)
                memeArray.GetArrayElementAtIndex(index).objectReferenceValue = memes[index];
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(collection);
            return collection;
        }

        static GameObject MakeScreen(string objectName, Transform parent, string screenId, bool active)
        {
            var screen = MakeRect(objectName, parent, Vector2.zero);
            Stretch((RectTransform)screen.transform);
            screen.SetActive(active);
            var view = screen.AddComponent<ScreenView>();
            SetString(view, "screenId", screenId);
            return screen;
        }

        static MemeDetailPresenter BuildMemeCardScreen(GameObject screen, Transform persistentRoot, ScreenNavigator navigator, MemeData initialData)
        {
            var detailBack = AddBackButton(screen.transform, "Meme Card Back", "Profile", navigator, -199f, 15f);
            detailBack.transform.SetAsLastSibling();

            var leftLine = AddPrefab("PageDecorationLine", screen.transform, "Meme Card Header Line Left");
            var rightLine = AddPrefab("PageDecorationLine", screen.transform, "Meme Card Header Line Right");
            SetTop((RectTransform)leftLine.transform, -153f, 85f, new Vector2(109f, 1f));
            SetTop((RectTransform)rightLine.transform, 153f, 85f, new Vector2(109f, 1f));
            leftLine.GetComponent<Image>().color = MemeHunterUiColors.LightPurple;
            rightLine.GetComponent<Image>().color = MemeHunterUiColors.LightPurple;
            var header = Text("Meme Card Header", screen.transform, "MEME CARD", MemeHunterUiColors.LightPurple, 15f, 72f, new Vector2(160f, 22f));

            var title = Text("Meme Name", screen.transform, initialData.DisplayName, MemeHunterUiColors.DarkNavy, 50f, 109f, new Vector2(460f, 60f));
            var card = AddPrefab("MemeCardFull", screen.transform, "Meme Card Artwork");
            SetTop((RectTransform)card.transform, 0f, 170f, new Vector2(383f, 432f));
            var cardView = card.GetComponent<MemeCardView>();

            var rarityLabel = Text("Rarity Label", screen.transform, "Rarity:", MemeHunterUiColors.DarkNavy, 20f, 610f, new Vector2(110f, 28f));
            var rarityValue = Text("Rarity Value", screen.transform, string.Empty, MemeHunterUiColors.BrightGreen, 20f, 610f, new Vector2(230f, 28f));
            ((RectTransform)rarityLabel.transform).anchoredPosition += new Vector2(-150f, 0f);
            ((RectTransform)rarityValue.transform).anchoredPosition += new Vector2(95f, 0f);
            var priceLabel = Text("Price Label", screen.transform, "Price:", MemeHunterUiColors.DarkNavy, 20f, 644f, new Vector2(110f, 28f));
            var priceValue = Text("Price Value", screen.transform, string.Empty, MemeHunterUiColors.BrightGreen, 20f, 644f, new Vector2(230f, 28f));
            ((RectTransform)priceLabel.transform).anchoredPosition += new Vector2(-150f, 0f);
            ((RectTransform)priceValue.transform).anchoredPosition += new Vector2(95f, 0f);

            var description = Text("Meme Description", screen.transform, string.Empty, MemeHunterUiColors.DarkNavy, 18f, 686f, new Vector2(458f, 350f));
            description.alignment = TextAlignmentOptions.TopLeft;
            description.textWrappingMode = TextWrappingModes.Normal;
            description.overflowMode = TextOverflowModes.Overflow;

            var presenter = screen.AddComponent<MemeDetailPresenter>();
            SetReference(presenter, "memeData", initialData);
            SetReference(presenter, "titleText", title);
            SetReference(presenter, "memeCard", cardView);
            SetReference(presenter, "rarityValueText", rarityValue);
            SetReference(presenter, "priceValueText", priceValue);
            SetReference(presenter, "descriptionText", description);
            SetReference(presenter, "navigator", navigator);
            return presenter;
        }

        static CollectionScreenPresenter BuildCollectionsScreen(GameObject screen, Transform persistentRoot, ScreenNavigator navigator, MemeDetailPresenter detailPresenter, MemeCollectionData collection)
        {
            var back = AddBackButton(screen.transform, "Collections Back", "Profile", navigator, -199f, 15f);
            back.transform.SetAsLastSibling();
            var title = Text("Collections Header", screen.transform, "COLLECTIONS", MemeHunterUiColors.DarkNavy, 45f, 86f, new Vector2(440f, 56f));
            var leftLine = AddPrefab("PageDecorationLine", screen.transform, "Collections Line Left");
            var rightLine = AddPrefab("PageDecorationLine", screen.transform, "Collections Line Right");
            SetTop((RectTransform)leftLine.transform, -194f, 147f, new Vector2(78f, 1f));
            SetTop((RectTransform)rightLine.transform, 194f, 147f, new Vector2(78f, 1f));
            leftLine.GetComponent<Image>().color = MemeHunterUiColors.LightPurple;
            rightLine.GetComponent<Image>().color = MemeHunterUiColors.LightPurple;

            var filterRoot = MakeRect("Rarity Filters", screen.transform, new Vector2(480f, 102f));
            SetTop((RectTransform)filterRoot.transform, 0f, 162f, new Vector2(480f, 102f));
            var rarityValues = new[] { MemeRarity.Common, MemeRarity.Uncommon, MemeRarity.Rare, MemeRarity.Legendary };
            var rarityLabels = new[] { "COMMON", "UNCOMMON", "RARE", "LEGENDARY" };
            var raritySprites = new[]
            {
                "Assets/mst_files/UI/Rarity/Common Circle.png",
                "Assets/mst_files/UI/Rarity/Uncommon Circle.png",
                "Assets/mst_files/UI/Rarity/Rare Circle.png",
                "Assets/mst_files/UI/Rarity/Legendary Circle.png"
            };
            var rarityButtons = new Button[rarityValues.Length];
            for (var index = 0; index < rarityValues.Length; index++)
            {
                var x = -180f + index * 120f;
                var filterItem = MakeRect(rarityLabels[index], filterRoot.transform, new Vector2(112f, 92f));
                SetTop((RectTransform)filterItem.transform, x, 0f, new Vector2(112f, 92f));
                var circle = Image("Rarity Circle", filterItem.transform, new Vector2(58f, 58f), AssetDatabase.LoadAssetAtPath<Sprite>(raritySprites[index]), Color.white);
                var circleRect = (RectTransform)circle.transform;
                circleRect.anchorMin = new Vector2(0.5f, 1f);
                circleRect.anchorMax = new Vector2(0.5f, 1f);
                circleRect.pivot = new Vector2(0.5f, 1f);
                circleRect.anchoredPosition = new Vector2(0f, 0f);
                circleRect.sizeDelta = new Vector2(58f, 58f);
                circle.raycastTarget = false;
                var label = Text("Rarity Label", filterItem.transform, rarityLabels[index], MemeHunterUiColors.DarkNavy, index == 3 ? 20f : 15f, 62f, new Vector2(118f, 25f));
                var target = filterItem.AddComponent<Image>();
                target.color = new Color(1f, 1f, 1f, 0f);
                var button = filterItem.AddComponent<Button>();
                button.targetGraphic = target;
                rarityButtons[index] = button;
            }

            var galleryViewport = MakeRect("Collection Gallery", screen.transform, new Vector2(480f, 760f));
            SetTop((RectTransform)galleryViewport.transform, 0f, 274f, new Vector2(480f, 760f));
            galleryViewport.AddComponent<RectMask2D>();
            var scroll = galleryViewport.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            var content = MakeRect("Collection Content", galleryViewport.transform, new Vector2(480f, 1570f));
            var contentRect = (RectTransform)content.transform;
            contentRect.anchorMin = new Vector2(0.5f, 1f);
            contentRect.anchorMax = new Vector2(0.5f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(480f, 1570f);
            scroll.viewport = (RectTransform)galleryViewport.transform;
            scroll.content = contentRect;

            var cards = new MemeCardView[collection.Memes.Length];
            var emptySlots = new GameObject[5];
            for (var index = 0; index < collection.Memes.Length + emptySlots.Length; index++)
            {
                var column = index % 2;
                var row = index / 2;
                var x = column == 0 ? -120f : 120f;
                var y = -18f - row * 310f;
                if (index < collection.Memes.Length)
                {
                    var card = AddPrefab("MemeCardPreview", content.transform, collection.Memes[index].DisplayName);
                    var rect = (RectTransform)card.transform;
                    rect.anchorMin = new Vector2(0.5f, 1f);
                    rect.anchorMax = new Vector2(0.5f, 1f);
                    rect.pivot = new Vector2(0.5f, 1f);
                    rect.anchoredPosition = new Vector2(x, y);
                    rect.sizeDelta = new Vector2(159f, 132f);
                    var cardView = card.GetComponent<MemeCardView>();
                    cardView.Bind(collection.Memes[index]);
                    var frame = card.GetComponent<Image>();
                    var cardButton = card.AddComponent<Button>();
                    cardButton.targetGraphic = frame;
                    cards[index] = cardView;
                }
                else
                {
                    var empty = AddPrefab("EmptyMemeCard", content.transform, "Empty Collection Slot " + (index - collection.Memes.Length + 1));
                    var rect = (RectTransform)empty.transform;
                    rect.anchorMin = new Vector2(0.5f, 1f);
                    rect.anchorMax = new Vector2(0.5f, 1f);
                    rect.pivot = new Vector2(0.5f, 1f);
                    rect.anchoredPosition = new Vector2(x, y);
                    rect.localScale = Vector3.one * 0.72f;
                    emptySlots[index - collection.Memes.Length] = empty;
                    if (index == collection.Memes.Length)
                    {
                        var outlineSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/mst_files/UI/Etc/Choose Meme Outline.png");
                        var outline = Image("Choose Meme Outline", empty.transform, new Vector2(78f, 78f), outlineSprite, Color.white);
                        outline.raycastTarget = false;
                    }
                }
            }

            var fadeObject = MakeRect("Gallery Scroll Fade", galleryViewport.transform, new Vector2(480f, 64f));
            var fadeRect = (RectTransform)fadeObject.transform;
            fadeRect.anchorMin = new Vector2(0f, 0f);
            fadeRect.anchorMax = new Vector2(1f, 0f);
            fadeRect.pivot = new Vector2(0.5f, 0f);
            fadeRect.anchoredPosition = Vector2.zero;
            fadeRect.sizeDelta = new Vector2(0f, 64f);
            var fadeGraphic = fadeObject.AddComponent<Image>();
            fadeGraphic.color = Color.white;
            fadeGraphic.raycastTarget = false;
            var fade = fadeObject.AddComponent<VerticalGradientEffect>();
            fade.SetColors(new Color(0.765f, 0.961f, 1f, 0f), MemeHunterUiColors.LightCyanBackground);

            var presenter = screen.AddComponent<CollectionScreenPresenter>();
            SetReference(presenter, "collectionData", collection);
            SetReferences(presenter, "memeCards", cards);
            SetReferences(presenter, "emptySlots", emptySlots);
            SetReferences(presenter, "rarityButtons", rarityButtons);
            SetEnumArray(presenter, "buttonRarities", rarityValues);
            SetReference(presenter, "navigator", navigator);
            SetReference(presenter, "detailPresenter", detailPresenter);
            return presenter;
        }

        static void AddProfileCollectionsButton(Transform profileScreen, ScreenNavigator navigator)
        {
            var existing = profileScreen.Find("Collections Navigation");
            if (existing != null)
                Object.DestroyImmediate(existing.gameObject);

            var menuSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/mst_files/UI/Blue Buttons/Menu.png");
            var root = MakeRect("Collections Navigation", profileScreen, new Vector2(36f, 36f));
            SetTop((RectTransform)root.transform, 214f, 435f, new Vector2(36f, 36f));
            var image = root.AddComponent<Image>();
            image.sprite = menuSprite;
            image.preserveAspect = true;
            var button = root.AddComponent<Button>();
            button.targetGraphic = image;
            var action = root.AddComponent<ScreenNavigationButton>();
            SetReference(action, "button", button);
            SetReference(action, "navigator", navigator);
            SetString(action, "destinationScreenId", "Collections");
        }

        static void DestroyChildIfPresent(Transform parent, string childName)
        {
            var child = parent.Find(childName);
            if (child != null)
                Object.DestroyImmediate(child.gameObject);
        }

        static GameObject AddBackButton(Transform parent, string name, string destination, ScreenNavigator navigator, float x, float top)
        {
            var root = AddPrefab("CircularActionButton", parent, name);
            SetTop((RectTransform)root.transform, x, top, new Vector2(63f, 63f));
            var icon = root.transform.Find("Icon Slot").GetComponent<Image>();
            icon.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/mst_files/UI/Blue Buttons/Discard button.png");
            icon.enabled = true;
            ((RectTransform)icon.transform).sizeDelta = new Vector2(42f, 42f);
            var action = root.AddComponent<ScreenNavigationButton>();
            SetReference(action, "circularButton", root.GetComponent<CircularActionButton>());
            SetReference(action, "navigator", navigator);
            SetString(action, "destinationScreenId", destination);
            return root;
        }

        static GameObject AddPrefab(string prefabName, Transform parent, string objectName)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(UiPrefabFolder + "/" + prefabName + ".prefab");
            if (prefab == null)
                throw new FileNotFoundException("Missing reusable UI prefab: " + prefabName);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = objectName;
            return instance;
        }

        static TMP_Text Text(string name, Transform parent, string value, Color color, float size, float top, Vector2 dimensions)
        {
            var gameObject = MakeRect(name, parent, dimensions);
            SetTop((RectTransform)gameObject.transform, 0f, top, dimensions);
            var text = gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            text.text = value;
            return text;
        }

        static Image Image(string name, Transform parent, Vector2 size, Sprite sprite, Color color)
        {
            var gameObject = MakeRect(name, parent, size);
            var image = gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.preserveAspect = true;
            return image;
        }

        static GameObject MakeRect(string name, Transform parent, Vector2 size)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            gameObject.transform.SetParent(parent, false);
            ((RectTransform)gameObject.transform).sizeDelta = size;
            return gameObject;
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

        static void SetEnumArray(Object target, string propertyName, MemeRarity[] values)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(propertyName);
            property.arraySize = values.Length;
            for (var index = 0; index < values.Length; index++)
                property.GetArrayElementAtIndex(index).enumValueIndex = (int)values[index];
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
