using UnityEngine;
using UnityEngine.UI;

public class OpenTrailer : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private string url = "https://www.youtube.com/watch?v=fl1KHs_C3ss";

    void Start()
    {
        if (button == null)
            button = GetComponent<Button>();

        button.onClick.AddListener(OpenURL);
    }

    void OpenURL()
    {
        string youtubeAppUrl = "vnd.youtube://www.youtube.com/watch?v=fl1KHs_C3ss";
        string youtubeFallbackUrl = "https://www.youtube.com/watch?v=fl1KHs_C3ss";

        #if UNITY_ANDROID
        Application.OpenURL("vnd.youtube:" + "fl1KHs_C3ss");
        #elif UNITY_IOS
          Application.OpenURL("youtube://www.youtube.com/watch?v=fl1KHs_C3ss");
        else
          Application.OpenURL(youtubeFallbackUrl);
        #endif
    }

    void OnDestroy()
    {
        button.onClick.RemoveListener(OpenURL);
    }
}