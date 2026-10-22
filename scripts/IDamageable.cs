namespace ArenaShooter;

/// <summary>Anything a projectile (or other damage source) can hurt.</summary>
public interface IDamageable
{
    void TakeDamage(float amount);
}
