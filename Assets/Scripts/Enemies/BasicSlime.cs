using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class BasicSlime : MonoBehaviour, IEnemy
{
    #region Fields
    public int Health { get; set; } = 1; // Slime's health

    [Header("Sight Settings")]
    [SerializeField] private int sightRange = 20; // Range in which the slime can see the player
    [SerializeField] private int sightAngle = 45; // Field of view angle

    [Header("Movement Settings")]
    [SerializeField] private float wanderSpeed = 1f;
    [SerializeField] private float approachSpeed = 2f;

    [Header("Slime Effects")]
    [SerializeField] private ParticleSystem deathParticlesPrefab; // Reference to the particle system prefab

    [Header("Audio Settings")]
    [SerializeField] private AudioClip[] slimeAudioClips; // Array of random audio clips for regular sounds
    [SerializeField] private AudioClip[] fleeAudioClips; // Array of flee audio clips when slime runs away
    [SerializeField] private float audioPlayRange = 10f; // Range in which the audio will be played
    [SerializeField] private float audioClipMinDelay = 4f; // Minimum time between playing audio clips
    [SerializeField] private float audioClipMaxDelay = 6f; // Maximum time between playing audio clips
    private static int maxConcurrentAudioClips = 5; // Max number of clips playing at the same time
    private static int currentAudioClips = 0; // Counter for how many clips are currently playing

    private static List<BasicSlime> allEnemies = new List<BasicSlime>(); // List to store all enemies

    private Transform player;
    private NavMeshAgent agent;
    private bool isPlayerInSight;
    private AudioSource audioSource; // AudioSource for the slime
    private bool isFleeing;
    private float fleeTimer;
    private float nextAudioTime; // Time until the next audio clip can play
    #endregion

    #region Unity Methods
    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player == null)
        {
            Debug.LogError("Player not found! Ensure the player has the 'Player' tag.");
            return;
        }

        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError("NavMeshAgent component missing!");
            return;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            Debug.LogError("AudioSource component missing!");
            return;
        }

        // Add this enemy to the list of all enemies
        allEnemies.Add(this);

        // Notify GestorEnemigos about this slime's spawn
        if (GestorEnemigos.Instance != null)
        {
            GestorEnemigos.Instance.AddEnemy();
        }

        SetNewRandomWanderDestination();
        nextAudioTime = Time.time + Random.Range(audioClipMinDelay, audioClipMaxDelay); // Initial delay
    }

    private void OnDestroy()
    {
        // Remove this enemy from the list when destroyed
        allEnemies.Remove(this);

        // Notify GestorEnemigos about this slime's death
        if (GestorEnemigos.Instance != null)
        {
            GestorEnemigos.Instance.RemoveEnemy();
        }
    }

    private void Update()
    {
        ReactToPlayer();

        // Play random audio based on distance from the player, and if the audio delay has passed
        PlayRandomAudioClip();
    }
    #endregion

    #region Movement
    private void ReactToPlayer()
    {
        if (player == null) return;

        if (IsPlayerInSight(player.position))
        {
            isPlayerInSight = true;
            ApproachPlayer(player.position);
        }
        else
        {
            isPlayerInSight = false;
            Wander();
        }
    }

    private void ApproachPlayer(Vector3 targetPosition)
    {
        if (agent.isStopped) agent.isStopped = false;

        agent.speed = approachSpeed;
        agent.SetDestination(targetPosition);
    }

    public void Wander()
    {
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            SetNewRandomWanderDestination();
        }

        agent.speed = wanderSpeed;
    }

    private void SetNewRandomWanderDestination()
    {
        Vector3 randomDirection = new Vector3(Random.Range(-10f, 10f), 0, Random.Range(-10f, 10f));
        Vector3 target = transform.position + randomDirection;

        if (NavMesh.SamplePosition(target, out NavMeshHit hit, 2f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }
    #endregion

    #region Utilities
    private bool IsPlayerInSight(Vector3 playerPosition)
    {
        Vector3 directionToPlayer = playerPosition - transform.position;

        if (directionToPlayer.magnitude < sightRange)
        {
            float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);
            return angleToPlayer < sightAngle;
        }

        return false;
    }

    private void PlayRandomAudioClip()
    {
        if (slimeAudioClips.Length == 0 || audioSource.isPlaying || Time.time < nextAudioTime) return;

        // Check if we can play a new audio clip (limit concurrent clips)
        if (currentAudioClips >= maxConcurrentAudioClips) return;

        // Play audio only if the player is close enough
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer <= audioPlayRange)
        {
            // Play random slime sound based on a 20% chance every 4-6 seconds
            if (Random.value < 0.2f) // 20% chance to play a sound
            {
                int randomIndex = Random.Range(0, slimeAudioClips.Length);
                audioSource.clip = slimeAudioClips[randomIndex];

                // Adjust volume based on distance (closer = louder, farther = quieter)
                float volume = Mathf.InverseLerp(0f, audioPlayRange, distanceToPlayer);
                audioSource.volume = volume;

                audioSource.PlayOneShot(audioSource.clip);
                currentAudioClips++; // Increment counter

                // Set the next time to play a clip, randomizing the delay
                nextAudioTime = Time.time + Random.Range(audioClipMinDelay, audioClipMaxDelay);
            }
        }

        // Reset the counter when the audio is finished playing
        currentAudioClips--; // Decrement counter
    }


    // This method is called when the slime flees
    public void Flee()
    {
        isFleeing = true;
        SetNewRandomWanderDestination();

        // Play random flee sound
        if (fleeAudioClips.Length > 0 && Random.value < 0.5f) // 50% chance to play a flee sound
        {
            int randomIndex = Random.Range(0, fleeAudioClips.Length);
            audioSource.clip = fleeAudioClips[randomIndex];

            // Reduce the volume by 50% for the flee sound
            float fleeSoundVolume = audioSource.volume * 0.5f;

            audioSource.PlayOneShot(audioSource.clip, fleeSoundVolume); // Adjusted volume
            currentAudioClips++; // Increment counter
        }
    }

    public void TakeDamage(int damage)
    {
        Health -= damage;
        Debug.Log($"Slime took damage! Current health: {Health}");

        if (Health <= 0)
        {
            Debug.Log("Slime defeated!");

            // Instantiate the particle system prefab and play it at the slime's position
            if (deathParticlesPrefab != null)
            {
                ParticleSystem particles = Instantiate(deathParticlesPrefab, transform.position, Quaternion.identity);
                particles.Play(); // Play the particle effect
            }

            // Notify nearby slimes to flee
            foreach (var enemy in allEnemies)
            {
                if (enemy != this && Vector3.Distance(transform.position, enemy.transform.position) <= sightRange)
                {
                    enemy.Flee();
                }
            }

            Destroy(gameObject); // Enemy dies
        }
    }
    #endregion
}
