using blqw.DI;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace blqw.Gadgets
{
    /// <summary>
    /// 对象池
    /// </summary>
    public class ObjectPool<T>
        where T : class
    {
        private readonly T[] _cached;
        private readonly IServiceProvider _serviceProvider;
        private readonly Func<IServiceProvider, T> _creator;
        private readonly Func<T, bool> _returns;

        public ObjectPool(IServiceProvider serviceProvider)
            : this(100)
        {
            _serviceProvider = serviceProvider;
        }

        public ObjectPool(int capacity) => _cached = new T[Math.Max(1, capacity)];

        public ObjectPool(IServiceProvider serviceProvider, Func<IServiceProvider, T> creator, Func<T, bool> returns, int capacity = 100)
            : this(100)
        {
            _serviceProvider = serviceProvider;
            _creator = creator;
            _returns = returns;
        }

        /// <summary>
        /// 对象池容量
        /// </summary>
        public virtual int Capacity => _cached.Length;

        /// <summary>
        /// 从兑换吃中获取对象,如果对象池已空,则动态创建对象
        /// </summary>
        public virtual IDisposable Get(out T value)
        {
            for (var i = 0; i < _cached.Length; i++)
            {
                var item = _cached[i];
                if (item != null && Interlocked.CompareExchange(ref _cached[i], null, item) == item)
                {
                    return new Recyclable<T>(value = item, Return);
                }
            }
            return new Recyclable<T>(value = Create(), Return);
        }

        private readonly static object[] _emptyArgs = new object[] { null };
        protected virtual T Create()
        {
            if (_creator != null)
            {
                return _creator(_serviceProvider);
            }
            if (_serviceProvider != null)
            {
                return _serviceProvider.CreateInstance<T>();
            }
            var ctor = typeof(T).GetTypeInfo()
                                .DeclaredConstructors
                                .ToArray();
            if (ctor.Length == 1)
            {
                var p = ctor[0].GetParameters();
                if (p.Length == 0)
                {
                    return (T)ctor[0].Invoke(null);
                }
                if (p.Length == 1 && typeof(IServiceProvider).IsAssignableFrom(p[0].ParameterType))
                {
                    return (T)ctor[0].Invoke(_emptyArgs);
                }
                throw new NotSupportedException();
            }
            var ret = ctor.FirstOrDefault(x =>
            {
                var p = x.GetParameters();
                return p.Length == 1 && typeof(IServiceProvider).IsAssignableFrom(p[0].ParameterType);
            })?.Invoke(_emptyArgs)
            ?? ctor.FirstOrDefault(x => x.GetParameters().Length == 0)?.Invoke(null);
            return ret as T ?? throw new NotSupportedException();
        }

        protected virtual void Return(T value)
        {
            if (_returns?.Invoke(value) ?? true)
            {
                for (var i = 0; i < _cached.Length; i++)
                {
                    var item = _cached[i];
                    if (item == null && Interlocked.CompareExchange(ref _cached[i], value, null) == null)
                    {
                        return;
                    }
                }
            }
        }

    }
}
