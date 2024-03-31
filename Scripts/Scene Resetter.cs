using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneResetter : MonoBehaviour
{
    private void Update()
    {
        if (MusicController.instance.isMusicPicked && Input.GetKeyDown(KeyCode.R))
        {
            RunMusicGen.clip = null;
            MusicImporter.clip = null;
            Spectrographer.spectrogram = null;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SkyboxGenerator.isGenerated = false;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
