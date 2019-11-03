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
        private readonly Func<T, bool> _returnback;

        /// <summary>
        /// 创建一个对象池,默认大小为100
        /// </summary>
        /// <param name="serviceProvider"></param>
        public ObjectPool(IServiceProvider serviceProvider)
            : this(100) => _serviceProvider = serviceProvider;

        /// <summary>
        /// 创建一个具有指定大小的对象池
        /// </summary>
        /// <param name="capacity"></param>
        public ObjectPool(int capacity) => _cached = new T[Math.Max(1, capacity)];

        /// <summary>
        /// 根据参数创建一个对象池
        /// </summary>
        /// <param name="serviceProvider">服务提供程序</param>
        /// <param name="creator">创建对象的方法</param>
        /// <param name="returnback">返回对象到对象池的方法</param>
        /// <param name="capacity">对象池的固定大小</param>
        public ObjectPool(IServiceProvider serviceProvider, Func<IServiceProvider, T> creator, Func<T, bool> returnback, int capacity = 100)
            : this(100)
        {
            _serviceProvider = serviceProvider;
            _creator = creator;
            _returnback = returnback;
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
                    return new Recyclable<T>(value = item, ReturnBack);
                }
            }
            return new Recyclable<T>(value = Create(), ReturnBack);
        }

        private static readonly object[] _emptyArgs = new object[] { null };
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
            })?.Invoke(_emptyArgs) ?? ctor.FirstOrDefault(x => x.GetParameters().Length == 0)?.Invoke(null);

            return ret as T ?? throw new NotSupportedException();
        }

        protected virtual void ReturnBack(T value)
        {
            if (_returnback?.Invoke(value) ?? true)
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
