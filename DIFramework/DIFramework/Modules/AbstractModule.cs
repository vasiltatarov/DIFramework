using System;
using System.Collections.Generic;
using System.Linq;
using DIFramework.Attributes;

namespace DIFramework.Modules
{
    public abstract class AbstractModule : IModule
    {
        private IDictionary<Type, Dictionary<string, Type>> _implementations;
        private IDictionary<Type, object> _instances;

        protected AbstractModule()
        {
            _implementations = new Dictionary<Type, Dictionary<string, Type>>();
            _instances = new Dictionary<Type, object>();
        }

        public abstract void Configure();

        public Type GetMapping(Type someClass, object attribute)
        {
            Dictionary<string, Type> currentImplementation = _implementations[someClass];

            Type type = null;

            if (attribute is Inject)
            {
                if (currentImplementation.Count == 1)
                {
                    type = currentImplementation.Values.First();
                }
                else
                {
                    throw new ArgumentException($"No available mapping for class: {someClass.FullName}");
                }
            }
            else if (attribute is Named)
            {
                Named named = attribute as Named;

                string dependencyName = named.Name;
                type = currentImplementation[dependencyName];
            }

            return type;
        }

        public object GetInstance(Type type)  
        {
            _instances.TryGetValue(type, out object value);

            return value;
        }

        public void SetInstance(Type implementation, object instance)
        {
            if (!_instances.ContainsKey(implementation))
            {
                _instances.Add(implementation, instance);
            }
        }

        protected void CreateMapping<TInter, TImpl>()
        {
            if (!_implementations.ContainsKey(typeof(TInter)))
            {
                _implementations.Add(typeof(TInter), new Dictionary<string, Type>());
            }

            _implementations[typeof(TInter)].Add(typeof(TImpl).Name, typeof(TImpl));
        }
    }
}
