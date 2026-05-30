using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 100f;
    public float currentHealth = 100f;
    public Canvas canvasEnd;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        if (amount <= 0f) return;

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        Debug.Log($"PlayerHealth: recibe {amount} de daño. Vida actual: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log("PlayerHealth: el jugador ha muerto.");
        // Aquí puedes desactivar el jugador, reproducir animación, reiniciar la escena, etc.
        gameObject.SetActive(false);
        canvasEnd.gameObject.SetActive(true);
    }
}
