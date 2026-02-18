using System;
using System.Collections.Generic;

public sealed class EnemyListData
{
    private readonly Dictionary<string, Enemy> _enemyById;

    public IReadOnlyList<Enemy> Enemies { get; }

    public EnemyListData(IReadOnlyList<Enemy> enemies)
    {
        Enemies = enemies ?? throw new ArgumentNullException(nameof(enemies));
        _enemyById = BuildEnemyByIdIndex(enemies);
    }

    public bool ContainsId(string id)
    {
        ValidateId(id);
        return _enemyById.ContainsKey(id);
    }

    public bool TryGetById(string id, out Enemy enemy)
    {
        ValidateId(id);
        return _enemyById.TryGetValue(id, out enemy);
    }

    public Enemy GetById(string id)
    {
        ValidateId(id);
        if (_enemyById.TryGetValue(id, out Enemy enemy))
        {
            return enemy;
        }

        throw new KeyNotFoundException($"Enemy not found. id={id}");
    }

    private static Dictionary<string, Enemy> BuildEnemyByIdIndex(IReadOnlyList<Enemy> enemies)
    {
        Dictionary<string, Enemy> enemyById = new Dictionary<string, Enemy>(enemies.Count);
        for (int i = 0; i < enemies.Count; i++)
        {
            Enemy enemy = enemies[i] ?? throw new ArgumentException("Enemy list cannot contain null entries.", nameof(enemies));
            if (enemyById.ContainsKey(enemy.Id))
            {
                throw new ArgumentException($"Duplicate enemy id detected. id={enemy.Id}", nameof(enemies));
            }

            enemyById.Add(enemy.Id, enemy);
        }

        return enemyById;
    }

    private static void ValidateId(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Enemy id cannot be null or empty.", nameof(id));
        }
    }
}
