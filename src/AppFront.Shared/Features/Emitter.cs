using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AppFront.Shared.Features
{
    public class Emitter
    {
        List<KeyValuePair<string, Action<object>>> events = new();

        public void On(string eventName, Action<dynamic> action)
        {
            events.Add(new KeyValuePair<string, Action<object>>(eventName, action));
        }

        public void On(string eventName, Action action)
        {
            events.Add(new KeyValuePair<string, Action<object>>(eventName, s => action()));
        }

        public void On(Type type, EmitTypeMode mode, Action action)
        {
            On(type.FullName + "::" + mode.ToString(), action);
        }

        public void On(Type type, EmitTypeMode mode, Action<dynamic> action)
        {
            On(type.FullName + "::" + mode.ToString(), action);
        }

        public void Emit(string eventName, object? payload = null)
        {

            var _actions = events.Where(s => s.Key == eventName);

            EmitActions(_actions, payload);
        }

        public void Emit(Type type, object? payload = null)
        {
            Emit(type, EmitTypeMode.All, payload);
        }

        public void Emit(Type type, EmitTypeMode mode, object? payload = null)
        {
            IEnumerable<KeyValuePair<string, Action<object>>> _actions;

            if (mode == EmitTypeMode.All)
                _actions = events.Where(s => s.Key.StartsWith(type.FullName + "::"));
            if (mode == EmitTypeMode.Update)//add,update,delete
            {
                List<EmitTypeMode> modes = new() { EmitTypeMode.Add, EmitTypeMode.Update, EmitTypeMode.Delete };
                List<string> variants = modes.Select(s => type.FullName + "::" + s.ToString()).ToList();
                _actions = events.Where(s => variants.Contains(s.Key));
            }
            else
                _actions = events.Where(s => s.Key == type.FullName + "::" + mode.ToString());

            EmitActions(_actions, payload);
        }

        public void RemoveEmit(string eventName, Action<dynamic>? action = null)
        {
            if (action == null)
            {
                events.RemoveAll(s => s.Key == eventName);
            }
            else
            {
                events.RemoveAll(s => s.Key == eventName && s.Value == action);
            }
        }

        public void RemoveEmit(Type type, EmitTypeMode mode, Action<dynamic>? action = null)
        {
            RemoveEmit(type.FullName + "::" + mode.ToString(), action);
        }

        //-------------
        private void EmitActions(IEnumerable<KeyValuePair<string, Action<object>>> actions, object? payload)
        {
            foreach (var action in actions)
            {
                action.Value?.Invoke(payload!);
            }
        }

        //protected static void Test1()
        //{
        //    On("q1", e=>MEthod2(e));
        //    On("q1", MEthod1);
        //}

        //private static void MEthod1()
        //{
        //    Console.WriteLine("1");
        //}

        //private static void MEthod2(string text)
        //{
        //    Console.WriteLine("2");
        //}
    }

    public enum EmitTypeMode
    {
        All = 0,
        Add,
        Update,
        Delete
    }
}
