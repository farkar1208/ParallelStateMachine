using Godot;
using System;
using System.Collections.Generic;
using ProtoBuf;
using Godot.Collections;
using System.Text.Json.Serialization;
using System.Reflection;
using System.Text.Json;
using System.Linq;

/*
    ParallelStateMachine is a state machine that can handle multiple states at once. 
    ParallelStateMachine:
        ParallelStateMachineState AddState(string name, int priority = 10);
        void ConnectState(int rule, int idU, int idV);
        void SelfPrint();
*/

[ProtoContract] [GlobalClass, Icon("res://addons/ParallelStateMachine/icon.png")] public partial class ParallelStateMachine : Resource
{
    public readonly System.Collections.Generic.Dictionary<string, ParallelStateMachineState> stateSearcher = [];
    [ProtoMember(1)] public readonly List<ParallelStateMachineState> stateList = [];
    [ProtoMember(2)] public readonly List<bool> stateFlags = [];
    public ParallelStateMachineState AddState(string name, int priority = 10)
    {
        return new ParallelStateMachineState(name, this, priority);
    }
    public ParallelStateMachineState this[string name] { get => stateSearcher[name]; }
    public ParallelStateMachineState FindState(string name)
    {
        return stateSearcher[name];
    }
    public void ConnectState(int rule, int idU, int idV)
    {
        if(stateList.Count <= idU || stateList.Count <= idV) throw new ArgumentOutOfRangeException(
            $"State ID out of range. Valid range is 0-{stateList.Count - 1}, but got {idU} and {idV}");
        if(idU == idV) throw new ArgumentException(
            $"Cannot connect a state to itself. Both IDs are {idU}");
        ParallelStateMachineState stateU = stateList[idU], stateV = stateList[idV];
        switch ((ParallelStateMachineState.ConnectRule)rule)
        {
            case ParallelStateMachineState.ConnectRule.InterruptionPolicy:
                stateU.closeOnOpen.Add(idV);
                stateV.closeOnOpen.Add(idU);
                break;
            case ParallelStateMachineState.ConnectRule.MutualExclusionPolicy:
                stateU.requiredClose.Add(idV);
                stateV.requiredClose.Add(idU);
                break;
            case ParallelStateMachineState.ConnectRule.PriorityInterruptionPolicy:
                if(stateU.priority > stateV.priority)
                {
                    stateU.closeOnOpen.Add(idV);
                    stateV.requiredClose.Add(idU);
                }
                else
                {
                    stateU.requiredClose.Add(idV);
                    stateV.closeOnOpen.Add(idU);
                }
                break;
            case ParallelStateMachineState.ConnectRule.ActivationDependency:
                stateU.requiredOpen.Add(idV);
                stateV.closeOnClose.Add(idU);
                break;
            case ParallelStateMachineState.ConnectRule.CascadingActivationPolicy:
                stateU.openOnOpen.Add(idV);
                stateV.closeOnClose.Add(idU);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(rule), rule, null);
        }
    }

    [ProtoAfterDeserialization] void AfterDeserialization()
    {
        for(int i = 0; i < stateList.Count; i++)
        {
            var state = stateList[i];
            state.id = i;
            state.parent = this;
            stateSearcher[state.name] = state;
        }
    }

    public override Variant _Get(StringName property)
    {
        if (property == "binary_value")
        {
            // ProtoBuf serialization
            using (var stream = new System.IO.MemoryStream())
            {
                Serializer.Serialize(stream, this);
                return stream.ToArray();
            }
        }
        return base._Get(property);
    }
    public override bool _Set(StringName property, Variant value)
    {
        if (property == "binary_value")
        {
            // ProtoBuf deserialization
            using (var stream = new System.IO.MemoryStream((byte[])value))
            {
                var deserialization = Serializer.Deserialize<ParallelStateMachine>(stream);
                stateList.Clear();
                stateFlags.Clear();
                stateSearcher.Clear();

                stateList.AddRange(deserialization.stateList);
                stateFlags.AddRange(deserialization.stateFlags);
                foreach (var kvp in deserialization.stateSearcher)
                {
                    stateSearcher.Add(kvp.Key, kvp.Value);
                }
                return true;
            }
        }
        return base._Set(property, value);
    }
    public override Array<Dictionary> _GetPropertyList()
    {
        var list = new Array<Dictionary>();
        var item = new Dictionary
        {
            ["name"] = "binary_value",
            ["type"] = (int)Variant.Type.PackedByteArray
        };
        list.Add(item);
        return list;
    }

    public void SelfPrint()
    {
        string json = JsonSerializer.Serialize(this, OptionsList.options);
        GD.Print(json);
    }
}

public class OptionsList
{
    public static readonly JsonSerializerOptions options = new()
    {
        WriteIndented = true,
        IncludeFields = true,
        Converters = { new ProtoMemberConverter<ParallelStateMachine>(),
                       new ProtoMemberConverter<ParallelStateMachineState>() }
    };
}

public class ProtoMemberConverter<T> : JsonConverter<T> where T : class
{
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // 反序列化逻辑（按需实现）
        throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        // 反射获取所有 ProtoMember 标记的属性
        var fields = typeof(T).GetFields()
            .Where(p => p.GetCustomAttribute<ProtoMemberAttribute>() != null);
        var properties = typeof(T).GetProperties()
            .Where(p => p.GetCustomAttribute<ProtoMemberAttribute>() != null);
        foreach (var field in fields)
        {
            writer.WritePropertyName(field.Name);
            JsonSerializer.Serialize(writer, field.GetValue(value), options);
        }
        foreach (var prop in properties)
        {
            writer.WritePropertyName(prop.Name);
            JsonSerializer.Serialize(writer, prop.GetValue(value), options);
        }

        writer.WriteEndObject();
    }
}
