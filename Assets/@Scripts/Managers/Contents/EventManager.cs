using System;
using System.Collections.Generic;
using static Define;
using UnityEngine;

public class EventManager : MonoBehaviour
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
