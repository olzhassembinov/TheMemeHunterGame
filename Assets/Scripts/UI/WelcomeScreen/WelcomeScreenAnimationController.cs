using System.Collections;
using MemeHunter.UI;
using UnityEngine;
public class WelcomeScreenAnimationController : MonoBehaviour
{
    [SerializeField] private GameObject screenOne;
    [SerializeField] private GameObject screenTwo;

    private IEnumerator swapAnimation;


    private Vector2 screenSize = new Vector2(498f,0f);

    public void StartContinueAnimation()
    {
        if(!AppUiRoot.instance.Is_ready) return;
        swapAnimation = SwapAnimation(-screenSize);
        AppUiRoot.instance.SetBusy(swapAnimation);
    }

    public void StartBackAnimation()
    {
        if(!AppUiRoot.instance.Is_ready) return;
        
        swapAnimation = SwapAnimation(screenSize);

        AppUiRoot.instance.SetBusy(swapAnimation);
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
        AppUiRoot.instance.SetFree(swapAnimation);
    }
}