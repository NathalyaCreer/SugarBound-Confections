using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public FearMeter fearMeter;
    public EnemyStateManager enemyStateManager;
    public static GameManager Instance;

    private enum NightPhase { Phase1, Phase2, Phase3 }
    private NightPhase currentNightPhase = NightPhase.Phase1;

    public float phase2Time = 2.0f;
    public float phase3Time = 4.0f;

    public bool isTimerPaused = false;

    public EnemyDifficultySettings phase1Settings;
    public EnemyDifficultySettings phase2Settings;
    public EnemyDifficultySettings phase3Settings;

    public VoiceLines phase1VoiceLines;
    public VoiceLines phase2VoiceLines;
    public VoiceLines phase3VoiceLines;

    public TextMeshProUGUI text;

    public int startTime = 0;
    public int endTime = 6;

    public float levelDuration = 5.0f;
    public float timemultiplier = 5;

    [SerializeField] private float actualTime = 0.0f;
    [SerializeField] private float gameTime = 0.0f;

    public int minTransitions = 4;
    public int maxTransitions = 8;

    [SerializeField] private float[] transitionTimes;

    private int transitions = 0;
    private int currentTransition = 0;

    public void SetGameTimerPause(bool isPaused)
    {
        isTimerPaused = isPaused;
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        transitions = Random.Range(minTransitions, maxTransitions);
        transitionTimes = new float[transitions];

        float delta = (endTime - startTime) / (transitions + 1.0f);
        for (int i = 0; i < transitions; i++)
        {
            transitionTimes[i] = delta * (i + 1) + Random.Range(-0.375f * delta, 0.375f * delta);
        }

        currentNightPhase = NightPhase.Phase1;
        ApplyPhaseSettings();
    }

    void Update()
    {
        
        if (!isTimerPaused)
        {
            actualTime += Time.deltaTime * timemultiplier;
        }
        
        gameTime = (actualTime / (levelDuration * 60.0f)) * (endTime - startTime);

        CheckNightPhases();
        UpdateUIText();
        HandleRandomTransitions();

        if (gameTime >= endTime)
        {
            this.enabled = false;
        }
    }

    private void CheckNightPhases()
    {
        if (gameTime >= phase3Time && currentNightPhase < NightPhase.Phase3)
        {
            currentNightPhase = NightPhase.Phase3;
            ApplyPhaseSettings();
            Debug.Log("Entered Phase 3: MAXIMUM DANGER");
        }
        else if (gameTime >= phase2Time && currentNightPhase < NightPhase.Phase2)
        {
            currentNightPhase = NightPhase.Phase2;
            ApplyPhaseSettings();
            Debug.Log("Entered Phase 2: Difficulty Increased");
        }
    }

    private void ApplyPhaseSettings()
    {
        if (enemyStateManager != null)
        {
            switch (currentNightPhase)
            {
                case NightPhase.Phase1:
                    enemyStateManager.SetPhaseSettings(phase1Settings);
                    enemyStateManager.SetVoiceLines(phase1VoiceLines);
                    break;
                case NightPhase.Phase2:
                    enemyStateManager.SetPhaseSettings(phase2Settings);
                    enemyStateManager.SetVoiceLines(phase2VoiceLines);
                    enemyStateManager.UnlockAbility("Ability2");
                    break;
                case NightPhase.Phase3:
                    enemyStateManager.SetPhaseSettings(phase3Settings);
                    enemyStateManager.SetVoiceLines(phase3VoiceLines);
                    enemyStateManager.UnlockAbility("Ability3");
                    break;
            }
        }
    }

    private void UpdateUIText()
    {
        if (text != null)
        {
            int displayHour = (int)gameTime;
            if (displayHour == 0)
                text.text = "12:00 AM";
            else
            {
                text.text = displayHour.ToString("00") + ":00 AM";
            }
        }
    }

    private void HandleRandomTransitions()
    {
        if (currentTransition < transitionTimes.Length && gameTime >= transitionTimes[currentTransition])
        {
            currentTransition++;
            NodeManager.Instance?.TransitionOccured();
            fearMeter?.IncreaseFear(10f);
        }
    }
}