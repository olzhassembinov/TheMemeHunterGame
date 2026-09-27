using System.IO;
using MemeHunter.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine;
using UnityEngine.UI;

namespace MemeHunter.Editor
{
    public static class UiComponentPrefabBuilder
    {
        const string PrefabFolder = "Assets/Prefabs/UI";
        const string FontPath = "Assets/MobileARTemplateAssets/UI/Fonts/Inter-Regular_SDF.asset";

        static TMP_FontAsset font;

        [MenuItem("Meme Hunter/UI/Generate Component Prefabs")]
        public static void Generate()
        {
            Directory.CreateDirectory(PrefabFolder);
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

            BuildGradientButton();
            BuildCircularActionButton();
            BuildPageDecorationLine();
            BuildRarityCircle();
            BuildEmptyMemeCard();
            BuildMemeCard("MemeCardPreview", "Assets/mst_files/UI/Etc/Preview Meme Card.png", new Vector2(159f, 132f));
            BuildMemeCard("MemeCardFull", "Assets/mst_files/UI/Etc/Meme Card.png", new Vector2(383f, 432f));
            BuildProfilePhoto();
            BuildNotificationCard();
            BuildScanCentre();
            BuildFormInput();

            AssetDatabase.SaveAssets();
            ValidateGeneratedPrefabs();
            AssetDatabase.Refresh();
            Debug.Log("Generated reusable Meme Hunter UI prefabs in " + PrefabFolder);
        }

        public static void BuildNotificationItemPrefab()
        {
            Directory.CreateDirectory(PrefabFolder);
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            BuildNotificationCard();
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Meme Hunter/Profile/Build Profile Scene")]
        public static void BuildProfileScene()
        {
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            var data = BuildProfileDataAsset();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var canvasObject = new GameObject("Profile Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = false;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(498f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystemObject.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();

            var background = CreateImage("Cyan Background", canvasObject.transform, new Vector2(498f, 1080f), null, MemeHunterUiColors.LightCyanBackground);
            Stretch((RectTransform)background.transform);
            var backgroundArt = CreateImage("Background Artwork", canvasObject.transform, new Vector2(498f, 1080f), Sprite("Assets/mst_files/UI/Etc/Colorful background.png"), new Color(1f, 1f, 1f, 0.08f));
            backgroundArt.preserveAspect = false;
            backgroundArt.raycastTarget = false;
            Stretch((RectTransform)backgroundArt.transform);

            var safeAreaObject = CreateRect("Safe Area", canvasObject.transform, Vector2.zero);
            var safeArea = safeAreaObject.AddComponent<SafeAreaFitter>();
            SetReference(safeArea, "target", safeAreaObject.transform);
            Stretch((RectTransform)safeAreaObject.transform);

            var screensRoot = CreateRect("Screens", safeAreaObject.transform, Vector2.zero);
            Stretch((RectTransform)screensRoot.transform);
            var persistentRoot = CreateRect("Persistent UI", safeAreaObject.transform, Vector2.zero);
            Stretch((RectTransform)persistentRoot.transform);

            var profileScreen = CreateRect("Profile", screensRoot.transform, Vector2.zero);
            Stretch((RectTransform)profileScreen.transform);

            var profilePhotoPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/ProfilePhoto.prefab");
            var profilePhotoObject = InstantiatePrefab(profilePhotoPrefab, profileScreen.transform, "Profile Photo");
            SetTopRect((RectTransform)profilePhotoObject.transform, 0f, 12f, new Vector2(183f, 183f));

            var nickname = MakeProfileText("Nickname", profileScreen.transform, "DigItalitE", MemeHunterUiColors.DarkNavy, 40f, 194f, new Vector2(440f, 48f));
            var subtitle = MakeProfileText("Age and City", profileScreen.transform, "Age 10, Astana", MemeHunterUiColors.DarkNavy, 16f, 240f, new Vector2(420f, 23f));
            var level = MakeProfileText("Level Value", profileScreen.transform, "12", MemeHunterUiColors.DarkNavy, 65f, 260f, new Vector2(160f, 74f));
            var levelLabel = MakeProfileText("Level Label", profileScreen.transform, "LEVEL", MemeHunterUiColors.DarkNavy, 15f, 329f, new Vector2(120f, 19f));

            var xpTrackObject = CreateRect("XP Track", profileScreen.transform, new Vector2(199f, 9f));
            SetTopRect((RectTransform)xpTrackObject.transform, 0f, 350f, new Vector2(199f, 9f));
            var xpTrack = xpTrackObject.AddComponent<RoundedRectGraphic>();
            xpTrack.color = MemeHunterUiColors.Teal;
            var xpFillObject = CreateRect("XP Progress", xpTrackObject.transform, new Vector2(51f, 9f));
            var xpFillRect = (RectTransform)xpFillObject.transform;
            xpFillRect.anchorMin = new Vector2(0f, 0.5f);
            xpFillRect.anchorMax = new Vector2(0f, 0.5f);
            xpFillRect.pivot = new Vector2(0f, 0.5f);
            xpFillRect.anchoredPosition = Vector2.zero;
            xpFillRect.sizeDelta = new Vector2(51f, 9f);
            var xpFill = xpFillObject.AddComponent<RoundedRectGraphic>();
            xpFill.color = Color.white;
            var xpGradient = xpFillObject.AddComponent<HorizontalGradientEffect>();
            xpGradient.SetColors(MemeHunterUiColors.Teal, MemeHunterUiColors.LightPurple);
            var xpText = MakeProfileText("XP Value", profileScreen.transform, "220/860", MemeHunterUiColors.DarkNavy, 12f, 360f, new Vector2(120f, 18f));
            var xpView = profileScreen.AddComponent<XpProgressView>();
            SetReference(xpView, "track", xpTrackObject.transform);
            SetReference(xpView, "progress", xpFillRect);
            SetReference(xpView, "valueText", xpText);
            SetReference(xpView, "trackImage", xpTrack);
            SetReference(xpView, "progressImage", xpFill);
            SetReference(xpView, "gradient", xpGradient);

            var leftDecoration = InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/PageDecorationLine.prefab"), profileScreen.transform, "Header Line Left");
            var rightDecoration = InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/PageDecorationLine.prefab"), profileScreen.transform, "Header Line Right");
            SetTopRect((RectTransform)leftDecoration.transform, -153f, 388f, new Vector2(109f, 1f));
            SetTopRect((RectTransform)rightDecoration.transform, 153f, 388f, new Vector2(109f, 1f));
            leftDecoration.GetComponent<Image>().color = MemeHunterUiColors.LightPurple;
            rightDecoration.GetComponent<Image>().color = MemeHunterUiColors.LightPurple;
            var sectionHeader = MakeProfileText("Best Meme Cards Header", profileScreen.transform, "BEST MEME CARDS", MemeHunterUiColors.LightPurple, 15f, 378f, new Vector2(190f, 22f));

            var tabBarObject = CreateRect("Profile Tabs", profileScreen.transform, new Vector2(480f, 32f));
            SetTopRect((RectTransform)tabBarObject.transform, 0f, 405f, new Vector2(480f, 32f));
            var tabBar = tabBarObject.AddComponent<ProfileTabBarView>();
            var tabButtons = new Button[3];
            var tabUnderlines = new Graphic[3];
            var tabLabels = new[] { "I AM", "MY FRIENDS", "GLOBAL" };
            var tabOffsets = new[] { -160f, 0f, 160f };
            for (var index = 0; index < tabLabels.Length; index++)
            {
                var tabObject = CreateRect(tabLabels[index], tabBarObject.transform, new Vector2(150f, 32f));
                SetTopRect((RectTransform)tabObject.transform, tabOffsets[index], 0f, new Vector2(150f, 32f));
                var hitTarget = tabObject.AddComponent<Image>();
                hitTarget.color = new Color(1f, 1f, 1f, 0f);
                var tabButton = tabObject.AddComponent<Button>();
                tabButton.targetGraphic = hitTarget;
                var tabText = CreateText("Label", tabObject.transform, Vector2.zero, MemeHunterUiColors.DarkNavy, 18f);
                Stretch(tabText.rectTransform);
                tabText.text = tabLabels[index];
                var underlineObject = CreateRect("Active Underline", tabObject.transform, new Vector2(72f, 2f));
                var underlineRect = (RectTransform)underlineObject.transform;
                underlineRect.anchorMin = new Vector2(0.5f, 0f);
                underlineRect.anchorMax = new Vector2(0.5f, 0f);
                underlineRect.pivot = new Vector2(0.5f, 0f);
                underlineRect.anchoredPosition = new Vector2(0f, 1f);
                underlineRect.sizeDelta = new Vector2(72f, 2f);
                var underline = underlineObject.AddComponent<Image>();
                underline.color = MemeHunterUiColors.DarkNavy;
                underline.raycastTarget = false;
                tabButtons[index] = tabButton;
                tabUnderlines[index] = underline;
            }
            SetArrayReference(tabBar, "tabs", tabButtons);
            SetArrayReference(tabBar, "underlines", tabUnderlines);

            var score = MakeProfileText("Profile Score", profileScreen.transform, "6099", MemeHunterUiColors.DarkNavy, 18f, 439f, new Vector2(74f, 24f));
            var scoreRect = (RectTransform)score.transform;
            scoreRect.anchoredPosition = new Vector2(-8f, -451f);
            var scoreIcon = CreateImage("Score Icon", profileScreen.transform, new Vector2(24f, 24f), Sprite("Assets/mst_files/UI/Blue Buttons/MemeHunter.png"), Color.white);
            SetTopRect((RectTransform)scoreIcon.transform, 45f, 439f, new Vector2(24f, 24f));
            scoreIcon.raycastTarget = false;

            var viewportObject = CreateRect("Meme Card Area", profileScreen.transform, new Vector2(426f, 607f));
            SetTopRect((RectTransform)viewportObject.transform, 0f, 469f, new Vector2(426f, 607f));
            viewportObject.AddComponent<RectMask2D>();
            var scrollRect = viewportObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.inertia = true;

            var contentObject = CreateRect("Card Content", viewportObject.transform, new Vector2(426f, 607f));
            var contentRect = (RectTransform)contentObject.transform;
            contentRect.anchorMin = new Vector2(0.5f, 1f);
            contentRect.anchorMax = new Vector2(0.5f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(426f, 607f);
            scrollRect.viewport = (RectTransform)viewportObject.transform;
            scrollRect.content = contentRect;

            var fullCardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/MemeCardFull.prefab");
            var fullCard = InstantiatePrefab(fullCardPrefab, contentObject.transform, "B-HOPPING");
            var fullCardRect = (RectTransform)fullCard.transform;
            fullCardRect.anchorMin = new Vector2(0.5f, 1f);
            fullCardRect.anchorMax = new Vector2(0.5f, 1f);
            fullCardRect.pivot = new Vector2(0.5f, 1f);
            fullCardRect.anchoredPosition = new Vector2(0f, -2f);
            fullCard.GetComponent<MemeCardView>().Bind(data.BestMemeArtwork, data.BestMemeTitle, string.Empty, MemeHunterUiColors.CommonGrey);

            var emptyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/EmptyMemeCard.prefab");
            var emptyOffsets = new[] { -146f, 0f, 146f };
            for (var index = 0; index < emptyOffsets.Length; index++)
            {
                var emptyCard = InstantiatePrefab(emptyPrefab, contentObject.transform, "Empty Meme Slot " + (index + 1));
                var emptyRect = (RectTransform)emptyCard.transform;
                emptyRect.anchorMin = new Vector2(0.5f, 1f);
                emptyRect.anchorMax = new Vector2(0.5f, 1f);
                emptyRect.pivot = new Vector2(0.5f, 1f);
                emptyRect.anchoredPosition = new Vector2(emptyOffsets[index], -440f);
                emptyRect.localScale = Vector3.one * 0.55f;
            }

            var presenter = profileScreen.AddComponent<ProfileScreenPresenter>();
            SetReference(presenter, "profileData", data);
            SetReference(presenter, "profilePhoto", profilePhotoObject.GetComponent<ProfilePhotoView>());
            SetReference(presenter, "nicknameText", nickname);
            SetReference(presenter, "subtitleText", subtitle);
            SetReference(presenter, "levelText", level);
            SetReference(presenter, "xpProgress", xpView);
            SetReference(presenter, "scoreText", score);
            SetReference(presenter, "bestMemeCard", fullCard.GetComponent<MemeCardView>());
            SetReference(presenter, "tabBar", tabBar);

            var backObject = InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/CircularActionButton.prefab"), persistentRoot.transform, "Profile Back Button");
            SetTopRect((RectTransform)backObject.transform, -199f, 16f, new Vector2(63f, 63f));
            var discardSprite = Sprite("Assets/mst_files/UI/Blue Buttons/Discard button.png");
            var iconImage = backObject.transform.Find("Icon Slot").GetComponent<Image>();
            iconImage.sprite = discardSprite;
            iconImage.enabled = true;
            ((RectTransform)iconImage.transform).sizeDelta = new Vector2(42f, 42f);
            SetReference(presenter, "backButton", backObject.GetComponent<CircularActionButton>());

            var scenePath = "Assets/Scenes/Profile.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("Built Profile scene at 498x1080 with reusable UI prefabs and profile presentation data.");
        }

        static ProfilePresentationData BuildProfileDataAsset()
        {
            const string folder = "Assets/Resources/Profile";
            const string path = folder + "/ProfilePresentationData.asset";
            Directory.CreateDirectory(folder);
            var data = AssetDatabase.LoadAssetAtPath<ProfilePresentationData>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<ProfilePresentationData>();
                AssetDatabase.CreateAsset(data, path);
            }

            var serializedData = new SerializedObject(data);
            serializedData.FindProperty("nickname").stringValue = "DigItalitE";
            serializedData.FindProperty("age").intValue = 10;
            serializedData.FindProperty("city").stringValue = "Astana";
            serializedData.FindProperty("level").intValue = 12;
            serializedData.FindProperty("currentXp").intValue = 220;
            serializedData.FindProperty("xpForNextLevel").intValue = 860;
            serializedData.FindProperty("score").intValue = 6099;
            serializedData.FindProperty("profilePhoto").objectReferenceValue = Sprite("Assets/mst_files/UI/Etc/Profile Photo.png");
            serializedData.FindProperty("bestMemeTitle").stringValue = "B-HOPPING";
            serializedData.FindProperty("bestMemeArtwork").objectReferenceValue = null;
            serializedData.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
            return data;
        }

        static GameObject InstantiatePrefab(GameObject prefab, Transform parent, string objectName)
        {
            if (prefab == null)
                throw new FileNotFoundException("Missing required UI prefab for " + objectName);

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = objectName;
            return instance;
        }

        static TMP_Text MakeProfileText(string name, Transform parent, string value, Color tint, float size, float top, Vector2 dimensions)
        {
            var text = CreateText(name, parent, Vector2.zero, tint, size);
            text.text = value;
            SetTopRect(text.rectTransform, 0f, top, dimensions);
            return text;
        }

        static void SetTopRect(RectTransform rect, float xOffset, float topOffset, Vector2 dimensions)
        {
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(xOffset, -topOffset);
            rect.sizeDelta = dimensions;
            rect.localScale = Vector3.one;
        }

        static void SetArrayReference<T>(Object target, string propertyName, T[] values) where T : Object
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName);
            property.arraySize = values.Length;
            for (var index = 0; index < values.Length; index++)
                property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        static void BuildGradientButton()
        {
            var root = CreateRoot("Generic Gradient Button", new Vector2(266f, 58f));
            var background = root.AddComponent<Image>();
            background.sprite = Sprite("Assets/mst_files/UI/Blue Buttons/Button.png");
            background.preserveAspect = true;

            var button = root.AddComponent<Button>();
            button.targetGraphic = background;
            button.transition = Selectable.Transition.ColorTint;

            var label = CreateText("Label", root.transform, new Vector2(0f, 0f), MemeHunterUiColors.DarkNavy, 20f);
            Stretch(label.rectTransform);
            label.text = "Button";
            var component = root.AddComponent<GradientButton>();
            SetReference(component, "button", button);
            SetReference(component, "background", background);
            SetReference(component, "label", label);
            Save(root, "GenericGradientButton");
        }

        static void BuildCircularActionButton()
        {
            var root = CreateRoot("Circular Action Button", new Vector2(63f, 63f));
            var radial = root.AddComponent<RadialGradientGraphic>();
            radial.SetColors(MemeHunterUiColors.Teal, MemeHunterUiColors.LightPurple);
            var button = root.AddComponent<Button>();
            button.targetGraphic = radial;
            button.transition = Selectable.Transition.ColorTint;

            var icon = CreateImage("Icon Slot", root.transform, new Vector2(24f, 24f), null, Color.white);
            icon.raycastTarget = false;
            icon.enabled = false;

            var component = root.AddComponent<CircularActionButton>();
            SetReference(component, "button", button);
            SetReference(component, "background", radial);
            SetReference(component, "icon", icon);
            Save(root, "CircularActionButton");
        }

        static void BuildPageDecorationLine()
        {
            var root = CreateRoot("Page Decoration Line", new Vector2(109f, 1f));
            var image = root.AddComponent<Image>();
            image.sprite = Sprite("Assets/mst_files/UI/Etc/Page Decoration Line.png");
            image.color = MemeHunterUiColors.DarkNavy;
            image.preserveAspect = true;
            Save(root, "PageDecorationLine");
        }

        static void BuildRarityCircle()
        {
            var root = CreateRoot("Meme Rarity Circle", new Vector2(58f, 58f));
            var image = root.AddComponent<Image>();
            image.sprite = Sprite("Assets/mst_files/UI/Rarity/Common Circle.png");
            image.preserveAspect = true;

            var component = root.AddComponent<RarityIndicatorView>();
            SetReference(component, "marker", image);
            SetReference(component, "commonSprite", Sprite("Assets/mst_files/UI/Rarity/Common Circle.png"));
            SetReference(component, "uncommonSprite", Sprite("Assets/mst_files/UI/Rarity/Uncommon Circle.png"));
            SetReference(component, "rareSprite", Sprite("Assets/mst_files/UI/Rarity/Rare Circle.png"));
            SetReference(component, "legendarySprite", Sprite("Assets/mst_files/UI/Rarity/Legendary Circle.png"));
            Save(root, "MemeRarityCircle");
        }

        static void BuildEmptyMemeCard()
        {
            var root = CreateRoot("Empty Meme Card", new Vector2(192f, 286f));
            var image = root.AddComponent<Image>();
            image.sprite = Sprite("Assets/mst_files/UI/Etc/Empty Card.png");
            image.preserveAspect = true;
            var button = root.AddComponent<Button>();
            button.targetGraphic = image;

            var component = root.AddComponent<EmptyMemeCardView>();
            SetReference(component, "button", button);
            SetReference(component, "outline", image);
            Save(root, "EmptyMemeCard");
        }

        static void BuildMemeCard(string prefabName, string framePath, Vector2 size)
        {
            var root = CreateRoot(prefabName, size);
            var frame = root.AddComponent<Image>();
            frame.sprite = Sprite(framePath);
            frame.preserveAspect = true;

            var artwork = CreateImage("Artwork Slot", root.transform, size - new Vector2(20f, 20f), null, Color.white);
            artwork.enabled = false;
            artwork.raycastTarget = false;
            var title = CreateText("Title", root.transform, new Vector2(0f, -size.y * 0.38f), MemeHunterUiColors.DarkNavy, 16f);
            title.gameObject.SetActive(false);
            var rarityTransform = CreateRect("Rarity", root.transform, new Vector2(58f, 58f)).transform;
            var rarityRect = (RectTransform)rarityTransform;
            rarityRect.anchorMin = new Vector2(1f, 1f);
            rarityRect.anchorMax = new Vector2(1f, 1f);
            rarityRect.pivot = new Vector2(0.5f, 0.5f);
            rarityRect.anchoredPosition = new Vector2(-36f, -36f);
            var rarityMarker = rarityTransform.gameObject.AddComponent<Image>();
            rarityMarker.sprite = Sprite("Assets/mst_files/UI/Rarity/Common Circle.png");
            rarityMarker.preserveAspect = true;
            var rarity = rarityTransform.gameObject.AddComponent<RarityIndicatorView>();
            SetReference(rarity, "marker", rarityMarker);
            SetReference(rarity, "commonSprite", Sprite("Assets/mst_files/UI/Rarity/Common Circle.png"));
            SetReference(rarity, "uncommonSprite", Sprite("Assets/mst_files/UI/Rarity/Uncommon Circle.png"));
            SetReference(rarity, "rareSprite", Sprite("Assets/mst_files/UI/Rarity/Rare Circle.png"));
            SetReference(rarity, "legendarySprite", Sprite("Assets/mst_files/UI/Rarity/Legendary Circle.png"));
            rarityTransform.gameObject.SetActive(false);
            var component = root.AddComponent<MemeCardView>();
            SetReference(component, "cardFrame", frame);
            SetReference(component, "artwork", artwork);
            SetReference(component, "titleText", title);
            SetReference(component, "rarityIndicator", rarity);
            Save(root, prefabName);
        }

        static void BuildProfilePhoto()
        {
            var root = CreateRoot("Profile Photo", new Vector2(183f, 183f));
            var maskImage = root.AddComponent<Image>();
            var placeholder = Sprite("Assets/mst_files/UI/Etc/Profile Photo.png");
            maskImage.sprite = placeholder;
            var mask = root.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            var photo = CreateImage("Photo", root.transform, new Vector2(183f, 183f), placeholder, Color.white);
            photo.raycastTarget = false;
            var component = root.AddComponent<ProfilePhotoView>();
            SetReference(component, "photoImage", photo);
            SetReference(component, "placeholderSprite", placeholder);
            Save(root, "ProfilePhoto");
        }

        static void BuildNotificationCard()
        {
            var root = CreateRoot("Notification Card", new Vector2(458f, 70f));
            var background = root.AddComponent<Image>();
            background.sprite = Sprite("Assets/mst_files/UI/Etc/Notifications Background.png");
            background.preserveAspect = true;
            background.color = new Color32(179, 255, 254, 255);

            var thumbnail = CreateImage("Thumbnail", root.transform, new Vector2(56f, 56f), Sprite("Assets/mst_files/UI/Etc/Profile Photo.png"), Color.white);
            Anchor(thumbnail.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(8f, -28f), new Vector2(64f, 28f));
            thumbnail.raycastTarget = false;

            var title = CreateText("Title", root.transform, Vector2.zero, Color.black, 25f);
            Anchor(title.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.76f, 0.5f), new Vector2(74f, 7f), new Vector2(-4f, 0f));
            title.alignment = TextAlignmentOptions.MidlineLeft;
            title.text = "Some event going on";

            var message = CreateText("Message", root.transform, Vector2.zero, Color.black, 15f);
            Anchor(message.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.76f, 0.5f), new Vector2(74f, -23f), new Vector2(-4f, -2f));
            message.alignment = TextAlignmentOptions.MidlineLeft;
            message.text = "oumaigaaa what is that";

            var time = CreateText("Time", root.transform, Vector2.zero, Color.black, 25f);
            Anchor(time.rectTransform, new Vector2(0.78f, 0f), new Vector2(1f, 1f), new Vector2(0f, 2f), new Vector2(-12f, -2f));
            time.alignment = TextAlignmentOptions.MidlineRight;
            time.text = "06:08";

            var unreadObject = CreateRect("Unread Marker", root.transform, new Vector2(10f, 10f));
            var unread = unreadObject.AddComponent<RoundedRectGraphic>();
            unread.color = MemeHunterUiColors.BrightGreen;
            unread.raycastTarget = false;
            Anchor((RectTransform)unread.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-26f, -5f), new Vector2(-16f, 5f));

            var component = root.AddComponent<NotificationItemView>();
            SetReference(component, "titleText", title);
            SetReference(component, "messageText", message);
            SetReference(component, "timeText", time);
            SetReference(component, "unreadMarker", unread);
            SetReference(component, "thumbnail", thumbnail);
            Save(root, "NotificationCard");
        }

        static void BuildScanCentre()
        {
            var root = CreateRoot("AR Scan Centre", new Vector2(203f, 206f));
            var image = root.AddComponent<Image>();
            image.sprite = Sprite("Assets/mst_files/UI/Etc/Scan Centre.png");
            image.preserveAspect = true;
            image.color = MemeHunterUiColors.BrightGreen;
            Save(root, "ARScanCentre");
        }

        static void BuildFormInput()
        {
            var root = CreateRoot("Form Input", new Vector2(338f, 78f));
            var fieldObject = CreateRect("Input Field", root.transform, new Vector2(338f, 52f));
            var fieldRect = (RectTransform)fieldObject.transform;
            Anchor(fieldRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero);

            var background = fieldObject.AddComponent<RoundedRectGraphic>();
            background.color = Color.white;
            var outline = fieldObject.AddComponent<Outline>();
            outline.effectColor = MemeHunterUiColors.FormBlue;
            outline.effectDistance = new Vector2(1f, -1f);

            var viewportObject = CreateRect("Text Viewport", fieldObject.transform, Vector2.zero);
            var viewport = (RectTransform)viewportObject.transform;
            Stretch(viewport, new Vector2(12f, 4f), new Vector2(-12f, -4f));
            viewportObject.AddComponent<RectMask2D>();

            var inputText = CreateText("Text", viewport, Vector2.zero, MemeHunterUiColors.DarkNavy, 15f);
            Stretch(inputText.rectTransform);
            inputText.alignment = TextAlignmentOptions.MidlineLeft;

            var placeholder = CreateText("Placeholder", viewport, Vector2.zero, MemeHunterUiColors.LightPurple, 15f);
            Stretch(placeholder.rectTransform);
            placeholder.alignment = TextAlignmentOptions.MidlineLeft;

            var input = fieldObject.AddComponent<TMP_InputField>();
            input.textComponent = inputText;
            input.placeholder = placeholder;
            input.textViewport = viewport;
            input.targetGraphic = background;
            input.lineType = TMP_InputField.LineType.SingleLine;

            var validation = CreateText("Validation", root.transform, new Vector2(0f, -54f), MemeHunterUiColors.RarePurple, 11f);
            Anchor(validation.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(10f, 0f), new Vector2(-10f, 22f));
            validation.alignment = TextAlignmentOptions.MidlineLeft;
            validation.gameObject.SetActive(false);

            var component = root.AddComponent<FormInputView>();
            SetReference(component, "inputField", input);
            SetReference(component, "validationText", validation);
            Save(root, "FormInput");
        }

        static GameObject CreateRoot(string name, Vector2 size)
        {
            var root = CreateRect(name, null, size);
            var rect = (RectTransform)root.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            return root;
        }

        static GameObject CreateRect(string name, Transform parent, Vector2 size)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            gameObject.transform.SetParent(parent, false);
            var rect = (RectTransform)gameObject.transform;
            rect.sizeDelta = size;
            return gameObject;
        }

        static Image CreateImage(string name, Transform parent, Vector2 size, Sprite sprite, Color tint)
        {
            var image = CreateRect(name, parent, size).AddComponent<Image>();
            image.sprite = sprite;
            image.color = tint;
            image.preserveAspect = true;
            return image;
        }

        static TextMeshProUGUI CreateText(string name, Transform parent, Vector2 position, Color tint, float fontSize)
        {
            var gameObject = CreateRect(name, parent, new Vector2(200f, 24f));
            var rect = (RectTransform)gameObject.transform;
            rect.anchoredPosition = position;
            var text = gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = fontSize;
            text.color = tint;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            return text;
        }

        static void Anchor(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        static void Stretch(RectTransform rect, Vector2? offsetMin = null, Vector2? offsetMax = null)
        {
            Anchor(rect, Vector2.zero, Vector2.one, offsetMin ?? Vector2.zero, offsetMax ?? Vector2.zero);
        }

        static Sprite Sprite(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                Debug.LogError("Missing UI sprite or sprite import settings: " + path);
            return sprite;
        }

        static void SetReference(Object target, string propertyName, Object value)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogError(target.GetType().Name + " has no serialized field named " + propertyName);
                return;
            }

            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        static void Save(GameObject root, string prefabName)
        {
            PrefabUtility.SaveAsPrefabAsset(root, PrefabFolder + "/" + prefabName + ".prefab");
            Object.DestroyImmediate(root);
        }

        static void ValidateGeneratedPrefabs()
        {
            var expectedSizes = new[]
            {
                new Vector2(266f, 58f),
                new Vector2(63f, 63f),
                new Vector2(109f, 1f),
                new Vector2(58f, 58f),
                new Vector2(192f, 286f),
                new Vector2(159f, 132f),
                new Vector2(383f, 432f),
                new Vector2(183f, 183f),
                new Vector2(458f, 70f),
                new Vector2(203f, 206f),
                new Vector2(338f, 78f)
            };
            var prefabNames = new[]
            {
                "GenericGradientButton", "CircularActionButton", "PageDecorationLine", "MemeRarityCircle",
                "EmptyMemeCard", "MemeCardPreview", "MemeCardFull", "ProfilePhoto", "NotificationCard",
                "ARScanCentre", "FormInput"
            };

            for (var index = 0; index < prefabNames.Length; index++)
            {
                var path = PrefabFolder + "/" + prefabNames[index] + ".prefab";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    Debug.LogError("Generated prefab could not be loaded: " + path);
                    continue;
                }

                var rect = prefab.transform as RectTransform;
                if (rect == null || Vector2.Distance(rect.sizeDelta, expectedSizes[index]) > 0.01f)
                    Debug.LogError("Generated prefab has an unexpected reference size: " + path);
            }
        }
    }
}