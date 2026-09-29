using System.Collections;
using UnityEngine;
public class WelcomeScreenAnimationController : MonoBehaviour
{
    [SerializeField] private GameObject screenOne;
    [SerializeField] private GameObject screenTwo;

    public bool Is_ready {get; private set;} = true;

    private Vector2 screenSize = new Vector2(498f,0f);

    public void StartContinueAnimation()
    {
        if(!Is_ready) return;
        Is_ready = false;
        StartCoroutine(SwapAnimation(-screenSize));
    }

    public void StartBackAnimation()
    {
        if(!Is_ready) return;
        Is_ready = false;
        StartCoroutine(SwapAnimation(screenSize));
    }

    private IEnumerator SwapAnimation(Vector2 moveBy)
    {
        float t = .75f;
        float elapsed = 0f;
        Vector2 initPos1 = screenOne.GetComponent<RectTransform>().anchoredPosition;
        Vector2 initPos2 = screenTwo.GetComponent<RectTransform>().anchoredPosition;
        
        while (elapsed < t)
        {
            elapsed+=Time.deltaTime;

            screenOne.GetComponent<RectTransform>().anchoredPosition = initPos1 + moveBy * Mathf.Lerp(elapsed/t,1, elapsed/t);
            screenTwo.GetComponent<RectTransform>().anchoredPosition = initPos2 + moveBy * Mathf.Lerp(elapsed/t,1, elapsed/t);
            yield return null;
        }
        yield return null;
        Is_ready = true;
    }
}