using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace blqw.Gadgets
{
    /// <summary>
    /// 可回收的对象
    /// </summary>
    class Recyclable<T> : IDisposable
        where T : class
    {
        private T _value;
        private readonly Action<T> _returning;

        public Recyclable(T value, Action<T> returning)
        {
            _value = value;
            _returning = returning ?? throw new ArgumentNullException(nameof(returning));
        }
        /// <summary>
        /// 回收对象
        /// </summary>
        public void Dispose()
        {
            var value = Interlocked.Exchange(ref _value, null);
            if (value != null)
            {
                _returning(value);
            }
        }
        // 析构函数
        ~Recyclable() => Dispose();
    }
}
