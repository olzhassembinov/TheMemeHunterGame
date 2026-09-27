using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MemeHunter.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class SceneLoadButton : MonoBehaviour
    {
        [SerializeField] Button button;
        [SerializeField] string sceneName;

        void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();

            if (button != null)
                button.onClick.AddListener(LoadScene);
        }

        public void LoadScene()
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogWarning("SceneLoadButton has no destination scene name.", this);
                return;
            }

            var scenePath = "Assets/Scenes/" + sceneName + ".unity";
            if (SceneUtility.GetBuildIndexByScenePath(scenePath) < 0)
            {
                Debug.LogWarning("Scene is not included in Build Settings: " + scenePath, this);
                return;
            }

            SceneManager.LoadScene(sceneName);
        }
    }
}
