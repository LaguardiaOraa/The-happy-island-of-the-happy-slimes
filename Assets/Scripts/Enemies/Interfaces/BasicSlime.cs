using UnityEngine;
using UnityEngine.AI;

public class BasicSlime : MonoBehaviour, IEnemy
{
    #region Fields
    // Campos p�blicos para salud, rango de visi�n y velocidades de movimiento
    public int Health { get; set; } = 1; // La salud del slime
    public int sightRange = 20; // Rango de visi�n predeterminado (en unidades)
    public int sightAngle = 45; // �ngulo de visi�n predeterminado (en grados)
    public float wanderSpeed = 1f; // Velocidad al deambular
    public float approachSpeed = 2f; // Velocidad al acercarse al jugador
    public float playerTrackingRange = 5f; // Rango en el que el slime dejar� de seguir al jugador

    // Campos privados para referencias a otros objetos y l�gica de movimiento
    private Transform player; // Referencia al Transform del jugador
    private NavMeshAgent agent; // NavMeshAgent para el movimiento
    private Vector3 lastKnownPlayerPosition; // Guardar la �ltima posici�n conocida del jugador
    private bool isPlayerInSight = false; // Controla si el jugador est� a la vista
    #endregion

    #region Unity Methods
    /// <summary>
    /// Inicializa las referencias y configura el NavMeshAgent para el movimiento del slime.
    /// </summary>
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

    /// <summary>
    /// Verifica si el jugador est� a la vista y maneja el movimiento en consecuencia.
    /// Tambi�n verifica la entrada de teclas (Q) para activar la reducci�n de salud para todos los enemigos.
    /// </summary>
    void Update()
    {
        // Reacciona a la posici�n del jugador
        ReactToPlayer();

        // Verifica si la tecla "Q" es presionada para activar la reducci�n de salud de todos los enemigos
        if (Input.GetKeyDown(KeyCode.Q))
        {
            MakeAllEnemiesLoseHealth();
        }
    }

    /// <summary>
    /// Maneja el comportamiento del slime en funci�n de la posici�n del jugador.
    /// Si el jugador est� a la vista, el slime se acerca al jugador. 
    /// Si no, deambular� aleatoriamente.
    /// </summary>
    public void ReactToPlayer()
    {
        // Asegura que 'player' est� correctamente asignado
        if (player != null)
        {
            if (IsPlayerInSight(player.position))
            {
                // El jugador est� a la vista, el slime comienza a acercarse
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

    /// <summary>
    /// Hace que el slime se acerque a la posici�n del objetivo (�ltima posici�n conocida del jugador).
    /// </summary>
    /// <param name="targetPosition">La posici�n del objetivo hacia donde se mover� el slime.</param>
    private void ApproachPlayer(Vector3 targetPosition)
    {
        // Establece la destinaci�n del NavMeshAgent hacia la posici�n del objetivo
        agent.SetDestination(targetPosition);

        // Rota el slime para mirar hacia la posici�n objetivo (ignorando el eje vertical)
        Vector3 directionToTarget = new Vector3(targetPosition.x - transform.position.x, 0, targetPosition.z - transform.position.z);
        if (directionToTarget.magnitude > 0.1f) // Evita rotaciones innecesarias
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, agent.angularSpeed * Time.deltaTime);
        }
    }

    /// <summary>
    /// Hace que el slime deambule aleatoriamente por el mapa.
    /// Si el slime no se est� moviendo hacia un destino, encontrar� un nuevo destino aleatorio.
    /// </summary>
    public void Wander()
    {
        // Si el agente no est� ya yendo hacia un destino, establece un nuevo destino de deambulaci�n
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            SetNewRandomWanderDestination();
        }
    }

    /// <summary>
    /// Establece un nuevo destino aleatorio dentro de un rango especificado.
    /// La posici�n del objetivo se elige aleatoriamente alrededor de la posici�n actual del slime.
    /// </summary>
    private void SetNewRandomWanderDestination()
    {
        // Genera una posici�n aleatoria en un radio m�s grande alrededor del slime
        Vector3 randomDirection = new Vector3(Random.Range(-10f, 10f), 0, Random.Range(-10f, 10f)); // Rango aumentado
        Vector3 newWanderTarget = transform.position + randomDirection;

        // Asegura que el nuevo objetivo est� sobre el NavMesh
        NavMeshHit hit;
        if (NavMesh.SamplePosition(newWanderTarget, out hit, 2f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
        else
        {
            // Si no se puede encontrar un punto v�lido, intenta de nuevo
            SetNewRandomWanderDestination();
        }
    }

    /// <summary>
    /// Verifica si el jugador est� dentro del rango de visi�n y el campo de visi�n del slime.
    /// </summary>
    /// <param name="playerPosition">La posici�n del jugador.</param>
    /// <returns>True si el jugador est� a la vista, false si no.</returns>
    public bool IsPlayerInSight(Vector3 playerPosition)
    {
        // Calcula la direcci�n desde el enemigo hacia el jugador
        Vector3 directionToPlayer = playerPosition - transform.position;

        // Verifica si el jugador est� dentro del rango de visi�n
        if (directionToPlayer.magnitude < sightRange)
        {
            // Calcula el �ngulo entre la direcci�n hacia el jugador y la direcci�n hacia el frente del slime
            float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);

            // Si el �ngulo est� dentro del campo de visi�n (por ejemplo, 45 grados), el jugador est� a la vista
            if (angleToPlayer < sightAngle)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Reduce la salud del slime por la cantidad de da�o especificada.
    /// Si la salud llega a 0, el slime es derrotado y destruido.
    /// </summary>
    /// <param name="damage">La cantidad de da�o a reducir de la salud.</param>
    public void TakeDamage(int damage)
    {
        Health -= damage;
        Debug.Log("�El slime ha recibido da�o! Salud actual: " + Health);

        if (Health <= 0)
        {
            Debug.Log("DEFEATADO");
            Destroy(gameObject); // Destruye el slime cuando su salud llega a 0
        }
    }

    /// <summary>
    /// Funci�n de acci�n para realizar alguna acci�n (por ejemplo, atacar o animaci�n de reposo).
    /// </summary>
    public void PerformAction()
    {
        Debug.Log("�Iniciando acci�n!");
        Attack();
    }

    /// <summary>
    /// Funci�n de ataque del slime.
    /// </summary>
    public void Attack()
    {
        Debug.Log("�El slime ataca!");
    }
    #endregion

    #region Debugging Methods
    /// <summary>
    /// Reduce la salud de todos los enemigos con la etiqueta "Enemy" en 1 cuando se presiona la tecla "Q".
    /// Este m�todo solo est� disponible en el Editor de Unity.
    /// </summary>
    private void MakeAllEnemiesLoseHealth()
    {
        // Encuentra todos los objetos con la etiqueta "Enemy"
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        // Itera a trav�s de todos los enemigos y reduce su salud en 1
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
