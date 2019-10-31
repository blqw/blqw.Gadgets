using System;
using System.Data;

namespace blqw.Gadgets.DatabaseExtensions
{
    /// <summary>
    /// 自动关闭的 <see cref="IDbCommand"/> 结构
    /// </summary>
    public struct SelfClosingDbCommand : IDisposable
    {
        private IDbConnection _connection;
        private IDbCommand _command;

        public SelfClosingDbCommand(IDbConnection connection)
        {
            _connection = connection.OpenIfClosed() ? connection : null;
            _command = null;
        }

        public SelfClosingDbCommand(IDbCommand command)
            : this(command?.Connection) =>
            _command = command;

        public void Dispose()
        {
            _command?.Dispose();
            _command = null;
            _connection?.SafeClose();
            _connection = null;
        }
    }
}
