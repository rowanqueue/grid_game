using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EventManager
{

    private Dictionary<Type, AGPEvent.Handler> _registeredHandlers = new Dictionary<Type, AGPEvent.Handler>();

    public void Register<T>(AGPEvent.Handler handler) where T : AGPEvent
    {
        var type = typeof(T);
        if (_registeredHandlers.ContainsKey(type))
        {
            if (!IsEventHandlerRegistered(type, handler))
                _registeredHandlers[type] += handler;
        }
        else
        {
            _registeredHandlers.Add(type, handler);
        }
    }

    public void Unregister<T>(AGPEvent.Handler handler) where T : AGPEvent
    {
        var type = typeof(T);
        if (!_registeredHandlers.TryGetValue(type, out var handlers)) return;

        handlers -= handler;

        if (handlers == null)
        {
            _registeredHandlers.Remove(type);
        }
        else
        {
            _registeredHandlers[type] = handlers;
        }
    }

    public void Fire(AGPEvent e)
    {
        var type = e.GetType();

        if (_registeredHandlers.TryGetValue(type, out var handlers))
        {
            handlers(e);
        }
    }

    public bool IsEventHandlerRegistered(Type typeIn, Delegate prospectiveHandler)
    {
        return _registeredHandlers[typeIn].GetInvocationList().Any(existingHandler => existingHandler == prospectiveHandler);
    }
}

public abstract class AGPEvent
{
    public readonly float creationTime;

    public AGPEvent()
    {
        creationTime = Time.time;
    }

    public delegate void Handler(AGPEvent e);
}
public class Tick : AGPEvent
{
    public Tick()
    {

    }
}

//register
//Services.EventManager.Register<PlayerActionEvent>(OnPlayerActionEvent);

//event
//public void OnPlayerActionEvent(AGPEvent e)

//fire
//Services.EventManager.Fire(new PlayerActionEvent(StepActionType.Action, "actionZone"));