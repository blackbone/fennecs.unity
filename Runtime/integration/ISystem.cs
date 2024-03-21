namespace fennecs.integration
{
    public interface ISystem
    {
        void OnAttachToWorld(World world);
        void OnDetachFromWorld(World world);
        
        void Execute();
    }
}