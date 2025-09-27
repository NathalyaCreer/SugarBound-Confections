using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FearMeter : MonoBehaviour
{
    // fearmeter Variables
    public float maxFear = 100f;
    public float fearIncreaseRate = 5f;
    public float fearDecreaseRate = 2f;

    //audio Variables
    public AudioSource highFearAudioSource;
    public float maxAudioVolume = 0.5f;
    public float maxAudioPitch = 1.5f;  //slower/faster sound
    public AnimationCurve audioCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private float currentFear = 0f;

    // Reference to the Canvas Group for fading
    public CanvasGroup fearOverlayCanvasGroup;
    public AnimationCurve fearCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    void Start()
    {
        if (highFearAudioSource != null)
        {
            highFearAudioSource.loop = true;
            highFearAudioSource.Play();
        }
    }

    void Update()
    {
        if (highFearAudioSource != null)
        {
            float normalizedFear = currentFear / maxFear;
            float curveValue = audioCurve.Evaluate(normalizedFear);
            highFearAudioSource.volume = curveValue * maxAudioVolume;
            highFearAudioSource.pitch = 1f + curveValue * (maxAudioPitch - 1f);  
        }
    }

    // Public method to increase the player's fear level
    public void IncreaseFear(float amount)
    {
        currentFear += amount;
        currentFear = Mathf.Min(currentFear, maxFear);
    }

    public float GetCurrentFearNormalized()
    {
        return currentFear / maxFear;
    }
}
