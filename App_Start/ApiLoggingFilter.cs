using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.ExceptionHandling;
using System.Web.Http.Filters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using NLog;

namespace MISReports_Api
{
    /// <summary>
    /// Logs every Web API call: one line when the request arrives (with its
    /// parameters) and one line when it finishes (status code + duration).
    /// The logger is named after the controller, so log lines show which API ran.
    /// Registered once in WebApiConfig, so it covers all current and future APIs.
    /// </summary>
    public class ApiRequestLoggingFilter : ActionFilterAttribute
    {
        private const string StopwatchKey = "__ApiRequestLogging_Stopwatch";
        private const int MaxArgumentsLength = 2000;
        private const int MaxErrorBodyLength = 1000;

        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            try
            {
                actionContext.Request.Properties[StopwatchKey] = Stopwatch.StartNew();

                var logger = GetLogger(actionContext);
                if (!logger.IsInfoEnabled)
                    return;

                logger.Info("Request started: {method} {url} -> {action}. Parameters: {parameters}",
                    actionContext.Request.Method.Method,
                    actionContext.Request.RequestUri?.PathAndQuery,
                    actionContext.ActionDescriptor.ActionName,
                    LogValueFormatter.FormatArguments(actionContext.ActionArguments, MaxArgumentsLength));
            }
            catch (Exception ex)
            {
                // Logging must never break an API call.
                Trace.WriteLine($"ApiRequestLoggingFilter.OnActionExecuting failed: {ex.Message}");
            }
        }

        public override void OnActionExecuted(HttpActionExecutedContext actionExecutedContext)
        {
            try
            {
                var actionContext = actionExecutedContext.ActionContext;
                var logger = GetLogger(actionContext);
                long elapsedMs = -1;
                if (actionContext.Request.Properties.TryGetValue(StopwatchKey, out object swObj) && swObj is Stopwatch sw)
                {
                    sw.Stop();
                    elapsedMs = sw.ElapsedMilliseconds;
                }

                string action = actionContext.ActionDescriptor.ActionName;
                string method = actionContext.Request.Method.Method;
                string url = actionContext.Request.RequestUri?.PathAndQuery;

                if (actionExecutedContext.Exception != null)
                {
                    // Full exception details are written by NLogExceptionLogger.
                    logger.Error("Request failed with unhandled exception: {method} {url} -> {action} after {elapsedMs} ms. {error}",
                        method, url, action, elapsedMs, actionExecutedContext.Exception.Message);
                    return;
                }

                var response = actionExecutedContext.Response;
                int statusCode = response == null ? 0 : (int)response.StatusCode;

                if (statusCode >= 500)
                {
                    logger.Error("Request completed with server error: {method} {url} -> {action} returned {statusCode} in {elapsedMs} ms. Response: {response}",
                        method, url, action, statusCode, elapsedMs, ReadErrorBody(response));
                }
                else if (statusCode >= 400)
                {
                    logger.Warn("Request rejected: {method} {url} -> {action} returned {statusCode} in {elapsedMs} ms. Response: {response}",
                        method, url, action, statusCode, elapsedMs, ReadErrorBody(response));
                }
                else
                {
                    logger.Info("Request completed: {method} {url} -> {action} returned {statusCode} in {elapsedMs} ms",
                        method, url, action, statusCode, elapsedMs);
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"ApiRequestLoggingFilter.OnActionExecuted failed: {ex.Message}");
            }
        }

        private static Logger GetLogger(HttpActionContext actionContext)
        {
            var controllerType = actionContext?.ControllerContext?.ControllerDescriptor?.ControllerType;
            return controllerType != null
                ? LogManager.GetLogger(controllerType.FullName)
                : LogManager.GetLogger(typeof(ApiRequestLoggingFilter).FullName);
        }

        /// <summary>
        /// Reads the error message of a 4xx/5xx response without consuming a stream.
        /// Only in-memory (ObjectContent / StringContent) bodies are read.
        /// </summary>
        private static string ReadErrorBody(HttpResponseMessage response)
        {
            if (response?.Content == null)
                return null;

            try
            {
                if (response.Content is ObjectContent objectContent)
                {
                    var value = objectContent.Value;
                    if (value is HttpError httpError)
                    {
                        string message = httpError.Message;
                        if (httpError.ExceptionMessage != null)
                            message += " | " + httpError.ExceptionMessage;
                        return LogValueFormatter.Truncate(message, MaxErrorBodyLength);
                    }
                    return LogValueFormatter.FormatValue(value, MaxErrorBodyLength);
                }

                if (response.Content is StringContent)
                {
                    return LogValueFormatter.Truncate(response.Content.ReadAsStringAsync().GetAwaiter().GetResult(), MaxErrorBodyLength);
                }
            }
            catch (Exception ex)
            {
                return "<unreadable response: " + ex.Message + ">";
            }

            return "<" + response.Content.GetType().Name + ">";
        }
    }

    /// <summary>
    /// Logs any exception that escapes a Web API controller, filter or
    /// serializer (i.e. exceptions not caught inside the action itself).
    /// </summary>
    public class NLogExceptionLogger : ExceptionLogger
    {
        public override void Log(ExceptionLoggerContext context)
        {
            try
            {
                var controllerType = context.ExceptionContext?.ActionContext?.ControllerContext?.ControllerDescriptor?.ControllerType;
                var logger = controllerType != null
                    ? LogManager.GetLogger(controllerType.FullName)
                    : LogManager.GetLogger(typeof(NLogExceptionLogger).FullName);

                logger.Error(context.Exception, "Unhandled exception in API request: {method} {url}",
                    context.Request?.Method?.Method,
                    context.Request?.RequestUri?.PathAndQuery);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"NLogExceptionLogger failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Converts request parameters to short, safe text for the log file.
    /// Values of properties/parameters that look like credentials are masked.
    /// </summary>
    internal static class LogValueFormatter
    {
        private static readonly string[] SensitiveNameParts =
        {
            "password", "passwd", "pwd", "secret", "token", "apikey", "api_key",
            "connectionstring", "connstring", "credential", "authorization"
        };

        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            MaxDepth = 5,
            ContractResolver = new MaskingContractResolver(),
            Error = (sender, args) => { args.ErrorContext.Handled = true; }
        };

        public static bool IsSensitiveName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;
            string lower = name.ToLowerInvariant();
            return SensitiveNameParts.Any(lower.Contains);
        }

        public static string FormatArguments(System.Collections.Generic.IDictionary<string, object> arguments, int maxLength)
        {
            if (arguments == null || arguments.Count == 0)
                return "(none)";

            var parts = arguments.Select(kv => kv.Key + "=" +
                (IsSensitiveName(kv.Key) ? "***" : FormatValue(kv.Value, maxLength)));
            return Truncate(string.Join(", ", parts), maxLength);
        }

        public static string FormatValue(object value, int maxLength)
        {
            if (value == null)
                return "null";

            try
            {
                switch (value)
                {
                    case string s:
                        return Truncate("\"" + s + "\"", maxLength);
                    case DateTime dt:
                        return dt.ToString("yyyy-MM-dd HH:mm:ss");
                    case Stream _:
                    case byte[] _:
                    case HttpContent _:
                        return "<" + value.GetType().Name + ">";
                }

                var type = value.GetType();
                if (type.IsPrimitive || type.IsEnum || value is decimal || value is Guid || value is TimeSpan || value is DateTimeOffset)
                    return Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture);

                if (value is JToken token)
                    return Truncate(MaskToken(token.DeepClone()).ToString(Formatting.None), maxLength);

                return Truncate(JsonConvert.SerializeObject(value, Formatting.None, SerializerSettings), maxLength);
            }
            catch (Exception ex)
            {
                return "<" + value.GetType().Name + ": not serializable (" + ex.Message + ")>";
            }
        }

        public static string Truncate(string text, int maxLength)
        {
            if (text == null || text.Length <= maxLength)
                return text;
            return text.Substring(0, maxLength) + "...(truncated, " + text.Length + " chars)";
        }

        private static JToken MaskToken(JToken token)
        {
            if (token is JObject obj)
            {
                foreach (var prop in obj.Properties().ToList())
                {
                    if (IsSensitiveName(prop.Name))
                        prop.Value = "***";
                    else
                        MaskToken(prop.Value);
                }
            }
            else if (token is JArray arr)
            {
                foreach (var item in arr)
                    MaskToken(item);
            }
            return token;
        }

        private class MaskingContractResolver : DefaultContractResolver
        {
            protected override JsonProperty CreateProperty(System.Reflection.MemberInfo member, MemberSerialization memberSerialization)
            {
                var property = base.CreateProperty(member, memberSerialization);
                if (IsSensitiveName(property.PropertyName))
                {
                    property.ValueProvider = new MaskedValueProvider();
                    property.PropertyType = typeof(string);
                }
                else if (typeof(Stream).IsAssignableFrom(property.PropertyType) || property.PropertyType == typeof(byte[]))
                {
                    property.Ignored = true;
                }
                return property;
            }
        }

        private class MaskedValueProvider : IValueProvider
        {
            public object GetValue(object target) => "***";
            public void SetValue(object target, object value) { }
        }
    }
}
