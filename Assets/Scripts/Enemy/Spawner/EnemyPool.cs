
public class EnemyPool : ObjectPool
{
    private void Awake()
    {
        ServiceLocator.Instance.Register<EnemyPool>(this);
        base.Awake(); 
    }

    public override void Initialize()
    {
        base.Initialize();
    }

    private void OnDestroy()
    {
        ServiceLocator.Instance.Unregister<EnemyPool>();
        base.OnDestroy();
    }
}