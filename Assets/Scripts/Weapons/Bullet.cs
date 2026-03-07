using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private int damage = 10;
    [SerializeField] private float lifetime = 5f;

    private Rigidbody2D rb;

    public void Initialize(Vector2 direction, float speed)
    {
        rb = GetComponent<Rigidbody2D>();
        
        rb.linearVelocity = direction * speed;

        // Rotate the bullet to face the direction
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
        Destroy(gameObject, lifetime);

    }

    void OnTriggerEnter2D(Collider2D other)
    {
        string layer = LayerMask.LayerToName(other.gameObject.layer);
        if (layer == "Enemies" || other.gameObject.CompareTag("Breakable"))
        {
            IDamageable damageable = other.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.ApplyDamage(damage);
                Destroy(gameObject);
            }
        }
    }
}