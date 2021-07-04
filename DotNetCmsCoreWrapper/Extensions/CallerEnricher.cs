using Serilog.Core;
using Serilog.Events;
using System;
using System.Diagnostics;
using System.Linq;

namespace VSec.DotNet.CmsCore.Wrapper.Serilog.Extension
{
    using System.Reflection;

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Serilog.Core.ILogEventEnricher" />
    public class CallerEnricher : ILogEventEnricher
    {
        /// <summary>
        /// Enrich the log event.
        /// </summary>
        /// <param name="logEvent">The log event to enrich.</param>
        /// <param name="propertyFactory">Factory for creating new properties to add to the event.</param>
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            var skip = 5;
            bool now = false;
            //while (true)
            {
                var stack = new StackTrace();
                var frames = stack.GetFrames();
                var methods = frames.Select(x => { return x.GetMethod(); }).ToArray();
                MethodBase method = null;
               // var rLoggerFound = methods.Where(x => x.DeclaringType != null && x.DeclaringType.Name == "RLogger").ToArray();
                var seriLoggerFound = methods.Where(x => x.DeclaringType != null && x.DeclaringType.Name == "Logger" && x.Name.Equals("Write")).ToArray();
                var sysLoggerFound = methods.Where(x => x.DeclaringType != null && x.DeclaringType.Name == "LoggerExtensions").ToArray();
                //if (rLoggerFound.Any())
                //{
                //    var index = Array.IndexOf(methods, rLoggerFound.First());
                //    var frame = (StackFrame)frames[index + 1];
                //    method = frame.GetMethod();
                //}
                //else
                if (sysLoggerFound.Any())
                {
                    var index = Array.IndexOf(methods, sysLoggerFound.Last());
                    var frame = frames[index + 4];
                    method = frame.GetMethod();
                }
                else
                {
                    var index = Array.IndexOf(methods, seriLoggerFound.Last());
                    var frame = frames[index + 2];
                    method = frame.GetMethod();
                }

                //if (method.DeclaringType != null && method.DeclaringType.Assembly != typeof(RLogger).Assembly)
                if (method != null)
                {
                    var assembly = $"{method.DeclaringType.Assembly.FullName}";
                    var className = $"{method.DeclaringType.Name}";
                    var methodName = $"{method.Name}";
                    logEvent.AddPropertyIfAbsent(new LogEventProperty("Assembly", new ScalarValue(assembly)));
                    logEvent.AddPropertyIfAbsent(new LogEventProperty("Class", new ScalarValue(className)));
                    logEvent.AddPropertyIfAbsent(new LogEventProperty("Method", new ScalarValue(methodName)));
                    //break; 
                }

                //skip++;
            }
        }
    }
}
