using System;
using System.Linq;
using System.Reflection;
using DIFramework.Attributes;
using DIFramework.Modules;

namespace DIFramework.Injectors
{
    public class Injector
    {
        private IModule _module;

        public Injector(IModule module)
        {
            _module = module;
        }

        public TClass Inject<TClass>()
        {
            var hasConstructorAttribute = CheckForConstructorInjection<TClass>();
            var hasFieldAttribute = CheckForFieldInjection<TClass>();

            if (hasConstructorAttribute && hasFieldAttribute)
            {
                throw new ArgumentException("There must be only field or constructor annotated with inject attribute");
            }

            if (hasConstructorAttribute)
            {
                return CreateConstructorInjection<TClass>();
            }
            else if (hasFieldAttribute)
            {
                return CreateFieldInjection<TClass>();
            }

            return default(TClass);
        }

        private TClass CreateFieldInjection<TClass>()
        {
            Type desireClass = typeof(TClass);
            var desireClassInstance = _module.GetInstance(desireClass);

            if (desireClassInstance == null)
            {
                desireClassInstance = Activator.CreateInstance(desireClass);
                _module.SetInstance(desireClass, desireClassInstance);
            }

            var fields = desireClass.GetFields((BindingFlags)62);

            foreach (var fieldInfo in fields)
            {
                if (!CheckForFieldInjection<TClass>())
                {
                    continue;
                }

                if (fieldInfo.GetCustomAttributes(typeof(Inject), true).Any())
                {
                    var injection = (Inject)fieldInfo
                        .GetCustomAttributes(typeof(Inject), true)
                        .FirstOrDefault();
                    Type dependency = null;

                    var named = fieldInfo.GetCustomAttribute(typeof(Named), true);
                    Type type = fieldInfo.FieldType;

                    if (named == null)
                    {
                        dependency = _module.GetMapping(type, injection);
                    }
                    else
                    {
                        dependency = _module.GetMapping(type, named);
                    }

                    if (type.IsAssignableFrom(dependency))
                    {
                        object instance = _module.GetInstance(dependency);

                        if (instance != null)
                        {
                            instance = Activator.CreateInstance(dependency);
                            _module.SetInstance(dependency, instance);
                        }

                        fieldInfo.SetValue(desireClassInstance, instance);
                    }
                }
            }

            return (TClass)desireClassInstance;
        }

        private TClass CreateConstructorInjection<TClass>()
        {
            var desireClass = typeof(TClass);

            if (desireClass == null)
            {
                return default(TClass);
            }

            var constructors = desireClass.GetConstructors();

            foreach (var constructorInfo in constructors)
            {
                if (!CheckForConstructorInjection<TClass>())
                {
                    continue;
                }

                var inject = (Inject)constructorInfo
                    .GetCustomAttributes(typeof(Inject), true)
                    .FirstOrDefault();
                var parameterTypes = constructorInfo.GetParameters();
                var constructorParams = new object[parameterTypes.Length];

                var i = 0;

                foreach (var parameter in parameterTypes)
                {
                    var named = parameter.GetCustomAttribute(typeof(Named));
                    Type dependency = null;

                    if (named == null)
                    {
                        dependency = _module.GetMapping(parameter.ParameterType, inject);
                    }
                    else
                    {
                        dependency = _module.GetMapping(parameter.ParameterType, named);
                    }

                    if (parameter.ParameterType.IsAssignableFrom(dependency))
                    {
                        object instance = _module.GetInstance(dependency);

                        if (instance != null)
                        {
                            constructorParams[i++] = instance;
                        }
                        else
                        {
                            instance = Activator.CreateInstance(dependency);
                            constructorParams[i++] = instance;
                            _module.SetInstance(parameter.ParameterType, instance);
                        }
                    }
                }

                return (TClass)Activator.CreateInstance(desireClass, constructorParams);
            }

            return default(TClass);
        }

        private bool CheckForFieldInjection<TClass>()
            => typeof(TClass)
                .GetFields((BindingFlags)62)
                .Any(f => f.GetCustomAttributes(typeof(Inject), true)
                    .Any());

        private bool CheckForConstructorInjection<TClass>()
            => typeof(TClass)
                .GetConstructors()
                .Any(c => c.GetCustomAttributes(typeof(Inject), true)
                    .Any());
    }
}
