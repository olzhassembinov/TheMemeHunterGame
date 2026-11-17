using System.Collections;
using MemeHunter.Game;
using MemeHunter.UI.Hunt;
using UnityEditor;
using UnityEngine;

namespace MemeHunter.Editor
{
    /// <summary>
    /// Plays one full hunt of a local test meme in the real AR scene, without the server:
    /// test meme placed 80 m away -> walk the fake GPS 10 m at a time -> meme appears -> Catch -> card reveal.
    /// Saves a screenshot of each step and logs the result, then quits.
    /// Batch: Unity -batchmode -projectPath . -executeMethod MemeHunter.Editor.HuntSimulation.Run (env UI_PREVIEW_DIR).
    /// Also in the menu: Meme Hunter / Simulate test hunt.
    /// </summary>
    public static class HuntSimulation
    {
        const string Scene = "Assets/Scenes/SampleScene.unity";

        public static void Run() => SimulationRunner.Start<HuntSimulationDriver>(Scene, quit: true);

        [MenuItem("Meme Hunter/Simulate test hunt")]
        static void RunFromMenu() => SimulationRunner.Start<HuntSimulationDriver>(Scene, quit: false);
    }

    public sealed class HuntSimulationDriver : SimulationDriver
    {
        protected override IEnumerator Run()
        {
            var until = Time.realtimeSinceStartup + 10f;
            while (HuntController.Instance == null && Time.realtimeSinceStartup < until) yield return null;
            var hunt = HuntController.Instance;
            var hud = FindFirstObjectByType<HuntHud>();
            if (hunt == null || hud == null) { Log("FAIL: hunt scene did not start"); yield break; }
            yield return new WaitForSecondsRealtime(1f);

            var caughtBefore = PlayerProgress.Caught.Count;
            var xpBefore = PlayerProgress.Xp;

            // 1) Test meme 80 m north of the (fake) player.
            Geo.FakeLat = 43.2573; Geo.FakeLng = 76.9288;
            hunt.StartTestHunt("legendary");
            var target = Net.Session.ActiveHunt;
            yield return new WaitForSecondsRealtime(1f);
            Log($"Test meme placed at {Dist(target):F0} m");
            Shot("sim_1_walking_80m");

            // 2) Walk toward it, 10 m every 0.6 s.
            var shot50 = false;
            while (Dist(target) > 25)
            {
                hunt.WalkToward(10);
                yield return new WaitForSecondsRealtime(0.6f);
                if (!shot50 && Dist(target) <= 50) { Log($"Walking: {Dist(target):F0} m"); Shot("sim_2_walking_50m"); shot50 = true; }
            }

            // 3) Within 30 m the meme appears in the camera view (it waits up to 1.5 s for a floor).
            yield return new WaitForSecondsRealtime(2.5f);
            Log($"Near: {Dist(target):F0} m, meme placed in view: {MemeSpawner.TryGetCenter(out _)}, " +
                $"in frame: {hud.MemeInFrame}, steady: {hud.IsSteady}, Catch enabled: {hud.CanCatch}");
            Shot("sim_3_meme_in_view");

            // 4) Catch (same as tapping the button).
            if (hud.CanCatch) hunt.Catch();
            else Log("Catch button was not enabled");
            yield return new WaitForSecondsRealtime(2.5f);
            Shot("sim_4_card_reveal");

            var caught = PlayerProgress.Caught.Count - caughtBefore;
            Log($"Caught: {caught}, XP {xpBefore} -> {PlayerProgress.Xp}, active hunt cleared: {Net.Session.ActiveHunt == null}");
            Failed = caught != 1;
            Log(Failed ? "FAIL" : "PASS: test meme found and caught");
        }

        static double Dist(Net.ClaimDto c) => Geo.DistanceMeters(Geo.FakeLat, Geo.FakeLng, c.Lat, c.Lng);
    }
}
