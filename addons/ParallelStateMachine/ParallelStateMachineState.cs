using Godot;
using System.Collections.Generic;
using ProtoBuf;
using Godot.Collections;

/*
    ParallelStateMachineState(string name, ParallelStateMachine parent, int priority = 10):
        void ConnectState(int rule, Array<int> connectStates);
            public enum ConnectRule
            {
                InterruptionPolicy = 0,
                MutualExclusionPolicy = 1,
                PriorityInterruptionPolicy = 2,
                ActivationDependency = 3,
                CascadingActivationPolicy = 4,
            }
        bool Open();
            virtual bool OnOpen();
        bool Close();
            virtual bool OnClose();
*/

[ProtoContract] [GlobalClass, Icon("res://addons/ParallelStateMachine/icon.png")] public partial class ParallelStateMachineState : Resource
{
    public int id;
    [ProtoMember(1)] public string name;
    public ParallelStateMachine parent;
    [ProtoMember(2)] public int priority;
    [ProtoMember(3)] public readonly List<int> requiredOpen = [];
    [ProtoMember(4)] public readonly List<int> requiredClose = [];
    [ProtoMember(5)] public readonly List<int> openOnOpen = [];
    [ProtoMember(6)] public readonly List<int> closeOnOpen = [];
    [ProtoMember(7)] public readonly List<int> closeOnClose = [];
    public ParallelStateMachineState(string name, ParallelStateMachine parent, int priority = 10)
    {
        this.name = name;
        this.priority = priority;
        this.parent = parent;
        this.id = parent.stateList.Count;
        parent.stateList.Add(this);
        parent.stateFlags.Add(false);
        parent.stateSearcher.Add(name, this);
    }
    public ParallelStateMachineState(){}
    public enum ConnectRule
    {
        InterruptionPolicy = 0,
        MutualExclusionPolicy = 1,
        PriorityInterruptionPolicy = 2,
        ActivationDependency = 3,
        CascadingActivationPolicy = 4,
    }
    public void ConnectState(int rule, Array<int> connectStates)
    {
        foreach (var connectState in connectStates)
        {
            parent.ConnectState(rule, id, connectState);
        }
    }
    public virtual bool OnOpen()
    {
        return true;
    }
    public bool Open()
    {
        foreach (var id in requiredOpen) if (parent.stateFlags[id] == false) return false;
        foreach (var id in requiredClose) if (parent.stateFlags[id] == true) return false;
        foreach (var id in openOnOpen) if (parent.stateList[id].Open() == false) return false;
        foreach (var id in closeOnOpen) if (parent.stateList[id].Close() == false) return false;
        return OnOpen();

    }
    public virtual bool OnClose()
    {
        return true;
    }
    public bool Close()
    {
        foreach (var id in closeOnClose) if (parent.stateList[id].Close() == false) return false;
        return OnClose();
    }

}