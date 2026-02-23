public sealed class Enemy
{
    public string Id { get; private set; }
    public int Hp { get; private set; }
    public int Attack { get; private set; }
    public int Defense { get; private set; }
    public EnemyGrade Grade { get; private set; }
}
