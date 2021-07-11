using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;

namespace PipelineRD.Converters
{
    public abstract class JsonObjectCreationConverter<TEntity> : JsonConverter
    {
        public string Property { get; set; }

        public Dictionary<string, Type> TypesMapping { get; set; }

        protected TEntity Create(Type objectType, JObject jsonObject)
        {
            JToken token;
            if (!jsonObject.TryGetValue(this.Property, out token))
            {
                return (TEntity)Activator.CreateInstance(objectType);
            }

            foreach (var type in this.TypesMapping)
            {
                if (type.Key.Equals(token.ToString()))
                {
                    return (TEntity)Activator.CreateInstance(type.Value);
                }
            }

            return (TEntity)Activator.CreateInstance(objectType);
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(TEntity).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType,
            object existingValue, JsonSerializer serializer)
        {
            var jsonObject = JObject.Load(reader);
            var target = Create(objectType, jsonObject);
            serializer.Populate(jsonObject.CreateReader(), target);
            return target;
        }

        public override bool CanWrite { get { return false; } }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
