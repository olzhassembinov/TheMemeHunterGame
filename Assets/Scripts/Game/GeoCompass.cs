using System;
using UnityEngine;

namespace MemeHunter.Game
{
    /// <summary>
    /// Which way the phone points (degrees clockwise from north) and the bearing to a place.
    /// Uses the phone's compass; if it has none (or it is unavailable), the walking direction from GPS is used;
    /// if that is unknown too, north-up (heading 0).
    /// </summary>
    public static class GeoCompass
    {
        static bool started;
        static bool compassBroken;
        static double lastLat, lastLng;
        static bool hasLast;
        static float movementHeading = float.NaN;

        /// <summary>True when the heading comes from a real sensor or from walking, false for the north-up fallback.</summary>
        public static bool HasHeading { get; private set; }

        public static float Heading
        {
            get
            {
                Start();
                TrackMovement();
                if (!compassBroken)
                {
                    try
                    {
                        if (Input.compass.enabled && Input.compass.timestamp > 0)
                        {
                            HasHeading = true;
                            return Input.compass.trueHeading > 0f ? Input.compass.trueHeading : Input.compass.magneticHeading;
                        }
                    }
                    catch (InvalidOperationException) { compassBroken = true; }   // legacy input disabled on this build
                }
                HasHeading = !float.IsNaN(movementHeading);
                return HasHeading ? movementHeading : 0f;
            }
        }

        static void Start()
        {
            if (started) return;
            started = true;
            try { Input.compass.enabled = true; }
            catch (InvalidOperationException) { compassBroken = true; }
        }

        /// <summary>Walking direction: bearing between GPS fixes at least 8 m apart.</summary>
        static void TrackMovement()
        {
            if (!Geo.TryGetFix(out var fix)) return;
            if (!hasLast) { lastLat = fix.Lat; lastLng = fix.Lng; hasLast = true; return; }
            if (Geo.DistanceMeters(lastLat, lastLng, fix.Lat, fix.Lng) < 8.0) return;
            movementHeading = Bearing(lastLat, lastLng, fix.Lat, fix.Lng);
            lastLat = fix.Lat;
            lastLng = fix.Lng;
        }

        /// <summary>Initial great-circle bearing from point 1 to point 2, degrees clockwise from north (0..360).</summary>
        public static float Bearing(double lat1, double lng1, double lat2, double lng2)
        {
            var p1 = lat1 * Math.PI / 180.0;
            var p2 = lat2 * Math.PI / 180.0;
            var dl = (lng2 - lng1) * Math.PI / 180.0;
            var y = Math.Sin(dl) * Math.Cos(p2);
            var x = Math.Cos(p1) * Math.Sin(p2) - Math.Sin(p1) * Math.Cos(p2) * Math.Cos(dl);
            var deg = Math.Atan2(y, x) * 180.0 / Math.PI;
            return (float)((deg + 360.0) % 360.0);
        }
    }
}
