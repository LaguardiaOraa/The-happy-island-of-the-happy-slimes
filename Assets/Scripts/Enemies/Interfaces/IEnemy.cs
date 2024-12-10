/// <summary>
/// Cosas en comun entre enemigos
/// </summary>
public interface IEnemy
{
    // Propiedad para gestionar la salud del enemigo
    int Health { get; set; }

    // M�todo para recibir da�o
    void TakeDamage(int damage);

    // M�todo para realizar alguna acci�n (por ejemplo, atacar)
    void PerformAction();

    // M�todo de ataque
    void Attack();

    // M�todo de movimiento o deambule
    void Wander();
}