using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SDroid.SteamWeb
{
    public class OperationRetryHelper
    {
        private static OperationRetryHelper _default = new OperationRetryHelper();

        [JsonConstructor]
        public OperationRetryHelper(
            int numberOfTries = 3,
            TimeSpan? requestDelay = null)
        {
            if (numberOfTries <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(numberOfTries),
                    "Number of tries should be a value bigger or equal to one.");
            }

            NumberOfTries = numberOfTries;
            RequestDelay = requestDelay ?? TimeSpan.FromSeconds(0.5);
        }

        public static OperationRetryHelper Default
        {
            get => _default;
            set => _default = value ?? new OperationRetryHelper();
        }

        [JsonIgnore]
        public Exception LastOperationException { get; private set; }

        [JsonIgnore]
        public bool LastOperationSuccess { get; private set; }

        public int NumberOfTries { get; }

        public TimeSpan RequestDelay { get; }


        // ReSharper disable once ExceptionNotThrown
        /// <summary>
        ///     Retries an operation till it return a valid response or stop throwing exceptions.
        /// </summary>
        /// <typeparam name="T">The return type of the operation.</typeparam>
        /// <param name="asyncOperation">The async operation.</param>
        /// <param name="validityChecker">The function to be used as the result validity checker.</param>
        /// <param name="shouldThrowExceptionOnTotalFailure">
        ///     Indicates if the operation should throw exceptions captured during a
        ///     failed final try.
        /// </param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The return value of the operation, or the failedResponse value on failure.</returns>
        /// <exception cref="Exception">The exception thrown on during the tries.</exception>
        /// <exception cref="AggregateException">
        ///     A exception representing collection of exceptions that are thrown during the
        ///     tries.
        /// </exception>
        // ReSharper disable once TooManyArguments
        public async Task<T> RetryOperationAsync<T>(
            Func<Task<T>> asyncOperation,
            Func<T, Task<bool>> validityChecker = null,
            // ReSharper disable once FlagArgument
            bool shouldThrowExceptionOnTotalFailure = true,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            validityChecker = validityChecker ??
                              (result => Task.FromResult(!EqualityComparer<T>.Default.Equals(result, default(T))));

            var exceptions = new List<Exception>();

            for (var i = 1; i <= NumberOfTries; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                try
                {
                    var result = await asyncOperation().ConfigureAwait(false);

                    if (await validityChecker(result).ConfigureAwait(false))
                    {
                        LastOperationSuccess = true;

                        return result;
                    }

                    Debug.WriteLine("RetryRequest Bad Result: {0}", result);
                }
                catch (Exception e)
                {
                    Debug.WriteLine("RetryRequest Exception: {0}", e);

                    exceptions.Add(e);
                }

                if (i != NumberOfTries &&
                    RequestDelay > default(TimeSpan) &&
                    !cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(RequestDelay, cancellationToken).ConfigureAwait(false);
                }
            }

            LastOperationSuccess = false;

            if (exceptions.Count > 0)
            {
                var exception = exceptions.Count == 1 ? exceptions[0] : new AggregateException(exceptions);
                LastOperationException = exception;

                if (shouldThrowExceptionOnTotalFailure)
                {
                    // ReSharper disable once ThrowingSystemException
                    throw exception;
                }
            }

            return default(T);
        }

        // ReSharper disable once ExceptionNotThrown
        /// <summary>
        ///     Retries an operation till it return a valid response or stop throwing exceptions.
        /// </summary>
        /// <typeparam name="T">The return type of the operation.</typeparam>
        /// <param name="operation">The operation.</param>
        /// <param name="validityChecker">The function to be used as the result validity checker.</param>
        /// <param name="shouldThrowExceptionOnTotalFailure">
        ///     Indicates if the operation should throw exceptions captured during a
        ///     failed final try.
        /// </param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The return value of the operation, or the failedResponse value on failure.</returns>
        /// <exception cref="Exception">The exception thrown on during the tries.</exception>
        /// <exception cref="AggregateException">
        ///     A exception representing collection of exceptions that are thrown during the
        ///     tries.
        /// </exception>
        // ReSharper disable once TooManyArguments
        public T RetryRequest<T>(
            Func<T> operation,
            Func<T, bool> validityChecker = null,
            // ReSharper disable once FlagArgument
            bool shouldThrowExceptionOnTotalFailure = true,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            validityChecker = validityChecker ?? (result => !EqualityComparer<T>.Default.Equals(result, default(T)));

            try
            {
                return RetryOperationAsync(
                    () => Task.FromResult(operation()),
                    result => Task.FromResult(validityChecker(result)),
                    shouldThrowExceptionOnTotalFailure,
                    cancellationToken
                ).Result;
            }
            catch (AggregateException e)
            {
                if (e.InnerExceptions.Count == 1)
                {
                    // ReSharper disable once ThrowingSystemException
                    throw e.InnerExceptions[0];
                }

                throw;
            }
        }
    }
}