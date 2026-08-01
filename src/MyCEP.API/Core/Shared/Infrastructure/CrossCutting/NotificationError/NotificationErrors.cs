using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace MyCEP.API.Core.Shared.Infrastructure.CrossCutting.NotificationError
{
    [ExcludeFromCodeCoverage]
    public sealed record NotificationErrors
    {
        private NotificationErrors() { }
        private readonly Dictionary<string, List<string>> _errors = new();
        private const string KeyExceptionError = "exception";

        public void AddError(string key, string message)
        {
            if (!_errors.ContainsKey(key))
            {
                _errors.Add(key, [message]);
                return;
            }

            _errors[key].Add(message);
        }

        public Error Error
        {
            get
            {
                var errors = new List<ErrorItem>(_errors.Count);
                foreach (var item in _errors)
                    errors.Add(new ErrorItem(item.Key, item.Value));

                return new Error(errors);
            }
        }

        public bool HaveError() => _errors.Count > 0;

        public static NotificationErrors Empty => new();

        public static NotificationErrors Create(string key, string message)
        {
            var notificationErros = new NotificationErrors();
            notificationErros.AddError(key, message);
            return notificationErros;
        }

        public static NotificationErrors Create(Exception exception)
        {
            int exceptionCounter = 1;
            var notificationErrors = Create(KeyExceptionError + "_" + exceptionCounter.ToString(), exception.Message);
            while (exception.InnerException != null)
            {
                notificationErrors.AddError(KeyExceptionError + "_" + (++exceptionCounter).ToString(), exception.InnerException.Message);
                exception = exception.InnerException;
            }

            return notificationErrors;
        }

        public static NotificationErrors Merge(NotificationErrors first, params NotificationErrors[] arraysOfErrors)
        {
            try
            {
                if (arraysOfErrors is null)
                    return first;

                var notificationErrors = new NotificationErrors(first ?? Empty);
                foreach (var error in arraysOfErrors)
                    foreach (var errorItem in error.Error.Errors)
                        foreach (var propertyErrositem in errorItem.PropertyErrors)
                            notificationErrors.AddError(errorItem.PropertyName, propertyErrositem);

                return notificationErrors;
            }
            catch (Exception ex)
            {
                return Create(ex);
            }
        }
    }

    public sealed record ErrorItem([property: JsonPropertyName("propertyName")] string PropertyName,
                                   [property: JsonPropertyName("propertyErrors")] List<string> PropertyErrors);

    public sealed record Error([property: JsonPropertyName("erros")] List<ErrorItem> Errors);
}
