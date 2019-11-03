using Microsoft.Extensions.DependencyInjection;
using System;
using System.Text;

namespace blqw.Gadgets
{
    public static class ObjectPoolExtensions
    {
        public static IServiceCollection AddObjectPool(this IServiceCollection services)
        {
            services.AddTransient(CreateStringBuilderPool);
            services.AddTransient(typeof(ObjectPool<>), typeof(ObjectPool<>));
            return services;

            ObjectPool<StringBuilder> CreateStringBuilderPool(IServiceProvider provider)
            {
                return new ObjectPool<StringBuilder>(provider,
                    x => new StringBuilder(),
                    x =>
                    {
                        if (x.Capacity >= 4096)
                        {
                            x.Clear();
                            return false;
                        }
                        x.Clear();
                        return true;
                    });
            }
        }

        public static IServiceCollection AddObjectPool<T>(this IServiceCollection services, Func<IServiceProvider, T> creator, Func<T, bool> returns, int capacity = 100)
            where T : class
        {
            services.AddTransient(p => new ObjectPool<T>(p, creator, returns, capacity));
            return services;
        }

        public static ObjectPool<T> GetPool<T>(this IServiceProvider provider)
            where T : class
            => provider.GetService<ObjectPool<T>>();
    }
}
