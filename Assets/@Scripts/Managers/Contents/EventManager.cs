using System;
using System.Collections.Generic;
using static Define;
using UnityEngine;

// Managers 가 new 로 생성하는 순수 pub/sub 이다.
// MonoBehaviour 를 상속하면 new 가 금지되어(Unity 규칙) 인스턴스가 null 이 된다.
public class EventManager
{
    // Dictionary to store events
    private Dictionary<GameEvent, Action> eventDictionary = new Dictionary<GameEvent, Action>();

    // Subscribe to an event
    public void Subscribe(GameEvent eventKey, Action listener)
    {
        if (eventDictionary.TryGetValue(eventKey, out var thisEvent))
        {
            thisEvent += listener;
            eventDictionary[eventKey] = thisEvent;
        }
        else
        {
            thisEvent += listener;
            eventDictionary.Add(eventKey, thisEvent);
        }
    }

    // Unsubscribe from an event
    public void Unsubscribe(GameEvent eventKey, Action listener)
    {
        if (eventDictionary.TryGetValue(eventKey, out var thisEvent))
        {
            thisEvent -= listener;
            eventDictionary[eventKey] = thisEvent;
        }
    }

    public void DeleteEvent(GameEvent eventKey)
    {
        if (eventDictionary.TryGetValue(eventKey, out var thisEvent))
        {
            eventDictionary.Remove(eventKey);
        }
    }

    // Trigger an event
    public void TriggerEvent(GameEvent eventKey)
    {
        if (eventDictionary.TryGetValue(eventKey, out var thisEvent))
        {
            thisEvent?.Invoke();
        }
    }
}
