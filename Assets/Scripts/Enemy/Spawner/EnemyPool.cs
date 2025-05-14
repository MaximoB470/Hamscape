public class EnemyPool : ObjectPool<EnemySetup>, IStartable
{
    private void Awake()
    {
        ServiceLocator.Instance.Register<EnemyPool>(this);
        UpdateManager.Instance.RegisterStartable(this);
    }

    public override void Initialize()
    {
        base.Initialize();
    }

    private void OnDestroy()
    {
        ServiceLocator.Instance.Unregister<EnemyPool>();
        UpdateManager.Instance.UnregisterStartable(this);
    }
}
