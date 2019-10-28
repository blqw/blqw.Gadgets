﻿using System;
using System.Collections;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace blqw.Gadgets.DatabaseExtensions
{
    /// <summary>
    /// SQL解析
    /// </summary>
    public static class SQLParser
    {
        // 用于分析占位符正则表达式
        private static readonly Regex _regex = new Regex(@"(?<!\{)\{(?<n>\d+)(?<x>[^}]*)\}", RegexOptions.Compiled);

        public static (string sql, object[] arguments) Parse(FormattableString sql)
        {
            var format = sql?.Format;
            var arguments = sql?.GetArguments() ?? Array.Empty<object>();
            if (string.IsNullOrWhiteSpace(format) || arguments.Length == 0)
            {
                return ("", arguments);
            }
            var length = arguments.Sum(x => x is IEnumerable e && !(x is string) ? e.Cast<object>().Count() : 1);
            if (length == arguments.Length)
            {
                return (format, arguments);
            }
            var index = arguments.Length;
            var buffer = new StringBuilder();
            Array.Resize(ref arguments, length);
            format = _regex.Replace(format, m =>
            {
                var n = int.Parse(m.Groups["n"].Value);
                var v = arguments[n];
                if (v is IEnumerable e && !(v is string))
                {
                    var r = e.GetEnumerator();
                    buffer.Append(m.Value); // {0}
                    if (r.MoveNext())
                    {
                        arguments[n] = r.Current;
                        while (r.MoveNext())
                        {
                            buffer.Append(",{");
                            buffer.Append(index);
                            buffer.Append(m.Groups["x"].Value);
                            buffer.Append('}');
                            arguments[index] = r.Current;
                            index++;
                        }
                    }
                    else
                    {
                        arguments[n] = DBNull.Value;
                    }
                    var b = buffer.ToString();
                    buffer.Clear();
                    return b;

                }
                return m.Value;
            });

            return (format, arguments);
        }
    }
}
