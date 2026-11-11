using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace MemeHunter.Game
{
    /// <summary>
    /// Puts the meme into the camera view when the player is close enough to catch it.
    /// Show(rarity) / Hide() / SetPhotoMode(bool). Creates everything from code: no scene or prefab edits.
    /// </summary>
    public static class MemeSpawner
    {
        static MemeSpawnerView view;

        /// <summary>Places a meme of this rarity in front of the player. Does nothing if one is already there.</summary>
        public static void Show(string rarity) => Ensure().Show(rarity);

        /// <summary>Removes the meme (hunt cancelled, caught, or the player walked away).</summary>
        public static void Hide()
        {
            if (view != null) view.Hide();
        }

        /// <summary>true while the photo is taken: the meme must NOT be in the picture that goes to doskaz.</summary>
        public static void SetPhotoMode(bool on)
        {
            if (view != null) view.SetPhotoMode(on);
        }

        static MemeSpawnerView Ensure()
        {
            if (view == null)
                view = new GameObject("MemeSpawner").AddComponent<MemeSpawnerView>();
            return view;
        }
    }

    /// <summary>
    /// Where the meme pictures come from.
    /// For now: PNG files inside Assets/Resources/MemeBillboards/&lt;rarity&gt;/ (common, uncommon, rare, legendary).
    /// Later the backend can send an image address with every card: replace PickFor with a download.
    /// </summary>
    public static class MemeImages
    {
        const string Folder = "MemeBillboards";

        /// <summary>A random picture for this rarity (or any picture if that rarity has none). null if there are none.</summary>
        public static Texture2D PickFor(string rarity)
        {
            var key = string.IsNullOrWhiteSpace(rarity) ? "common" : rarity.Trim().ToLowerInvariant();
            var pool = Resources.LoadAll<Texture2D>(Folder + "/" + key);
            if (pool.Length == 0)
                pool = Resources.LoadAll<Texture2D>(Folder);
            return pool.Length == 0 ? null : pool[Random.Range(0, pool.Length)];
        }
    }

    public sealed class MemeSpawnerView : MonoBehaviour
    {
        const float MaxWidthM = 1f;                 // the meme fits into a box 1 m wide ...
        const float MaxHeightM = 2f;                // ... and 2 m tall (keeps the picture's proportions)
        const float DistanceM = 3f;                 // placed this far in front of the player
        const float AssumedPhoneHeightM = 1.4f;     // floor is assumed this far below the phone if no floor is found
        const float FloorSearchSeconds = 1.5f;      // how long we look for the real floor before guessing

        static readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();

        GameObject billboard;
        Texture2D placeholderTexture;
        int version;            // Hide() changes it, so a placement that is still waiting for the floor is cancelled
        bool wanted;            // a meme is shown or being placed
        bool photoMode;

        public async void Show(string rarity)
        {
            if (wanted) return;
            wanted = true;
            var myVersion = ++version;

            var cam = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
            if (cam == null) { wanted = false; return; }

            var spot = await FindSpotAsync(cam);
            if (myVersion != version || cam == null) return;     // hidden while we were looking for the floor

            billboard = BuildBillboard(rarity, spot, cam.transform);
            billboard.SetActive(!photoMode);
        }

        public void Hide()
        {
            wanted = false;
            version++;
            if (billboard != null) Destroy(billboard);
            billboard = null;
            if (placeholderTexture != null) Destroy(placeholderTexture);
            placeholderTexture = null;
        }

        public void SetPhotoMode(bool on)
        {
            photoMode = on;
            if (billboard != null) billboard.SetActive(!on);
        }

        // ---------- Where to put it ----------

        /// <summary>Point on the floor about 3 m in front of the player. Uses AR plane detection if it finds a floor in time.</summary>
        async Awaitable<Vector3> FindSpotAsync(Camera cam)
        {
            var camPos = cam.transform.position;
            var forward = cam.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
            forward.Normalize();

            var guess = camPos + forward * DistanceM + Vector3.down * AssumedPhoneHeightM;

            var raycaster = FindFirstObjectByType<ARRaycastManager>();
            var planes = FindFirstObjectByType<ARPlaneManager>();
            var waited = 0f;
            while (raycaster != null && waited < FloorSearchSeconds)
            {
                var ray = new Ray(camPos, (guess - camPos).normalized);
                hits.Clear();
                if (raycaster.Raycast(ray, hits, TrackableType.PlaneWithinPolygon))
                {
                    foreach (var hit in hits)
                    {
                        // Only a floor (horizontal plane facing up), not a wall.
                        var plane = planes != null ? planes.GetPlane(hit.trackableId) : null;
                        if (plane != null && plane.alignment != PlaneAlignment.HorizontalUp) continue;

                        var p = hit.pose.position;
                        var flat = new Vector2(p.x - camPos.x, p.z - camPos.z).magnitude;
                        if (flat > 1.5f && flat < 8f) return p;
                    }
                }

                // Not exactly there (yet): use the lowest floor-like plane AR already knows. The real floor is the lowest one.
                if (waited >= 0.5f && TryGetLowestFloorY(planes, camPos.y, out var knownFloorY))
                    return new Vector3(guess.x, knownFloorY, guess.z);

                await Awaitable.NextFrameAsync();
                waited += Time.unscaledDeltaTime;
            }

            if (TryGetLowestFloorY(planes, camPos.y, out var lastFloorY))
                return new Vector3(guess.x, lastFloorY, guess.z);
            return guess;                                       // no plane at all: assume the floor is 1.4 m below the phone
        }

        /// <summary>Height of the lowest horizontal plane that is at least 1 m below the phone (tables and shelves are higher).</summary>
        static bool TryGetLowestFloorY(ARPlaneManager planes, float phoneY, out float floorY)
        {
            floorY = 0f;
            if (planes == null) return false;

            var found = false;
            foreach (var plane in planes.trackables)
            {
                if (plane.alignment != PlaneAlignment.HorizontalUp) continue;
                var y = plane.transform.position.y;
                if (y > phoneY - 1f) continue;
                if (!found || y < floorY) { floorY = y; found = true; }
            }
            return found;
        }

        // ---------- What to show ----------

        GameObject BuildBillboard(string rarity, Vector3 spot, Transform cam)
        {
            var root = new GameObject("MemeBillboard");
            root.transform.position = spot;                      // the bottom of the meme stands on this point

            var art = new GameObject("Art", typeof(SpriteRenderer));
            art.transform.SetParent(root.transform, false);
            var sr = art.GetComponent<SpriteRenderer>();
            sr.sortingOrder = 5;

            var texture = MemeImages.PickFor(rarity);
            if (texture != null)
            {
                // Real picture, scaled to fit the 1 x 2 m box. Pivot is bottom-center.
                var aspect = (float)texture.width / texture.height;
                var width = Mathf.Min(MaxWidthM, MaxHeightM * aspect);
                var height = width / aspect;
                var pixelsPerUnit = texture.height / height;
                sr.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0f), pixelsPerUnit, 0, SpriteMeshType.FullRect);
            }
            else
            {
                // No pictures in Resources/MemeBillboards yet: a plain coloured 1 x 2 m panel with the rarity written on it.
                placeholderTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                placeholderTexture.SetPixel(0, 0, PlaceholderColor(rarity));
                placeholderTexture.Apply();
                sr.sprite = Sprite.Create(placeholderTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0f), 1f);
                art.transform.localScale = new Vector3(MaxWidthM, MaxHeightM, 1f);

                var labelGo = new GameObject("Label");
                labelGo.transform.SetParent(root.transform, false);
                labelGo.transform.localPosition = new Vector3(0f, MaxHeightM * 0.5f, -0.01f);   // in front of the panel
                var label = labelGo.AddComponent<TextMeshPro>();
                label.text = (string.IsNullOrEmpty(rarity) ? "meme" : rarity).ToUpperInvariant() + "\nMEME";
                label.alignment = TextAlignmentOptions.Center;
                label.color = Color.white;
                label.enableAutoSizing = true;
                label.fontSizeMin = 0.5f;
                label.fontSizeMax = 6f;
                label.rectTransform.sizeDelta = new Vector2(MaxWidthM * 0.9f, MaxHeightM * 0.9f);
                label.sortingOrder = 6;
            }

            root.AddComponent<MemeBillboardView>().Init(cam);
            return root;
        }

        static Color PlaceholderColor(string rarity)
        {
            switch ((rarity ?? "").Trim().ToLowerInvariant())
            {
                case "uncommon":  return new Color(0.20f, 0.60f, 0.35f, 0.95f);
                case "rare":      return new Color(0.50f, 0.28f, 0.80f, 0.95f);
                case "legendary": return new Color(0.90f, 0.65f, 0.10f, 0.95f);
                default:          return new Color(0.40f, 0.42f, 0.50f, 0.95f);
            }
        }
    }

    /// <summary>Pop-in animation and a slight "breathing". Turns to face the player once, then stays where it is.</summary>
    public sealed class MemeBillboardView : MonoBehaviour
    {
        const float PopSeconds = 0.35f;

        float age;

        public void Init(Transform camera)
        {
            var away = transform.position - camera.position;     // sprites are seen from their -Z side
            away.y = 0f;
            if (away.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(away);
        }

        void Update()
        {
            age += Time.deltaTime;

            var pop = Mathf.Clamp01(age / PopSeconds);
            var scale = pop < 1f ? EaseOutBack(pop) : 1f;
            var breathing = 1f + 0.02f * Mathf.Sin(age * 2.5f);          // feet stay on the ground
            transform.localScale = new Vector3(scale, scale * breathing, scale);
        }

        static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }
    }
}
