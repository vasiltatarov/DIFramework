using System;

namespace DIFramework.Modules
{
    public interface IModule
    {
        void Configure();

        Type GetMapping(Type someClass, object attribute);

        object GetInstance(Type type);

        void SetInstance(Type implementation, object instance);
    }
}
