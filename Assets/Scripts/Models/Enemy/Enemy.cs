using System;

public sealed class Enemy
{
    public string Id { get; }
    public int Hp { get; }
    public int AttackPower { get; }

    public Enemy(string id, int hp, int attackPower)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Enemy id cannot be null or empty.", nameof(id));
        }

        if (hp < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(hp));
        }

        if (attackPower < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(attackPower));
        }

        Id = id;
        Hp = hp;
        AttackPower = attackPower;
    }
}
