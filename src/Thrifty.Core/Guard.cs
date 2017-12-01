using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Thrifty
{
    public static class Guard
    {
        private static bool IsNullOrWhiteSpace(this String a)
        {
            return String.IsNullOrWhiteSpace(a);
        }

        /// <summary>
        /// 判断（路径）参数中是否包含非法字符。
        /// </summary>
        /// <param name="argument"></param>
        /// <param name="argumentName"></param>
        [System.Diagnostics.DebuggerHidden]
        public static void ArgumentContainsInvalidPathChars(string argument, string argumentName)
        {
            if (argument.IsNullOrWhiteSpace())
            {
                return;
            }
            var invliadChars = Path.GetInvalidPathChars();

            if (argument.Any(c => invliadChars.Contains(c)))
            {
                throw new ArgumentException($"@The provided String argument {argumentName} contains invalid path character.");
            }
        }

        /// <summary>
        /// 当条件不满足时抛出异常。
        /// </summary>
        /// <param name="condition">要测试的条件。</param>
        /// <param name="paramName">参数名称。</param>
        /// <param name="message">异常消息。</param>
        [System.Diagnostics.DebuggerHidden]
        public static void ArgumentCondition(bool condition, string message, string paramName = null)
        {
            if (!condition)
            {
                var ex = paramName.IsNullOrWhiteSpace() ? new ArgumentException(message) : new ArgumentException(message, paramName);
                throw ex;
            }
        }
        [System.Diagnostics.DebuggerHidden]
        public static void ArgumentIsUri(string argument, string argumentName, UriKind kind = UriKind.RelativeOrAbsolute)
        {
            if (!argument.IsNullOrWhiteSpace())
            {
                if (Uri.IsWellFormedUriString(Uri.EscapeUriString(argument), kind))
                {
                    return;
                }
            }
            throw new ArgumentException(String.Format(@"The provided string argument {0} must  be uri.", argumentName), argumentName);
        }
        [System.Diagnostics.DebuggerHidden]
        public static void AbsolutePhysicalPath(string argument, string argumentName)
        {
            if (argument.IsNullOrWhiteSpace())
            {
                throw new ArgumentException(String.Format(@"The provided string argument {0} must  be absolute physical path.", argumentName), argumentName);
            }
            if (!Path.IsPathRooted(argument))
            {
                throw new ArgumentException(String.Format(@"The provided string argument {0} must  be absolute physical path.", argumentName), argumentName);
            }
        }

        /// <summary>
        /// 当参数不是相对路径（包括文件系统路径和 Uri）是抛出异常。
        /// </summary>
        /// <param name="argument">参数。</param>
        /// <param name="argumentName">参数名。</param>
        [System.Diagnostics.DebuggerHidden]
        public static void ArgumentIsRelativePath(string argument, string argumentName)
        {
            if (argument.IsNullOrWhiteSpace())
            {
                throw new ArgumentException(String.Format(@"The provided string argument {0} must  be relative path.", argumentName), argumentName);
            }
            Guard.ArgumentContainsInvalidPathChars(argument, argumentName);
            var virtualPath = argumentName.Replace(@"\", @"/");
            if (Uri.IsWellFormedUriString(Uri.EscapeUriString(virtualPath), UriKind.Absolute))
            {
                throw new ArgumentException(String.Format(@"The provided string argument {0} must  be relative path.", argumentName), argumentName);
            }
            var path = argument.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            if (Path.IsPathRooted(path))
            {
                throw new ArgumentException(String.Format(@"The provided string argument {0} must  be relative path.", argumentName), argumentName);
            }
        }
        [System.Diagnostics.DebuggerHidden]
        public static void ArgumentNullOrWhiteSpaceString(string argumentValue, string argumentName)
        {
            Guard.ArgumentNotNullOrEmptyString(argumentValue, argumentName, true);
        }
        [System.Diagnostics.DebuggerHidden]
        public static void ArgumentNotNullOrEmptyString(string argumentValue, string argumentName)
        {
            Guard.ArgumentNotNullOrEmptyString(argumentValue, argumentName, false);
        }

        private static void ArgumentNotNullOrEmptyString(string argumentValue, string argumentName, bool trimString)
        {
            if ((trimString && argumentValue.IsNullOrWhiteSpace()) || (!trimString && String.IsNullOrEmpty(argumentValue)))
            {
                throw new ArgumentException(String.Format(@"The provided String argument {0} must not be empty.", argumentName));
            }
        }
        [System.Diagnostics.DebuggerHidden]
        public static void ArgumentNotNullOrEmptyArray<T>(IEnumerable<T> argumentValue, string argumentName)
        {
            if (argumentValue == null || !argumentValue.Any())
            {
                throw new ArgumentException(String.Format(@"The provided array argument {0} must not be null or empty array.", argumentName));
            }
        }

        /// <summary>
        /// Checks an argument to ensure it isn't null
        /// </summary>
        /// <param name="argumentValue">The argument value to check.</param>
        /// <param name="argumentName">The name of the argument.</param>
        [System.Diagnostics.DebuggerHidden]
        public static void ArgumentNotNull(object argumentValue, string argumentName)
        {
            if (argumentValue == null)
                throw new ArgumentNullException(argumentName);
        }

    }
}
