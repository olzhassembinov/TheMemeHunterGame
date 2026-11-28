using MemeHunter.Net;
using MemeHunter.UI.Kit;

namespace MemeHunter.UI.Hunt
{
    /// <summary>The error popups from the design task list: no internet, no GPS, generic error (Figma popup card).</summary>
    public static class HuntErrors
    {
        public static void NoInternet() =>
            Popup.Show("No connection", "We can't reach the meme servers right now. Check your internet and try again.");

        public static void NoGps() =>
            Popup.Show("No GPS signal", "We can't find where you are yet. Step outside or wait a few seconds, then try again.");

        public static void LocationDenied() =>
            Popup.Show("Location is off", "Meme Hunter needs your location to find memes. Allow it in the phone settings.");

        /// <summary>The claim ran out (30 minutes on the server): the card is free for everyone again.</summary>
        public static void TimeUp() =>
            Popup.Show("Time's up!", "The meme ran away: a claimed meme waits only 30 minutes. Start a new hunt to find another one.");

        public static void Generic(string details = null) =>
            Popup.Show("Oops, something went wrong", string.IsNullOrEmpty(details) ? "Please try again in a moment." : details);

        /// <summary>Picks the right popup for a failed server call.</summary>
        public static void From(ApiException e)
        {
            if (e.IsNetworkError) NoInternet();
            else Generic(e.Message);
        }
    }
}
