using Microsoft.Extensions.DependencyInjection;

using PipelineRD.Async;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PipelineRD.Builders
{
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

        public void InjectAll()
        {
            InjectContexts();
            InjectSteps();
            InjectPipelineBuilders();
            InjectPipelineInitializers();
            InjectPipelines();
        }

        public void InjectContexts()
        {
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
                Services.AddScoped(context.AsType());

            _contextsAlreadySet = true;
        }

        public void InjectSteps()
        {
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
                Services.AddScoped(step.Interface, step.Type);
            }

            _stepsAlreadySet = true;
        }

        public void InjectPipelines()
        {
            if (_pipelinesAlreadySet) return;

            Services.AddScoped(typeof(IPipeline<>), typeof(Pipeline<>));
            _pipelinesAlreadySet = true;
        }

        public void InjectPipelineInitializers()
        {
            if (_initializersAlreadySet) return;
            Services.AddScoped(typeof(IPipelineInitializer<>), typeof(PipelineInitializer<>));
            _initializersAlreadySet = true;
        }

        public void InjectPipelineBuilders()
        {
            if (_buildersAlreadySet) return;

            var pipelineBuilders = from type in Types
                                   where !type.IsAbstract && !type.IsGenericTypeDefinition
                                   let interfaces = type.GetInterfaces()
                                   let genericInterfaces = interfaces.Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPipelineBuilder<>))
                                   let matchingInterface = interfaces.FirstOrDefault()
                                   where genericInterfaces.Any()
                                   select new { Interface = matchingInterface, Type = type };

            foreach (var builder in pipelineBuilders)
                Services.AddScoped(builder.Interface, builder.Type);

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
