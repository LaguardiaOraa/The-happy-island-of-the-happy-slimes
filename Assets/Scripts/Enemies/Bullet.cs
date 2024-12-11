using UnityEngine;

public class Bullet : MonoBehaviour
{
    #region Fields
    public float lifeTime = 5f; // Tiempo de vida del proyectil antes de ser destruido
    public int damage = 1; // Da�o que causar� el proyectil
    private Rigidbody rb; // Rigidbody del proyectil para moverlo
    #endregion

    #region Unity Methods
    void Start()
    {
        // Obt�n el Rigidbody para poder mover la bala
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("El proyectil debe tener un Rigidbody para moverse.");
        }

        // Destruye el proyectil despu�s de un tiempo para evitar que se quede en la escena indefinidamente
        Destroy(gameObject, lifeTime);
    }

    void OnCollisionEnter(Collision collision)
    {
        // Verifica si el proyectil ha chocado con un enemigo
        if (collision.gameObject.CompareTag("Enemy"))
        {
            // Obt�n el componente IEnemy del enemigo (como en el caso del slime)
            IEnemy enemy = collision.gameObject.GetComponent<IEnemy>();
            if (enemy != null)
            {
                // Llama a la funci�n TakeDamage del enemigo, aplicando el da�o
                enemy.TakeDamage(damage);
            }

            // Destruye la bala al impactar
            Destroy(gameObject);
        }

        // Destruye la bala si colisiona con cualquier otro objeto
        else if (collision.gameObject.CompareTag("Environment"))
        {
            Destroy(gameObject);
        }
    }
    #endregion
}
