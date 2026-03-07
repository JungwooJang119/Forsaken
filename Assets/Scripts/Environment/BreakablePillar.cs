using UnityEngine;

public class BreakablePillar : MonoBehaviour, IDamageable
{
    [SerializeField] private int health;
    private float totalHealth;
    private float cooldown = 0;

    public int Health {get {return health; } set {health = value;}}
    public float Cooldown {get {return cooldown; } set {cooldown = value;}}

    void Start()
    {
       totalHealth = health; 
    }
    public void ApplyDamage(int damage)
    {
        health -= damage;
        float t = health / totalHealth;
        Color oldColor = gameObject.GetComponent<SpriteRenderer>().color;
        gameObject.GetComponent<SpriteRenderer>().color = oldColor * t;
        if (health <= 0)
        {
            Destroy(gameObject);
        }
    }
}
