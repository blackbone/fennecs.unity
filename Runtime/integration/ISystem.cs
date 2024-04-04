namespace fennecs.integration
{
    public interface ISystem
    {
        void OnAttachToWorld(World world);
        void OnDetachFromWorld(World world);
    }

    public interface IPreUpdateSystem : ISystem
    {
        void PreUpdateExecute();
    }

    public interface IUpdateSystem : ISystem
    {
        void UpdateExecute();
    }

    public interface IPostUpdateSystem : ISystem
    {
        void PostUpdateExecute();
    }
}