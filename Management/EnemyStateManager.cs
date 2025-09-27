using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.XR.Interaction.Toolkit;

public class EnemyStateManager : MonoBehaviour
{
    public static EnemyStateManager Instance;

    // Assigned in Inspector for robustness
    [Header("Core Components")]
    [SerializeField] private Enemywalk walkingAI;
    [SerializeField] private EnemyAppear teleportingAI;
    [SerializeField] private FearMeter playerFearMeter;
    [SerializeField] private CookingManager cookingManager;
    [SerializeField] private MemorySystem memorySystem;
    [SerializeField] private NavMeshAgent navMeshAgent;

    [Header("Phase Abilities")]
    public bool canTeleportToPlayer = true;
    public bool canUnlockAbility2 = false;
    public bool canUnlockAbility3 = false;
    public bool canSteal = false;
    public bool canSpawnTeleportDoor = false;
    public bool canChangeMission = false;

    [Header("Difficulty Settings")]
    public EnemyDifficultySettings phase1Settings;
    public EnemyDifficultySettings phase2Settings;
    public EnemyDifficultySettings phase3Settings;
    private EnemyDifficultySettings currentSettings;

    [Header("Voice Lines")]
    public VoiceLines phase1VoiceLines;
    public VoiceLines phase2VoiceLines;
    public VoiceLines phase3VoiceLines;
    [HideInInspector] public VoiceLines currentVoiceLines;
    [SerializeField] private AudioSource voiceAudioSource;

    [Header("Fear Interaction")]
    [Range(0f, 1f)]
    public float fearImpactOnBoredom = 0.5f;

    [Header("Proximity Based Behavior")]
    public Transform safeObject; //object Demon Reacts to
    public float safeObjectRadius = 10f; //The distance Demon cares
    public float safeObjectWeightBonus = 50;  //How much the weight increases

    [Header("Jumpscare Assets")]
    public GameObject monsterPrefab; //demon to appear
    private GameObject monsterInstance;
    public Transform jumpscarePoint;
    public AudioClip jumpscareAudioClip;
    [SerializeField] private AudioSource jumpscareAudioSource;
    public Animator jumpscareCameraAnimator;

    [Header("Abilities")]
    public List<AIAbilitySO> abilities;

    [Header("Behavior Control")]
    public float decisionInterval = 10f;
    public float minSpeakTime = 10f;
    public float maxSpeakTime = 30f;
    public Vector3 distractionLocation;
    public float distractionRadius = 15f;

    // Internal state
    public EnemyBaseState currentState;
    public EnemyIdleState IdleState = new EnemyIdleState();
    public EnemyWalkingState WalkingState = new EnemyWalkingState();
    public EnemyDistractedState DistractedState = new EnemyDistractedState();
    public float currentBoredom = 0f;
    private Dictionary<string, float> abilityCooldowns = new Dictionary<string, float>();
    
    // Coroutines
    private Coroutine speakCoroutine;
    private Coroutine decisionCoroutine;

    public float boredomIncreaseRate => currentSettings?.boredomIncreaseRate ?? 1f;
    public float boredomThreshold => currentSettings?.boredomThreshold ?? 80f;

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

        if (walkingAI == null || teleportingAI == null || playerFearMeter == null || navMeshAgent == null)
        {
            Debug.LogError("Essential AI components not assigned! Disabling EnemyStateManager.");
            enabled = false;
            return;
        }

        // Populate ability cooldowns safely
        foreach (var ability in abilities)
        {
            if (!abilityCooldowns.ContainsKey(ability.abilityName))
            {
                abilityCooldowns.Add(ability.abilityName, 0f);
            }
        }
    }

    void Start()
    {
        TransitionToState(IdleState);
        speakCoroutine = StartCoroutine(SpeakRoutine());
        decisionCoroutine = StartCoroutine(DecisionRoutine());
    }

    void Update()
    {
        currentState.UpdateState(this);
    }

    public void TransitionToState(EnemyBaseState state)
    {
        currentState = state;
        currentState.EnterState(this);
    }

    public bool IsAbilityReady(AIAbilitySO ability)
    {
        if (!abilityCooldowns.ContainsKey(ability.abilityName)) return false;
        return abilityCooldowns[ability.abilityName] <= 0;
    }

    public void ActivateAbility(AIAbilitySO ability)
    {
        if (IsAbilityReady(ability))
        {
            ability.Execute(this);
            abilityCooldowns[ability.abilityName] = ability.cooldown;
        }
    }

    public bool IsPlayerNearSafeObject()
    {
        if (safeObject != null && playerFearMeter != null)
        {
            float distance = Vector3.Distance(playerFearMeter.transform.position, safeObject.position);
            return distance <= safeObjectRadius;
        }
        return false;
    }

    private IEnumerator SpeakRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minSpeakTime, maxSpeakTime));
            PlayRandomVoiceLine();
        }
    }

    private void PlayRandomVoiceLine()
    {
        if (currentVoiceLines != null && currentVoiceLines.clips.Length > 0 && voiceAudioSource != null && !voiceAudioSource.isPlaying)
        {
            AudioClip clipToPlay = currentVoiceLines.clips[Random.Range(0, currentVoiceLines.clips.Length)];
            voiceAudioSource.PlayOneShot(clipToPlay);
        }
    }

    private IEnumerator DecisionRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(decisionInterval);
            DecideAndAct();
        }
    }
    
    private void DecideAndAct()
    {
        List<AIAbilitySO> potentialAbilities = new List<AIAbilitySO>();

        foreach(var ability in abilities)
        {
            if (IsAbilityReady(ability) && ability.CanExecute(this))
            {
                potentialAbilities.Add(ability);
            }
        }

        if (potentialAbilities.Count > 0)
        {
            AIAbilitySO selectedAbility = ChooseAbilityByWeight(potentialAbilities);
            if (selectedAbility != null)
            {
                ActivateAbility(selectedAbility);
            }
        }
    }

    private AIAbilitySO ChooseAbilityByWeight(List<AIAbilitySO> availableAbilities)
    {
        float totalWeight = 0;
        foreach(var ability in availableAbilities)
        {
            totalWeight += ability.GetDynamicWeight(this);
        }

        float randomValue = Random.Range(0, totalWeight);
        float currentWeight = 0;

        foreach(var ability in availableAbilities)
        {
            currentWeight += ability.GetDynamicWeight(this);
            if (randomValue <= currentWeight)
            {
                return ability;
            }
        }
        return null;
    }

    public void OnPatrolComplete()
    {
        currentBoredom = 0f;
        TransitionToState(IdleState);
    }

    public void ActivateJumpscare()
    {
        StartCoroutine(JumpscareRoutine());
    }
    
    private IEnumerator JumpscareRoutine()
    {
        // ... (Jumpscare logic)
        yield return null;
    }
}