using Microsoft.Extensions.DependencyInjection;

using PipelineRD.Async;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PipelineRD.Builders
{
    using Enums;

    public class PipelineServicesBuilder : IPipelineServicesBuilder
    {
        public IEnumerable<TypeInfo> Types { get; private set; }
        public IServiceCollection Services { get; private set; }
        public bool ValidatorsAlreadySet { get; set; }

        private bool _contextsAlreadySet;
        private bool _stepsAlreadySet;
        private bool _pipelinesAlreadySet;
        private bool _initializersAlreadySet;
        private bool _buildersAlreadySet;

        public PipelineServicesBuilder(IEnumerable<TypeInfo> types, IServiceCollection services)
        {
            Types = types;
            Services = services;
        }

        public void InjectAll(InjectionLifetime? lifetime = null)
        {
            lifetime ??= InjectionLifetime.Scoped;
            InjectContexts(lifetime);
            InjectSteps(lifetime);
            InjectPipelineBuilders(lifetime);
            InjectPipelineInitializers(lifetime);
            InjectPipelines(lifetime);
        }

        public void InjectContexts(InjectionLifetime? lifetime = null)
        {
            lifetime ??= InjectionLifetime.Scoped;
            if (_contextsAlreadySet) return;

            var contexts = Types.Where(a => a.IsClass && a.BaseType == typeof(BaseContext));

            var duplicatedContexts = contexts.GroupBy(x => x.Name)
                .Where(x => x.Count() > 1)
                .Select(x => x.Key);

            if (duplicatedContexts.Any())
            {
                throw new Exception($"It cannot exists two pipeline contexts with the same name. Please, change the name of the context(s): {string.Join(", ", duplicatedContexts)}");
            }

            foreach (var context in contexts)
                switch(lifetime)
                {
                    case InjectionLifetime.Singleton:
                    {
                        Services.AddSingleton(context.AsType());
                        break;
                    }
                    case InjectionLifetime.Scoped:
                    {
                        Services.AddScoped(context.AsType());
                        break;
                    }
                    case InjectionLifetime.Transient:
                    {
                        Services.AddTransient(context.AsType());
                        break;
                    }
                    default: break;
                }
               

            _contextsAlreadySet = true;
        }

        public void InjectSteps(InjectionLifetime? lifetime = null)
        {
            lifetime ??= InjectionLifetime.Scoped;
            if (_stepsAlreadySet) return;

            var searchClasses = new Type[] { typeof(RequestStep<>), typeof(RollbackRequestStep<>), typeof(AsyncRequestStep<>) };

            var steps = Types
                .Where(x => !x.IsAbstract && x.IsClass && searchClasses.Any(t => IsSubclassOfGeneric(x, t)))
                .Select(x => new
                {
                    Type = x.AsType(),
                    Interface = GetInterfaces(x, false).FirstOrDefault()
                });

            foreach (var step in steps)
            {
                switch(lifetime)
                {
                    case InjectionLifetime.Singleton:
                    {
                        Services.AddSingleton(step.Interface, step.Type);
                        break;
                    }
                    case InjectionLifetime.Scoped:
                    {
                        Services.AddScoped(step.Interface, step.Type);
                        break;
                    }
                    case InjectionLifetime.Transient:
                    {
                        Services.AddTransient(step.Interface, step.Type);
                        break;
                    }
                    default: break;
                }
   
            }

            _stepsAlreadySet = true;
        }

        public void InjectPipelines(InjectionLifetime? lifetime = null)
        {
            lifetime ??= InjectionLifetime.Scoped;
            if (_pipelinesAlreadySet) return;

             
            switch(lifetime)
            {
                case InjectionLifetime.Singleton:
                {
                    Services.AddSingleton(typeof(IPipeline<>), typeof(Pipeline<>));
                    break;
                }
                case InjectionLifetime.Scoped:
                {
                    Services.AddScoped(typeof(IPipeline<>), typeof(Pipeline<>));
                    break;
                }
                case InjectionLifetime.Transient:
                {
                    Services.AddTransient(typeof(IPipeline<>), typeof(Pipeline<>));
                    break;
                }
                default: break;
            }
            _pipelinesAlreadySet = true;
        }

        public void InjectPipelineInitializers(InjectionLifetime? lifetime = null)
        {
            lifetime ??= InjectionLifetime.Scoped;
            if (_initializersAlreadySet) return;
             
            switch(lifetime)
            {
                case InjectionLifetime.Singleton:
                {
                    Services.AddSingleton(typeof(IPipelineInitializer<>), typeof(PipelineInitializer<>));
                    break;
                }
                case InjectionLifetime.Scoped:
                {
                    Services.AddScoped(typeof(IPipelineInitializer<>), typeof(PipelineInitializer<>));
                    break;
                }
                case InjectionLifetime.Transient:
                {
                    Services.AddTransient(typeof(IPipelineInitializer<>), typeof(PipelineInitializer<>));
                    break;
                }
                default: break;
            }
            _initializersAlreadySet = true;
        }

        public void InjectPipelineBuilders(InjectionLifetime? lifetime = null)
        {
            lifetime ??= InjectionLifetime.Scoped;
            if (_buildersAlreadySet) return;

            var pipelineBuilders = from type in Types
                                   where !type.IsAbstract && !type.IsGenericTypeDefinition
                                   let interfaces = type.GetInterfaces()
                                   let genericInterfaces = interfaces.Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPipelineBuilder<>))
                                   let matchingInterface = interfaces.FirstOrDefault()
                                   where genericInterfaces.Any()
                                   select new { Interface = matchingInterface, Type = type };

            foreach (var builder in pipelineBuilders)
            {
                switch(lifetime)
                {
                    case InjectionLifetime.Singleton:
                    {
                        Services.AddSingleton(builder.Interface, builder.Type);
                        break;
                    }
                    case InjectionLifetime.Scoped:
                    {
                        Services.AddScoped(builder.Interface, builder.Type);
                        break;
                    }
                    case InjectionLifetime.Transient:
                    {
                        Services.AddTransient(builder.Interface, builder.Type);
                        break;
                    }
                    default: break;
                }
            }
                
            
            _buildersAlreadySet = true;
        }

        #region Generic helpers methods 
        private static bool IsSubclassOfGeneric(Type current, Type genericBase)
        {
            do
            {
                if (current.IsGenericType && current.GetGenericTypeDefinition() == genericBase)
                    return true;
            }
            while ((current = current.BaseType) != null);
            return false;
        }

        public static IEnumerable<Type> GetInterfaces(Type type, bool includeInherited)
        {
            if (includeInherited || type.BaseType == null)
                return type.GetInterfaces();
            else
                return type.GetInterfaces().Except(type.BaseType.GetInterfaces());
        }
        #endregion
    }
}
