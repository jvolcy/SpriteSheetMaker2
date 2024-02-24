using UnityEngine;
using System.Collections;

public class Blink : MonoBehaviour
{
    public float blinkTime = 0.1f;
    public float blinkInterval = 3f;

    public GameObject LeftEye;
    public GameObject RightEye;
    SkinnedMeshRenderer LeftEyeSkinnedMeshRenderer;
    SkinnedMeshRenderer rightEyeSkinnedMeshRenderer;

    Material EyeMaterial;
    public Material EyeLidMaterial;

    private void Start()
    {
        LeftEyeSkinnedMeshRenderer = LeftEye.GetComponent<SkinnedMeshRenderer>();
        rightEyeSkinnedMeshRenderer = RightEye.GetComponent<SkinnedMeshRenderer>();

        EyeMaterial = LeftEyeSkinnedMeshRenderer.material;

        float startTime = Random.Range(0f, blinkInterval);  //ensure that different players blink at different times

        InvokeRepeating("StartBlink", startTime, blinkInterval);
        InvokeRepeating("EndBlink", startTime + blinkTime, blinkInterval);

    }

    private void StartBlink()
    {
        LeftEyeSkinnedMeshRenderer.material = EyeLidMaterial;
        rightEyeSkinnedMeshRenderer.material = EyeLidMaterial;

    }

    private void EndBlink()
    {
        LeftEyeSkinnedMeshRenderer.material = EyeMaterial;
        rightEyeSkinnedMeshRenderer.material = EyeMaterial;
    }

}

