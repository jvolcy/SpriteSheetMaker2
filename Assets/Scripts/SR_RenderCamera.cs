using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

//https://forum.unity.com/threads/how-to-save-manually-save-a-png-of-a-camera-view.506269/

public class SR_RenderCamera : MonoBehaviour
{
    public int OutputImageHeight = 256;
    public int OutputImageWidth = 256;

    public void CamCapture(string filename)
    {
        Camera Cam = GetComponent<Camera>();

        RenderTexture renderTexture = new RenderTexture(OutputImageWidth, OutputImageHeight, 1);

        RenderTexture currentRT = RenderTexture.active;
        RenderTexture currentCamRT = Cam.targetTexture;

        Cam.targetTexture = renderTexture;
        RenderTexture.active = Cam.targetTexture;

        Cam.Render();

        Texture2D Image = new Texture2D(Cam.targetTexture.width, Cam.targetTexture.height);
        Image.ReadPixels(new Rect(0, 0, Cam.targetTexture.width, Cam.targetTexture.height), 0, 0);
        Image.Apply();

        Cam.targetTexture = currentCamRT;
        RenderTexture.active = currentRT;

        var Bytes = Image.EncodeToPNG();
        Destroy(Image);

        File.WriteAllBytes(filename, Bytes);
    }


}


