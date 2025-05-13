public class EnemyPool : ObjectPool<EnemySetup>, IStartable
{
    private void Awake()
    {
        UpdateManager.Instance.RegisterStartable(this);
    }

    public override void Initialize()
    {
        base.Initialize();
    }

    private void OnDestroy()
    {
        UpdateManager.Instance.UnregisterStartable(this);
    }
}
