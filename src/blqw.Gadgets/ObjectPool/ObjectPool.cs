using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Linq.Expressions;
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
        private static readonly object[] _emptyArgs = new object[] { null };

        private readonly T[] _cached;
        private readonly Func<T> _creator;
        private readonly Func<T, bool> _returnback;


        /// <summary>
        /// 创建一个对象池,默认大小为100
        /// </summary>
        /// <param name="serviceProvider"></param>
        public ObjectPool(IServiceProvider serviceProvider)
            : this(serviceProvider, null, null, 100)
        {
        }

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
        public ObjectPool(IServiceProvider serviceProvider, Func<T> creator, Func<T, bool> returnback, int capacity = 100)
            : this(capacity)
        {
            _creator = creator ?? BuildCreator(serviceProvider);
            _returnback = returnback;
        }

        private Func<T> BuildCreator(IServiceProvider serviceProvider)
        {
            if (serviceProvider != null)
            {
                return () => ActivatorUtilities.GetServiceOrCreateInstance<T>(serviceProvider);
            }
            FindConstructor(typeof(T), out var best, out var alternative);

            if (best != null)
            {
                var ctor = Expression.New(best, Expression.Constant(serviceProvider));
                return (Func<T>)Expression.Lambda(ctor).Compile();
            }
            if (alternative != null)
            {
                return Activator.CreateInstance<T>;
            }
            throw new NotSupportedException("没有找到合适的构造函数");
        }

        private void FindConstructor(Type type, out ConstructorInfo best, out ConstructorInfo alternative)
        {
            best = null;
            alternative = null;
            var ctors = typeof(T).GetTypeInfo().DeclaredConstructors.ToArray();
            foreach (var ctor in ctors)
            {
                var p = ctor.GetParameters();
                if (p.Length == 0)
                {
                    alternative = ctor;
                }
                else if (p.Length == 1 && typeof(IServiceProvider).IsAssignableFrom(p[0].ParameterType))
                {
                    best = ctor;
                    return;
                }
            }
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
            return new Recyclable<T>(value = _creator(), ReturnBack);
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
