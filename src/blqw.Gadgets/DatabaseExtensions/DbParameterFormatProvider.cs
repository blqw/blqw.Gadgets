using System;
using System.Collections;
using System.Data;
using System.Text;

namespace blqw.Gadgets.DatabaseExtensions
{
    internal class DbParameterFormatProvider : IFormatProvider, ICustomFormatter
    {
        private IDbCommand _cmd;
        private Func<string, string> _placeholder;
        private readonly StringBuilder _buffer = new StringBuilder();

        public void SetCommand(IDbCommand command)
        {
            _cmd = command;
            _placeholder = _cmd.Connection.GetParameterPlaceholderHandler();
        }

        public void ClearCommand()
        {
            _cmd = null;
            _placeholder = null;
            ClearBuffer();
        }
        private void ClearBuffer()
        {
            if (_buffer.Length > 0)
            {
                _buffer.Clear();
            }
        }

        public object GetFormat(Type formatType) => this;

        public string Format(string format, object arg, IFormatProvider formatProvider)
        {
            if (arg is IEnumerable e && !arg.IsAtom())
            {
                var ee = e.GetEnumerator();
                if (ee.MoveNext())
                {
                    var p = AddParameter(ee.Current, format);
                    if (!ee.MoveNext())
                    {
                        return p;
                    }
                    ClearBuffer();
                    _buffer.Append(p);
                    do
                    {
                        _buffer.Append(',');
                        _buffer.Append(AddParameter(ee.Current, format));
                    } while (ee.MoveNext());
                    var sql = _buffer.ToString();
                    ClearBuffer();
                    return sql;
                }
                return AddParameter(null, format);
            }

            return AddParameter(arg, format);
        }


        private string AddParameter(object arg, string format)
        {
            if (arg is IDataParameter parameter)
            {
                _cmd.AddParameter(arg);
                return _placeholder(parameter.ParameterName);
            }
            var p = _cmd.AddParameter(arg);
            if (string.IsNullOrEmpty(p.ParameterName))
            {
                p.ParameterName = "p" + _cmd.Parameters.Count.ToString();
            }
            return _placeholder(p.ParameterName);
        }
    }
}
