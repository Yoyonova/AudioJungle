using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using SimpleFileBrowser;

public class MusicImporter : MonoBehaviour
{
    [SerializeField] private string initialPath;
    public static AudioClip clip;
    public static string description;
    public void OpenFileBrowser()
    {
        Debug.Log("Opening File Browser");
        FileBrowser.ShowLoadDialog(paths => StartCoroutine(LoadAudioClip(paths[0])), null, FileBrowser.PickMode.Files, title:"Import Music", loadButtonText:"Import");
    }

    private IEnumerator LoadAudioClip(string filePath)
    {
        Debug.Log("Chosen file: " + filePath);
        UpdateDescription(filePath);

        string audioURL = "file://" + filePath;

        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(audioURL, AudioType.MPEG))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                clip = DownloadHandlerAudioClip.GetContent(www);
                MusicController.EndMusicPick();
            }
            else
            {
                Debug.LogError("Error loading audio file: " + www.error);
            }
        }
    }

    private void UpdateDescription(string filePath)
    {
        description = filePath;
        description = description.Substring(description.LastIndexOf("\\") + 1);
        description = description.Substring(0, description.IndexOf("."));
    }
}
