using UnityEngine;
using UnityEngine.AI;

public class RangedSlime : MonoBehaviour, IEnemy
{
    #region Fields
    public int Health { get; set; } = 1; // La salud del slime
    public int sightRange = 20; // Rango de visi�n predeterminado (en unidades)
    public int sightAngle = 45; // �ngulo de visi�n predeterminado (en grados)
    public float wanderSpeed = 1f; // Velocidad al deambular
    public float approachSpeed = 2f; // Velocidad al acercarse al jugador
    public float playerTrackingRange = 5f; // Rango en el que el slime dejar� de seguir al jugador

    // Rango de disparo y tiempo de recarga
    public float attackRange = 10f; // Rango de disparo del slime
    public float shootCooldown = 2f; // Tiempo en segundos entre disparos
    private float lastShootTime = 0f; // Registro de la �ltima vez que dispar�

    // Referencias a los objetos de jugador y proyectiles
    private Transform player; // Referencia al Transform del jugador
    private NavMeshAgent agent; // NavMeshAgent para el movimiento
    private Vector3 lastKnownPlayerPosition; // Guardar la �ltima posici�n conocida del jugador
    private bool isPlayerInSight = false; // Controla si el jugador est� a la vista

    // Prefab del proyectil
    public GameObject projectilePrefab; // Prefab del proyectil (esfera)
    public float projectileSpeed = 10f; // Velocidad del proyectil
    #endregion

    #region Unity Methods
    void Start()
    {
        // Encuentra el objeto jugador en la escena (suponiendo que tiene la etiqueta "Player")
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (player == null)
        {
            Debug.LogError("�Jugador no encontrado! Aseg�rate de que el jugador tenga la etiqueta 'Player'.");
        }

        // Obtiene el componente NavMeshAgent para el movimiento y la b�squeda de caminos
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError("Componente NavMeshAgent no encontrado en BasicSlime.");
        }

        // Configura las propiedades predeterminadas del NavMeshAgent para el movimiento
        agent.speed = wanderSpeed;
        agent.angularSpeed = 300f; // Velocidad de rotaci�n hacia el objetivo
        agent.stoppingDistance = 0.1f; // Para evitar detenerse demasiado pronto

        // Comienza a deambular inmediatamente
        SetNewRandomWanderDestination();
    }

    void Update()
    {
        // Reacciona a la posici�n del jugador
        ReactToPlayer();

        // Intenta disparar al jugador si est� dentro del rango
        if (isPlayerInSight && Time.time - lastShootTime > shootCooldown)
        {
            ShootAtPlayer();
        }

        // Verifica si la tecla "Q" es presionada para activar la reducci�n de salud de todos los enemigos
        if (Input.GetKeyDown(KeyCode.Q))
        {
            MakeAllEnemiesLoseHealth();
        }
    }

    public void ReactToPlayer()
    {
        if (player != null)
        {
            if (IsPlayerInSight(player.position))
            {
                // El jugador est� a la vista, el slime comienza a acercarse o disparar
                if (!isPlayerInSight)
                {
                    isPlayerInSight = true;
                    lastKnownPlayerPosition = player.position; // Guardar la �ltima posici�n conocida
                    ApproachPlayer(lastKnownPlayerPosition);
                }
                else
                {
                    // Contin�a actualizando la �ltima posici�n conocida mientras el jugador est� a la vista
                    lastKnownPlayerPosition = player.position;
                    ApproachPlayer(lastKnownPlayerPosition);
                }
            }
            else
            {
                // El jugador sali� de la vista, deambular� aleatoriamente
                if (isPlayerInSight)
                {
                    isPlayerInSight = false;
                    Wander();
                }
                else
                {
                    // Si el jugador ya no est� a la vista, sigue deambulando
                    Wander();
                }
            }
        }
    }

    private void ApproachPlayer(Vector3 targetPosition)
    {
        // Calculate the direction from the slime to the player
        Vector3 directionToPlayer = targetPosition - transform.position;

        // Normalize the direction to maintain the 5f distance
        directionToPlayer.y = 0; // We don't want the slime to move vertically
        directionToPlayer.Normalize();

        // Set the position to be 5 units away from the player
        Vector3 targetPositionAdjusted = targetPosition - directionToPlayer * 5f;

        // Establece la destinaci�n del NavMeshAgent hacia la nueva posici�n ajustada
        agent.SetDestination(targetPositionAdjusted);

        // Rotate the slime to look at the player
        Quaternion targetRotation = Quaternion.LookRotation(targetPosition - transform.position);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, agent.angularSpeed * Time.deltaTime);
    }

    public void Wander()
    {
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            SetNewRandomWanderDestination();
        }
    }

    private void SetNewRandomWanderDestination()
    {
        Vector3 randomDirection = new Vector3(Random.Range(-10f, 10f), 0, Random.Range(-10f, 10f)); // Rango aumentado
        Vector3 newWanderTarget = transform.position + randomDirection;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(newWanderTarget, out hit, 2f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
        else
        {
            SetNewRandomWanderDestination();
        }
    }

    public bool IsPlayerInSight(Vector3 playerPosition)
    {
        Vector3 directionToPlayer = playerPosition - transform.position;

        if (directionToPlayer.magnitude < sightRange)
        {
            float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);

            if (angleToPlayer < sightAngle)
            {
                return true;
            }
        }

        return false;
    }

    private void ShootAtPlayer()
    {
        // Verifica que el proyectil est� disponible
        if (projectilePrefab != null)
        {
            // Crea el proyectil en la posici�n del slime y lo orienta hacia el jugador
            GameObject projectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity);

            // Ignorar colisiones entre el proyectil y el slime
            Collider projectileCollider = projectile.GetComponent<Collider>();
            if (projectileCollider != null)
            {
                Collider slimeCollider = GetComponent<Collider>(); // Obtener el collider del shooter
                Physics.IgnoreCollision(projectileCollider, slimeCollider);
            }

            // Dirigir el proyectil hacia el jugador
            Vector3 directionToPlayer = (player.position - transform.position).normalized;

            // Aplicar velocidad al proyectil para que se mueva hacia el jugador
            Rigidbody rb = projectile.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = directionToPlayer * projectileSpeed;
            }

            lastShootTime = Time.time; // Actualiza el tiempo de la �ltima vez que dispar�
        }
        else
        {
            Debug.LogWarning("�No se ha asignado el prefab del proyectil!");
        }
    }


    public void TakeDamage(int damage)
    {
        Health -= damage;
        Debug.Log("�El slime ha recibido da�o! Salud actual: " + Health);

        if (Health <= 0)
        {
            Debug.Log("DEFEATED");
            Destroy(gameObject); // Destruye el slime cuando su salud llega a 0
        }
    }

    public void PerformAction()
    {
        Debug.Log("�Iniciando acci�n!");
        Attack();
    }

    public void Attack()
    {
        Debug.Log("�El slime ataca!");
    }

    private void MakeAllEnemiesLoseHealth()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        foreach (GameObject enemy in enemies)
        {
            IEnemy enemyScript = enemy.GetComponent<IEnemy>();
            if (enemyScript != null)
            {
                enemyScript.TakeDamage(1);
            }
        }
    }
    #endregion
}
