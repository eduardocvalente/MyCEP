using System.Diagnostics.CodeAnalysis;
using MyCEP.API.Core.Shared.Infrastructure.CrossCutting.NotificationError;

namespace MyCEP.API.Core.Shared.Infrastructure.CrossCutting.Result
{
    public enum ErrorType
    {
        ValidationError = 1,
        DatabaseError = 2,
        BusinessError = 3,
        InternalError = 4,
        UndefinedError = 5,
        ExceptionError = 6,
    }

    public interface IAnalysisResult
    {
    }

    public interface IAnalysisResult<T> : IAnalysisResult
    {
    }

    public interface ISuccessResult : IAnalysisResult
    {
    }

    public interface ISuccessResult<T> : ISuccessResult, IAnalysisResult<T>
    {
        T Data { get; init; }
    }

    public interface IFailResult : IAnalysisResult
    {
    }

    public interface IFailResult<T> : IFailResult, IAnalysisResult<T>
    {
        NotificationErrors NotificationErrors { get; init; }
        ErrorType ErrorType { get; init; }
    }

    [ExcludeFromCodeCoverage]
    internal class SuccessResult<T>(T result) : ISuccessResult<T>
    {
        public T Data { get; init; } = result;
    }

    [ExcludeFromCodeCoverage]
    internal class FailResult<T> : IFailResult<T>
    {
        public FailResult(NotificationErrors notificationErrors, ErrorType errorType)
        {
            NotificationErrors = notificationErrors;
            ErrorType = errorType;
        }

        public NotificationErrors NotificationErrors { get; init; }
        public ErrorType ErrorType { get; init; }
    }

    [ExcludeFromCodeCoverage]
    public static class ResultFactory
    {
        public static ISuccessResult<T> CreateSuccess<T>(T data)
        {
            return new SuccessResult<T>(data);
        }

        public static ISuccessResult<T> CreateSuccess<T>()
        {
            return new SuccessResult<T>(default!);
        }

        public static IFailResult<T> CreateFail<T>(Exception exception)
        {
            return new FailResult<T>(NotificationErrors.Create(exception), ErrorType.ExceptionError);
        }

        public static IFailResult<T> CreateFail<T>(NotificationErrors notificationErrors)
        {
            return new FailResult<T>(notificationErrors, ErrorType.UndefinedError);
        }

        public static IFailResult<T> CreateFail<T>(NotificationErrors notificationErrors, ErrorType errorType)
        {
            return new FailResult<T>(notificationErrors, errorType);
        }

        public static IFailResult<T> CreateFail<T>()
        {
            return new FailResult<T>(NotificationErrors.Empty, ErrorType.UndefinedError);
        }

        public static IFailResult<T> CreateFailFromOtherFailType<T, K>(IFailResult<K> failResult)
        {
            var errors = failResult.NotificationErrors;
            return CreateFail<T>(errors, failResult.ErrorType);
        }
    }
}