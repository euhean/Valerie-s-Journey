/// <summary>
/// Base class for all living game objects. Provides simple health/damage.
/// </summary>
public abstract class Entity : MonoBehaviour {
    public float health = 100f;

    public virtual void TakeDamage(float amount) {
        health -= amount;
        if (health <= 0f) {
            Die();
        }
    }

    protected virtual void Die() {
        Destroy(gameObject);
    }
}