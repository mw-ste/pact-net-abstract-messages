namespace PactNetAbstactMessages;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;

public class MatcherWrapperConverter : JsonConverter
{
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        var json = new JObject();

        var matcherWrapper = (IMatcherWrapper)value;

        if (serializer.TypeNameHandling != TypeNameHandling.None)
        {
            var type = matcherWrapper.ContentType;
            // $type needs to be first key!
            json["$type"] = $"{type}, {type.Assembly.GetName().Name}";
        }

        foreach ((string key, object obj) in matcherWrapper)
        {
            // property names need to match the used CamelCasePropertyNamesContractResolver 
            var k = System.Text.Json.JsonNamingPolicy.CamelCase.ConvertName(key);
            json[k] = JToken.FromObject(obj, serializer);
        }

        json.WriteTo(writer);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) =>
        throw new NotImplementedException();

    public override bool CanConvert(Type objectType) =>
        objectType
            .GetInterfaces()
            .Contains(typeof(IMatcherWrapper));
}
