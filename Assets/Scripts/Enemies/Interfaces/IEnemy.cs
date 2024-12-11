/// <summary>
/// Cosas en comun entre enemigos
/// </summary>
public interface IEnemy
{
    // Propiedad para gestionar la salud del enemigo
    int Health { get; set; }

    // M�todo para recibir da�o
    void TakeDamage(int damage);

    // M�todo de movimiento o deambule
    void Wander();
}