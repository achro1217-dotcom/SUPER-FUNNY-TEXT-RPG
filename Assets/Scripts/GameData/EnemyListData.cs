using System.Collections.Generic;

public sealed class EnemyListData : IdIndexedListData<Enemy>
{
    public IReadOnlyList<Enemy> Enemies => Items;

    public EnemyListData(IReadOnlyList<Enemy> enemies)
        : base(enemies, enemy => enemy.Id, "Enemy")
    {
    }
}
