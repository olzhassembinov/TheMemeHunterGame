using System;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

namespace MemeHunter.Game
{
    /// <summary>
    /// Device position + distance maths. Same API in the Editor (fake position) and on the phone (real GPS).
    /// </summary>
    public static class Geo
    {
        /// <summary>One position reading.</summary>
        public struct Fix
        {
            public double Lat;
            public double Lng;
            public float AccuracyM;   // GPS error radius in meters (smaller is better)
        }

        // Fake position used ONLY in the Unity Editor (a Mac has no GPS). The dev panel moves it to "walk".
        // Default: a point near KBTU, Almaty. Put your test card near it.
        public static double FakeLat = 43.2573;
        public static double FakeLng = 76.9288;

        /// <summary>
        /// Asks for the location permission (Android) and starts the GPS.
        /// Returns true when positions are available. Safe to call repeatedly.
        /// </summary>
        public static async Awaitable<bool> StartAsync()
        {
#if UNITY_EDITOR
            await Awaitable.NextFrameAsync();
            return true;                                   // the fake position is always available
#else
#if UNITY_ANDROID
            if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
            {
                Permission.RequestUserPermission(Permission.FineLocation);

                // The system dialog covers the app; wait up to 30 s for the answer.
                var until = Time.realtimeSinceStartup + 30f;
                while (!Permission.HasUserAuthorizedPermission(Permission.FineLocation)
                       && Time.realtimeSinceStartup < until)
                    await Awaitable.NextFrameAsync();

                if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
                    return false;                          // the player said no
            }
#endif
            if (!Input.location.isEnabledByUser)
                return false;                              // location is switched off in phone settings

            if (Input.location.status == LocationServiceStatus.Stopped)
                Input.location.Start(3f, 3f);              // desired accuracy 3 m, update every 3 m

            // Starting takes a few seconds ("Initializing"); wait up to 20 s.
            var timeout = Time.realtimeSinceStartup + 20f;
            while (Input.location.status == LocationServiceStatus.Initializing
                   && Time.realtimeSinceStartup < timeout)
                await Awaitable.NextFrameAsync();

            return Input.location.status == LocationServiceStatus.Running;
#endif
        }

        /// <summary>Current position, or false when the GPS is not running yet.</summary>
        public static bool TryGetFix(out Fix fix)
        {
#if UNITY_EDITOR
            fix = new Fix { Lat = FakeLat, Lng = FakeLng, AccuracyM = 5f };
            return true;
#else
            fix = default;
            if (Input.location.status != LocationServiceStatus.Running)
                return false;
            var d = Input.location.lastData;               // lat/lng arrive as float: ~0.5 m precision, fine for us
            fix = new Fix { Lat = d.latitude, Lng = d.longitude, AccuracyM = d.horizontalAccuracy };
            return true;
#endif
        }

        /// <summary>Straight-line distance in meters between two coordinates (haversine formula).</summary>
        public static double DistanceMeters(double lat1, double lng1, double lat2, double lng2)
        {
            const double EarthRadiusM = 6371000.0;
            var dLat = ToRad(lat2 - lat1);
            var dLng = ToRad(lng2 - lng1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
            return 2 * EarthRadiusM * Math.Asin(Math.Sqrt(a));
        }

        static double ToRad(double degrees) => degrees * Math.PI / 180.0;
    }
}
