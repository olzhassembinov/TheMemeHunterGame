using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Samples.ARStarterAssets;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;
using UnityEngine.XR.Templates.AR;

namespace MemeHunter.Game
{
    /// <summary>
    /// Switches off the leftovers of the "AR Mobile" Unity template at runtime (no scene edits):
    /// tap-to-place cubes, the coaching prompts ("Tap to Place"...), the cube button, the options button and the menus.
    /// The designer's own UI is not touched. Temporary: the new design will replace these objects.
    /// </summary>
    public static class TemplateUiCleanup
    {
        public static void Run()
        {
            // Tapping the ground must not spawn template objects any more.
            foreach (var trigger in Object.FindObjectsByType<ARInteractorSpawnTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                trigger.enabled = false;
            foreach (var spawner in Object.FindObjectsByType<ObjectSpawner>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                spawner.enabled = false;

            // Coaching prompts + options / cube buttons.
            foreach (var goal in Object.FindObjectsByType<GoalManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                goal.enabled = false;
                Hide(goal.greetingPrompt);
                Hide(goal.optionsButton);
                Hide(goal.createButton);
            }

            // Object menu and its buttons.
            foreach (var menu in Object.FindObjectsByType<ARTemplateMenuManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                menu.enabled = false;
                if (menu.createButton != null) Hide(menu.createButton.gameObject);
                if (menu.deleteButton != null) Hide(menu.deleteButton.gameObject);
                if (menu.cancelButton != null) Hide(menu.cancelButton.gameObject);
                Hide(menu.objectMenu);
                Hide(menu.modalMenu);
            }

            // The step prompts live under an object named "Coaching UI".
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (t.name == "Coaching UI")
                    t.gameObject.SetActive(false);
        }

        static void Hide(GameObject go)
        {
            if (go != null) go.SetActive(false);
        }
    }
}
