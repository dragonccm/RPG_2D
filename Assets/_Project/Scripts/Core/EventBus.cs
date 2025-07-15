using System;
using System.Collections.Generic;

namespace Core
{
    public static class EventBus
    {
        private static Dictionary<Type, Action<object>> _subscribers;

        public static void Initialize()
        {
            _subscribers = new Dictionary<Type, Action<object>>();
        }

        public static void Subscribe<T>(Action<T> callback)
        {
            var type = typeof(T);
            if (!_subscribers.ContainsKey(type))
                _subscribers[type] = e => { };
            _subscribers[type] += (e) => callback((T)e);
        }

        public static void Publish<T>(T e)
        {
            var type = typeof(T);
            if (_subscribers.TryGetValue(type, out var handlers))
                handlers.Invoke(e);
        }
    }
}
