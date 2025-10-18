using System.ComponentModel;

namespace Modules.ObjectPoolSystem
{
    public interface IPoolable : IComponent
    {
        void Initilize();

        void Dispose();
    }
}
