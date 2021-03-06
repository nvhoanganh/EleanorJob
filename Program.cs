using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using static System.Text.Json.JsonElement;

namespace parser
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("dotnet run -- example1.json mapper2.json");
                return;
            }

            string formRspText = System.IO.File.ReadAllText($"./examples/{args[0]}");
            string mapperText = System.IO.File.ReadAllText($"./examples/{args[1]}");

            EleanorJob.Process(formRspText, mapperText);
        }
    }

    static class EleanorJob
    {
        public static void Process(string formResp, string mapperDef)
        {
            Console.WriteLine("🏁🏁🏁🏁🏁🏁 Starting Eleanor Job 🏁🏁🏁🏁🏁🏁");

            var nestedJson = HelperClass.ConvertToNestedJson(formResp);
            var input = JsonSerializer.Deserialize<Dictionary<string, dynamic>>(nestedJson);

            var definition = JsonSerializer.Deserialize<Dictionary<string, dynamic>>(mapperDef);
            var vars = new Dictionary<string, dynamic>();
            var steps = definition["steps"].EnumerateArray();
            // for first step, payload is the same as input
            var payload = input;

            Process(input, input, vars, steps);

            Console.WriteLine("🏁🏁🏁🏁🏁🏁 DONE in 60 Seconds 🏁🏁🏁🏁🏁🏁");
        }

        public static void Process(
            Dictionary<string, dynamic> payload,
            Dictionary<string, dynamic> input,
            Dictionary<string, object> vars,
            ArrayEnumerator steps,
            string prefix = "")
        {
            var stepIndex = 1;
            foreach (JsonElement step in steps)
            {
                Console.WriteLine($"{prefix}Executing step {stepIndex}:{step.Get("type")}");
                Console.WriteLine($"- current $payload:\n{payload.Print()}");
                Console.WriteLine($"- current $vars:\n{vars.Print()}");
                IStep stepProcessor = HelperClass.StepFactoryCreate(step);
                Dictionary<string, dynamic> stepOutput = stepProcessor.Process(payload, input, vars);

                // output of this step then it will be input to the next step
                payload = stepOutput;
                Console.WriteLine($"{prefix}Completed step {stepIndex}:{step.Get("type")}");
                Console.WriteLine($"- after $payload:\n{payload.Print()}");
                Console.WriteLine($"- after $vars:\n{vars.Print()}");
                stepIndex++;
            }
        }

    }

    #region  Methods used for talking to CRM

    // this is the CRM entity
    class Entity
    {
        public Dictionary<string, object> Attributes { get; set; }
        public Entity(string name)
        {
            Attributes = new Dictionary<string, object>();
        }
        public Entity(string name, string id)
        {
            Attributes = new Dictionary<string, object>();
        }
    }

    interface ICrmService
    {
        string CreateEntity(string entityType, Dictionary<string, dynamic> properties);
        string UpdateEntity(string id, string entityType, Dictionary<string, dynamic> properties);
        string SearchCrmObject(string entityType, Dictionary<string, object> filters);
    }

    // to create simple CRM foreignkey, we need to specify these 2 fields
    class EntityReference
    {
        public string LogicalName { get; set; }
        public object Id { get; set; }
    }

    class MockCrmService : ICrmService
    {
        public string CreateEntity(string entityType, Dictionary<string, dynamic> properties)
        {
            // need to create
            var newEntity = new Entity(entityType);
            foreach (KeyValuePair<string, dynamic> prop in properties)
            {
                dynamic value = prop.Value.GetType() == typeof(JsonElement) ?
                    ((JsonElement)prop.Value).GetJsonElementValue() :
                    prop.Value;

                newEntity.Attributes.Add(prop.Key, value);
            }

            Console.WriteLine($"Creating new {entityType} using: \n{properties.Print()}");
            // call crm service and savef
            return Guid.NewGuid().ToString();
        }

        // search crm for matching object based on the search criteria
        public string SearchCrmObject(string entityType, Dictionary<string, object> filters)
        {
            // use the provided filfters to search for the matching CRM record
            Console.WriteLine($"Searching CRM for Entity {entityType}, using:\n {filters.Print()}");
            if (filters.Print().Contains("AlreadyExistArea")) return "FoundGUID1111222";
            // returns null if not found
            return null;
        }

        public string UpdateEntity(string id, string entityType, Dictionary<string, dynamic> properties)
        {
            // update crm using the provided information (similar to create )
            // return the same id
            var newEntity = new Entity(entityType, id);

            var sb = new StringBuilder("");

            foreach (KeyValuePair<string, dynamic> prop in properties)
            {
                dynamic value = prop.Value.GetType() == typeof(JsonElement) ?
                    ((JsonElement)prop.Value).GetJsonElementValue() :
                    prop.Value;

                newEntity.Attributes.Add(prop.Key, value);

                sb.AppendLine($"- {prop.Key} = {prop.Value} ({value.GetType()})");
            }
            Console.WriteLine($"Updating {entityType} ({id}) using: \n{sb}");
            return id;
        }
    }
    #endregion

    #region Eleanor Steps

    /// <summary>
    /// Step has simple interface
    /// </summary>
    interface IStep
    {
        Dictionary<string, dynamic> Process(Dictionary<string, dynamic> payload, Dictionary<string, dynamic> input, Dictionary<string, object> vars);
    }

    /// <summary>
    /// Transform JSON object using the expression in its value
    /// </summary>
    class Transformer : IStep
    {
        private readonly JsonElement _jsonSchema;

        public Transformer(JsonElement jsonSchema)
        {
            _jsonSchema = jsonSchema;
        }

        public Dictionary<string, dynamic> Process(Dictionary<string, dynamic> payload, Dictionary<string, dynamic> input, Dictionary<string, object> vars)
        {
            var dict = new Dictionary<string, dynamic>();

            foreach (JsonProperty prop in _jsonSchema.EnumerateObject())
            {
                var valueExpression = prop.Value.ToString();
                var vl = HelperClass.Evaluate(valueExpression, input, payload, vars);
                dict.Add(prop.Name, vl);
            }
            return dict;
        }
    }

    class CreateCrmEntity : IStep
    {
        private readonly string destinationEntity;
        private readonly ICrmService crmService;

        public CreateCrmEntity(string destinationEntity)
        {
            this.destinationEntity = destinationEntity;
            this.crmService = new MockCrmService();
        }

        public Dictionary<string, dynamic> Process(Dictionary<string, dynamic> payload, Dictionary<string, dynamic> input, Dictionary<string, object> vars)
        {
            return new Dictionary<string, dynamic> {
                {"Crm_Id", this.crmService.CreateEntity(destinationEntity, payload) }
            };
        }
    }

    class UpsertCrmEntity : IStep
    {
        private readonly string destinationEntity;
        private readonly ArrayEnumerator searchFields;
        private readonly ICrmService crmService;

        public UpsertCrmEntity(string destinationEntity, JsonElement searchFields)
        {
            this.destinationEntity = destinationEntity;
            this.searchFields = searchFields.EnumerateArray();
            this.crmService = new MockCrmService();
        }

        public Dictionary<string, dynamic> Process(Dictionary<string, dynamic> payload, Dictionary<string, dynamic> input, Dictionary<string, object> vars)
        {
            var searchFilters = new Dictionary<string, object>();
            foreach (JsonElement searchF in searchFields)
            {
                searchFilters.Add(searchF.ToString(), payload[searchF.ToString()]);
            }

            string crmId = this.crmService.SearchCrmObject(destinationEntity, searchFilters);

            if (string.IsNullOrEmpty(crmId))
            {
                return new Dictionary<string, dynamic> {
                    {"Crm_Id", this.crmService.CreateEntity(destinationEntity, payload) }
                };
            }
            else
            {
                return new Dictionary<string, dynamic> {
                    {"Crm_Id", this.crmService.UpdateEntity(crmId, destinationEntity, payload) }
                };
            }

        }
    }

    class SetVariable : IStep
    {
        private readonly string name;
        private readonly string expression;

        public SetVariable(string name, string expression)
        {
            this.name = name;
            this.expression = expression;
        }

        public Dictionary<string, dynamic> Process(Dictionary<string, dynamic> payload, Dictionary<string, dynamic> input, Dictionary<string, object> vars)
        {
            dynamic vl = HelperClass.Evaluate(this.expression, input, payload, vars);
            if (vars.ContainsKey(this.name))
            {
                Console.WriteLine($"Update variable {name}={vl}");
                vars[this.name] = vl;
            }
            else
            {
                Console.WriteLine($"Add variable {name}={vl}");
                vars.Add(this.name, vl);
            }
            // SetVariable doesn't modify the payload
            return payload;
        }
    }

    class ForEachStep : IStep
    {
        private readonly string collectionExpression;
        private readonly ArrayEnumerator steps;

        public ForEachStep(string collectionExpression, JsonElement steps)
        {
            this.collectionExpression = collectionExpression;
            this.steps = steps.EnumerateArray();
        }

        public Dictionary<string, dynamic> Process(Dictionary<string, dynamic> payload, Dictionary<string, dynamic> input, Dictionary<string, object> vars)
        {
            // for each loop through each value in the collectionExpression and apply each step
            Console.WriteLine($"- Looping through each item in : {collectionExpression}");
            var collectionItems = (JsonElement)HelperClass.Evaluate(collectionExpression, input, payload, vars);
            var i = 0;
            foreach (var item in collectionItems.EnumerateArray())
            {
                var scopePayloadJson = JsonSerializer.Serialize(item);
                Console.WriteLine($"Current Array Element: {scopePayloadJson}");
                var scopePayload = JsonSerializer.Deserialize<Dictionary<string, dynamic>>(scopePayloadJson);
                EleanorJob.Process(scopePayload, input, vars, steps, $"Scope - ForEach[{i}] : ");
                i++;
            }

            // unlike the scatter-gather component it does not modify the payload, so we just return the original here
            return payload;
        }
    }
    #endregion

    #region Helpers

    static class HelperClass
    {
        public static dynamic Evaluate(string expression, Dictionary<string, object> input, Dictionary<string, object> payload, Dictionary<string, object> vars)
        {
            Func<string, dynamic> ParseLevel1 = delegate (string name)
            {
                switch (name)
                {
                    case "$payload": return payload;
                    case "$input": return input;
                    case "$vars": return vars;
                    default:
                        return null;
                }
            };

            Func<string, dynamic> GetValueFromExpression = delegate (string express)
            {
                // e.g hardcoded values or access full properties
                if (expression.IndexOf('.') < 0)
                {
                    // hardcoded string
                    if (expression.StartsWith("'") && expression.EndsWith("'"))
                    {
                        return expression.Replace("'", "");
                    }
                    // just return the object 
                    return ParseLevel1(expression);
                }

                var parts = expression.Split('.');
                dynamic root = ParseLevel1(parts[0])[parts[1]];

                if (parts.Length >= 3)
                {
                    // dot notation express (e.g "$input.address.country.code")
                    // https://stackoverflow.com/questions/22669044/how-to-get-the-index-of-second-comma-in-a-string/22669242
                    var secondDot = expression.IndexOf('.', expression.IndexOf('.') + 1);
                    var subPath = expression.Substring(secondDot);

                    // we need to covert the value back to JSONElement so that we can evaluate it by using dot notation
                    var jsonStr = JsonSerializer.Serialize(root);
                    JsonElement ele = JsonSerializer.Deserialize<JsonElement>(jsonStr);

                    return ele.GetNestedJsonElement(subPath).GetJsonElementValue();
                }

                return root;
            };

            var regex = new Regex(@"(?<method>[^\(]*)(\((?<params>.*)\))[^\)]*");

            if (!regex.IsMatch(expression)) return GetValueFromExpression(expression);

            // e.g. int($payload.address.postcode)
            GroupCollection groups = regex.Match(expression).Groups;
            var method = groups["method"].Value;
            var methodParams = groups["params"].Value;
            switch (method)
            {
                case "crmEntity":
                    return new EntityReference
                    {
                        LogicalName = Evaluate(methodParams.Split(',')[0].Trim(), input, payload, vars).ToString(),
                        Id = Guid.Parse(Evaluate(methodParams.Split(',')[1].Trim(), input, payload, vars).ToString())
                    };
                case "int":
                    return int.Parse(Evaluate(methodParams.Trim(), input, payload, vars).ToString());
                case "guid":
                    return Guid.Parse(Evaluate(methodParams.Trim(), input, payload, vars).ToString());
                case "bool":
                    return bool.Parse(Evaluate(methodParams.Trim(), input, payload, vars).ToString());
                default:
                    throw new Exception($"Method '{method}' not recognized");
            }
        }

        public static IStep StepFactoryCreate(JsonElement step)
        {
            if (step.Get("type").ToString() == "TRANSFORM") return new Transformer(step.Get("jsonSchema").Value);
            if (step.Get("type").ToString() == "CREATE_CRM_ENTITY") return new CreateCrmEntity(step.Get("destEntityName").ToString());
            if (step.Get("type").ToString() == "SET_VARIABLE") return new SetVariable(step.Get("name").ToString(), step.Get("value").ToString());
            if (step.Get("type").ToString() == "UPSERT_CRM_ENTITY") return new UpsertCrmEntity(step.Get("destEntityName").ToString(), step.Get("searchByFields").Value);
            if (step.Get("type").ToString() == "FOR_EACH") return new ForEachStep(step.Get("collection").ToString(), step.Get("steps").Value);

            throw new Exception($"{step.Get("type")} is not a known step");
        }

        public static string ConvertToNestedJson(string dotNotation)
        {
            var dotnotation = JsonSerializer.Deserialize<Dictionary<string, object>>(dotNotation);
            var nestedDict = dotnotation.ParseDotNotation();
            var nestedJson = JsonSerializer.Serialize(nestedDict);
            return nestedJson;
        }
    }

    public static partial class ExtensionMethods
    {
        // https://stackoverflow.com/questions/61553962/getting-nested-properties-with-system-text-json
        public static JsonElement? Get(this JsonElement element, string name) =>
            element.ValueKind != JsonValueKind.Null && element.ValueKind != JsonValueKind.Undefined && element.TryGetProperty(name, out var value)
                ? value : (JsonElement?)null;

        public static string Print(this Dictionary<string, dynamic> obj) => JsonSerializer.Serialize(obj, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        public static object GetJsonElementValue(this JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    return element.GetString();
                case JsonValueKind.Number:
                    return element.GetDecimal();
                default:
                    // fall back
                    return element.ToString();
            }
        }

        // https://stackoverflow.com/questions/61553962/getting-nested-properties-with-system-text-json
        public static JsonElement GetNestedJsonElement(this JsonElement jsonElement, string path)
        {
            if (jsonElement.ValueKind == JsonValueKind.Null ||
                jsonElement.ValueKind == JsonValueKind.Undefined)
            {
                return default;
            }

            string[] segments =
                path.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

            for (int n = 0; n < segments.Length; n++)
            {
                jsonElement = jsonElement.TryGetProperty(segments[n], out JsonElement value) ? value : default;

                if (jsonElement.ValueKind == JsonValueKind.Null ||
                    jsonElement.ValueKind == JsonValueKind.Undefined)
                {
                    return default;
                }
            }

            return jsonElement;
        }

        private static Dictionary<string, object> CheckAndCreateKey(string key, object value, Dictionary<string, object> dict)
        {
            if (!key.Contains("."))
            {
                dict[key] = value;
                return dict;
            }

            var firstLevel = key.Split('.')[0];
            var remainingParts = key.Replace(firstLevel + ".", "");
            var index = -1;
            if (firstLevel.Contains("["))
            {
                var bits = firstLevel.Split('[', ']');
                firstLevel = bits[0];
                index = int.Parse(bits[1]);
            }

            if (!dict.ContainsKey(firstLevel))
            {
                // new property
                var nestedDict = CheckAndCreateKey(remainingParts, value, new Dictionary<string, object>());
                if (index > -1)
                {
                    // this is an Array
                    var list = new List<Dictionary<string, object>>();

                    while (list.Count <= index) // add require length, in case when index are in the wrong order (e.g. cars[1].make appears first before cars[0].make)
                        list.Add(new Dictionary<string, object>());

                    list[index] = nestedDict;
                    dict[firstLevel] = list;
                    return dict;
                }

                dict[firstLevel] = nestedDict;
                return dict;
            }

            if (index > -1)
            {
                var list = (List<Dictionary<string, object>>)dict[firstLevel];
                while (list.Count <= index) // add missing items
                    list.Add(new Dictionary<string, object>());

                var nestedDict = CheckAndCreateKey(remainingParts, value, (Dictionary<string, object>)list[index]);
                dict[firstLevel] = list;
                return dict;
            }

            var current = (Dictionary<string, object>)dict[firstLevel];
            dict[firstLevel] = CheckAndCreateKey(remainingParts, value, current);
            return dict;
        }

        public static Dictionary<string, object> ParseDotNotation(this Dictionary<string, object> input)
        {
            var formattedDictionary = new Dictionary<string, object>();
            foreach (var pair in input)
            {
                CheckAndCreateKey(pair.Key, pair.Value, formattedDictionary);
            }
            return formattedDictionary;
        }
    }
    #endregion
}
