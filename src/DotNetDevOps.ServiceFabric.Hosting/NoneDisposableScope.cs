using Autofac;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DotNetDevOps.ServiceFabric.Hosting
{
    public class NoneDisposableScope : IDisposable
    {
        private static FieldInfo stackField;

        private ILifetimeScope lifetimeScope;

        public NoneDisposableScope(ILifetimeScope lifetimeScope)
        {
            this.lifetimeScope = lifetimeScope;

            if (stackField == null)
                stackField = lifetimeScope.Disposer.GetType().GetField("_items", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);


        }

        private Stack<IDisposable> stack => stackField
               .GetValue(lifetimeScope.Disposer) as Stack<IDisposable> ?? throw new Exception("autofact changed, please update");

        public void Dispose()
        {


            var newStack = new Stack<IDisposable>();
            foreach (var item in stack.Reverse())
            {
                if (!_doNotDisposes.Contains(item))
                {
                    newStack.Push(item);
                }
            }
            stackField.SetValue(lifetimeScope.Disposer, newStack);



            lifetimeScope.Dispose();

        }

        public T Resolve<T>()
        {
            return Capture(lifetimeScope.Resolve<T>());
        }
        public object Resolve(Type type)
        {
            return Capture(lifetimeScope.Resolve(type));
        }

        private HashSet<object> _doNotDisposes = new HashSet<object>();

        private T Capture<T>(T t)
        {
            if (t is IDisposable)
            {
                _doNotDisposes.Add(t);
            }
            return t;
        }
    }


}
