// Copyright © Erickson Lopez. MIT License.
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using AwesomeAssertions;
using EricksonLopez.Resilience.Classification;
using EricksonLopez.Resilience.Exceptions;
using EricksonLopez.Result;
using NSubstitute;
using Xunit;

namespace EricksonLopez.Resilience.Tests;

public sealed class ResultRetryClassifierTests
{
    private readonly ResultRetryClassifier _classifier = ResultRetryClassifier.Instance;

    [Fact]
    public void ClassifyResult_WithSuccessResult_ReturnsDoNotRetry()
    {
        // Arrange
        var result = Result<string>.Success("ok");

        // Act
        var decision = _classifier.ClassifyResult(result);

        // Assert
        decision.Should().Be(RetryabilityDecision.DoNotRetry);
    }

    [Fact]
    public void ClassifyResult_WithTransientErrorRetryability_ReturnsRetry()
    {
        // Arrange
        var error = new Error("CODE_TRANSIENT", "Temporary database hiccup", ErrorType.Failure, ErrorSeverity.Error, ErrorRetryability.Transient);
        var result = Result<int>.Failure(error);

        // Act
        var decision = _classifier.ClassifyResult(result);

        // Assert
        decision.Should().Be(RetryabilityDecision.Retry);
    }

    [Fact]
    public void ClassifyResult_WithPermanentErrorRetryability_ReturnsDoNotRetry()
    {
        // Arrange
        var error = new Error("CODE_PERM", "Hard permanent failure", ErrorType.Failure, ErrorSeverity.Error, ErrorRetryability.Permanent);
        var result = Result<int>.Failure(error);

        // Act
        var decision = _classifier.ClassifyResult(result);

        // Assert
        decision.Should().Be(RetryabilityDecision.DoNotRetry);
    }

    [Fact]
    public void ClassifyResult_WithOutcomeFailureAndNullError_ReturnsUndetermined()
    {
        // Arrange
        var mockOutcome = Substitute.For<IResultOutcome>();
        mockOutcome.IsSuccess.Returns(false);
        mockOutcome.Error.Returns((Error?)null);

        // Act
        var decision = _classifier.ClassifyResult(mockOutcome);

        // Assert
        decision.Should().Be(RetryabilityDecision.Undetermined);
    }

    [Fact]
    public void ClassifyResult_WithNonIResultOutcome_ReturnsUndetermined()
    {
        // Arrange & Act
        var decision1 = _classifier.ClassifyResult("arbitrary string");
        var decision2 = _classifier.ClassifyResult(12345);
        var decision3 = _classifier.ClassifyResult<object?>(null);

        // Assert
        decision1.Should().Be(RetryabilityDecision.Undetermined);
        decision2.Should().Be(RetryabilityDecision.Undetermined);
        decision3.Should().Be(RetryabilityDecision.Undetermined);
    }

    [Fact]
    public void ClassifyError_WithNullError_ReturnsDoNotRetry()
    {
        // Act
        var decision = _classifier.ClassifyError(null);

        // Assert
        decision.Should().Be(RetryabilityDecision.DoNotRetry);
    }

    [Fact]
    public void ClassifyError_WithNonErrorInstance_ReturnsUndetermined()
    {
        // Act
        var decision = _classifier.ClassifyError("invalid error object");

        // Assert
        decision.Should().Be(RetryabilityDecision.Undetermined);
    }

    [Theory]
    [InlineData(ErrorType.Unavailable, RetryabilityDecision.Retry)]
    [InlineData(ErrorType.Infrastructure, RetryabilityDecision.Retry)]
    [InlineData(ErrorType.Validation, RetryabilityDecision.DoNotRetry)]
    [InlineData(ErrorType.Domain, RetryabilityDecision.DoNotRetry)]
    [InlineData(ErrorType.Unauthorized, RetryabilityDecision.DoNotRetry)]
    [InlineData(ErrorType.Forbidden, RetryabilityDecision.DoNotRetry)]
    [InlineData(ErrorType.NotFound, RetryabilityDecision.DoNotRetry)]
    [InlineData(ErrorType.Conflict, RetryabilityDecision.DoNotRetry)]
    [InlineData(ErrorType.Failure, RetryabilityDecision.Undetermined)]
    [InlineData(ErrorType.Unexpected, RetryabilityDecision.Undetermined)]
    public void ClassifyError_ByCategory_ReturnsExpectedDecision(ErrorType errorType, RetryabilityDecision expected)
    {
        // Arrange
        var error = new Error("CODE", "Message", errorType, ErrorSeverity.Error, ErrorRetryability.NotApplicable);

        // Act
        var decision = _classifier.ClassifyError(error);

        // Assert
        decision.Should().Be(expected);
    }

    [Fact]
    public void ClassifyException_WithNullException_ReturnsDoNotRetry()
    {
        // Act
        var decision = _classifier.ClassifyException(null!);

        // Assert
        decision.Should().Be(RetryabilityDecision.DoNotRetry);
    }

    [Fact]
    public void ClassifyException_WithTimeout_ReturnsRetry()
    {
        // Arrange
        var timeoutEx = new ResilienceTimeoutException(TimeSpan.FromSeconds(5), "TestPolicy");
        var sysTimeout = new TimeoutException();

        // Act & Assert
        _classifier.ClassifyException(timeoutEx).Should().Be(RetryabilityDecision.Retry);
        _classifier.ClassifyException(sysTimeout).Should().Be(RetryabilityDecision.Retry);
    }

    [Fact]
    public void ClassifyException_WithUserCancellation_ReturnsDoNotRetry()
    {
        // Arrange
        var cancelEx = new OperationCanceledException(new CancellationToken(true));

        // Act
        var decision = _classifier.ClassifyException(cancelEx);

        // Assert
        decision.Should().Be(RetryabilityDecision.DoNotRetry);
    }

    [Fact]
    public void ClassifyException_WithHttpRequestExceptionWithoutStatusCode_ReturnsRetry()
    {
        // Arrange
        var httpEx = new HttpRequestException("Network failure without HTTP response");

        // Act
        var decision = _classifier.ClassifyException(httpEx);

        // Assert
        decision.Should().Be(RetryabilityDecision.Retry);
    }

    [Theory]
    [InlineData(HttpStatusCode.RequestTimeout, RetryabilityDecision.Retry)] // 408
    [InlineData(HttpStatusCode.TooManyRequests, RetryabilityDecision.Retry)] // 429
    [InlineData(HttpStatusCode.InternalServerError, RetryabilityDecision.Retry)] // 500
    [InlineData(HttpStatusCode.BadGateway, RetryabilityDecision.Retry)] // 502
    [InlineData(HttpStatusCode.ServiceUnavailable, RetryabilityDecision.Retry)] // 503
    [InlineData(HttpStatusCode.GatewayTimeout, RetryabilityDecision.Retry)] // 504
    [InlineData(HttpStatusCode.BadRequest, RetryabilityDecision.DoNotRetry)]
    [InlineData(HttpStatusCode.NotFound, RetryabilityDecision.DoNotRetry)]
    [InlineData(HttpStatusCode.Unauthorized, RetryabilityDecision.DoNotRetry)]
    [InlineData(HttpStatusCode.Forbidden, RetryabilityDecision.DoNotRetry)]
    public void ClassifyException_WithHttpRequestException_ReturnsExpectedDecision(HttpStatusCode statusCode, RetryabilityDecision expected)
    {
        // Arrange
        var httpEx = new HttpRequestException("HTTP Error", null, statusCode);

        // Act
        var decision = _classifier.ClassifyException(httpEx);

        // Assert
        decision.Should().Be(expected);
    }

    [Fact]
    public void ClassifyException_WithSocketOrIOException_ReturnsRetry()
    {
        // Arrange
        var socketEx = new SocketException((int)SocketError.ConnectionReset);
        var ioEx = new IOException("Pipe broken");

        // Act & Assert
        _classifier.ClassifyException(socketEx).Should().Be(RetryabilityDecision.Retry);
        _classifier.ClassifyException(ioEx).Should().Be(RetryabilityDecision.Retry);
    }

    [Fact]
    public void ClassifyException_WithContractOrValidationException_ReturnsDoNotRetry()
    {
        // Arrange
        var argEx = new ArgumentNullException("param");
        var invOpEx = new InvalidOperationException("bad state");
        var notSuppEx = new NotSupportedException("not supported");

        // Act & Assert
        _classifier.ClassifyException(argEx).Should().Be(RetryabilityDecision.DoNotRetry);
        _classifier.ClassifyException(invOpEx).Should().Be(RetryabilityDecision.DoNotRetry);
        _classifier.ClassifyException(notSuppEx).Should().Be(RetryabilityDecision.DoNotRetry);
    }

    [Fact]
    public void ClassifyException_WithUnhandledException_ReturnsUndetermined()
    {
        // Arrange
        var formatEx = new FormatException("invalid format");

        // Act
        var decision = _classifier.ClassifyException(formatEx);

        // Assert
        decision.Should().Be(RetryabilityDecision.Undetermined);
    }

    [Fact]
    public void ClassifyException_WithTimeoutRejectedException_ReturnsRetry()
    {
        var localTimeoutEx = new TimeoutRejectedException();
        _classifier.ClassifyException(localTimeoutEx).Should().Be(RetryabilityDecision.Retry);

        var pollyTimeoutEx = new Polly.Timeout.TimeoutRejectedException();
        _classifier.ClassifyException(pollyTimeoutEx).Should().Be(RetryabilityDecision.Retry);
    }

    [Theory]
    [InlineData(1205, RetryabilityDecision.Retry)]
    [InlineData(3960, RetryabilityDecision.Retry)]
    [InlineData(10053, RetryabilityDecision.Retry)]
    [InlineData(10054, RetryabilityDecision.Retry)]
    [InlineData(10060, RetryabilityDecision.Retry)]
    [InlineData(40613, RetryabilityDecision.Retry)]
    [InlineData(40197, RetryabilityDecision.Retry)]
    [InlineData(40501, RetryabilityDecision.Retry)]
    [InlineData(547, RetryabilityDecision.Undetermined)]
    public void ClassifyException_WithSqlException_ClassifiesByNumber(int number, RetryabilityDecision expected)
    {
        var ex = new SqlException { Number = number };
        _classifier.ClassifyException(ex).Should().Be(expected);

        // Also test as InnerException
        var wrapped = new DummyWrapperException("Wrapped", ex);
        _classifier.ClassifyException(wrapped).Should().Be(expected);
    }

    [Fact]
    public void ClassifyException_WithSqlExceptionWithoutNumberOrInvalidType_ReturnsUndetermined()
    {
        var exNoNumber = new AnotherNamespace.SqlException();
        _classifier.ClassifyException(exNoNumber).Should().Be(RetryabilityDecision.Undetermined);

        var exStringNumber = new AnotherNamespaceWithStr.SqlException { Number = "1205" };
        _classifier.ClassifyException(exStringNumber).Should().Be(RetryabilityDecision.Undetermined);
    }

    [Theory]
    [InlineData("40P01", RetryabilityDecision.Retry)]
    [InlineData("40001", RetryabilityDecision.Retry)]
    [InlineData("08000", RetryabilityDecision.Retry)]
    [InlineData("08003", RetryabilityDecision.Retry)]
    [InlineData("08006", RetryabilityDecision.Retry)]
    [InlineData("57P01", RetryabilityDecision.Retry)]
    [InlineData("23505", RetryabilityDecision.Undetermined)]
    [InlineData(null, RetryabilityDecision.Undetermined)]
    public void ClassifyException_WithNpgsqlException_ClassifiesBySqlState(string? sqlState, RetryabilityDecision expected)
    {
        var ex = new NpgsqlException { SqlState = sqlState };
        _classifier.ClassifyException(ex).Should().Be(expected);
    }

    [Fact]
    public void ClassifyException_WithNpgsqlExceptionWithoutSqlState_ReturnsUndetermined()
    {
        var exNoSqlState = new AnotherNamespaceNpgsql.NpgsqlException();
        _classifier.ClassifyException(exNoSqlState).Should().Be(RetryabilityDecision.Undetermined);
    }

    [Fact]
    public void ClassifyException_WithPostgresException_ClassifiesBySqlState()
    {
        var ex = new PostgresException { SqlState = "40P01" };
        _classifier.ClassifyException(ex).Should().Be(RetryabilityDecision.Retry);

        var exNonTransient = new PostgresException { SqlState = "23505" };
        _classifier.ClassifyException(exNonTransient).Should().Be(RetryabilityDecision.Undetermined);
    }

    [Theory]
    [InlineData(1213, RetryabilityDecision.Retry)]
    [InlineData(1205, RetryabilityDecision.Retry)]
    [InlineData(1062, RetryabilityDecision.Undetermined)]
    public void ClassifyException_WithMySqlException_ClassifiesByNumber(int number, RetryabilityDecision expected)
    {
        var ex = new MySqlException { Number = number };
        _classifier.ClassifyException(ex).Should().Be(expected);
    }

    [Fact]
    public void ClassifyException_WithMySqlExceptionWithoutNumber_ReturnsUndetermined()
    {
        var exNoNumber = new AnotherNamespaceMySql.MySqlException();
        _classifier.ClassifyException(exNoNumber).Should().Be(RetryabilityDecision.Undetermined);
    }
}

public class SqlException : Exception
{
    public int Number { get; set; }
}

public class NpgsqlException : Exception
{
    public string? SqlState { get; set; }
}

public class PostgresException : Exception
{
    public string? SqlState { get; set; }
}

public class MySqlException : Exception
{
    public int Number { get; set; }
}

public class TimeoutRejectedException : Exception
{
}

public class DummyWrapperException : Exception
{
    public DummyWrapperException(string message, Exception inner) : base(message, inner) { }
}

