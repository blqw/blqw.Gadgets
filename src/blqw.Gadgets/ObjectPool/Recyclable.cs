using System;
using System.Threading;

namespace blqw.Gadgets
{
    /// <summary>
    /// 可回收的对象
    /// </summary>
    internal class Recyclable<T> : IDisposable
        where T : class
    {
        private T _value;
        private readonly Action<T> _returnback;

        public Recyclable(T value, Action<T> returnback)
        {
            _value = value;
            _returnback = returnback ?? throw new ArgumentNullException(nameof(returnback));
        }
        /// <summary>
        /// 回收对象
        /// </summary>
        public void Dispose()
        {
            var value = Interlocked.Exchange(ref _value, null);
            if (value != null)
            {
                _returnback(value);
            }
        }
        // 析构函数
        ~Recyclable() => Dispose();
    }
}
