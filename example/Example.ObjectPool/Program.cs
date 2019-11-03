using blqw.Gadgets;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace Example.ObjectPool
{
    class Program
    {
        static void Main(string[] args)
        {
            var provider = new ServiceCollection()
                                .AddObjectPool()
                                .AddObjectPool(
                                    x => new List<string>(),
                                    x =>
                                    {
                                        x.Clear();
                                        return true;
                                    }
                                )
                                .BuildServiceProvider();

            var pool = provider.GetPool<object>();
            using (pool.Get(out var obj))
            {
                Console.WriteLine(obj);
            }


            var pool2 = provider.GetPool<StringBuilder>();

            using (pool2.Get(out var sb))
            {
                sb.Append("111");
                Console.WriteLine(sb.ToString());
            }
            using (pool2.Get(out var sb))
            {
                sb.Append("222");
                Console.WriteLine(sb.ToString());
            }

            var pool3 = provider.GetPool<List<string>>();
            using (pool3.Get(out var list))
            {
                Console.WriteLine(list);
            }
        }
    }
}
