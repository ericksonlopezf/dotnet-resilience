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
using Xunit;

namespace EricksonLopez.Resilience.Tests;

public sealed class TransientExceptionClassifierTests
{
    [Fact]
    public void IsTransient_WithSocketException_ReturnsTrue()
    {
        // Arrange
        var ex = new SocketException((int)SocketError.TimedOut);

        // Act & Assert
        TransientExceptionClassifier.IsTransient(ex).Should().BeTrue();
    }

    [Fact]
    public void IsTransient_WithResilienceTimeoutException_ReturnsTrue()
    {
        // Arrange
        var ex = new ResilienceTimeoutException(TimeSpan.FromSeconds(5), "policy");

        // Act & Assert
        TransientExceptionClassifier.IsTransient(ex).Should().BeTrue();
    }

    [Fact]
    public void IsTransient_WithUserCancellation_ReturnsFalse()
    {
        // Arrange
        var ex = new OperationCanceledException(new CancellationToken(true));

        // Act & Assert
        TransientExceptionClassifier.IsTransient(ex).Should().BeFalse();
    }

    [Fact]
    public void IsTransient_WithInvalidOperationException_ReturnsFalse()
    {
        // Arrange
        var ex = new InvalidOperationException("Business state error");

        // Act & Assert
        TransientExceptionClassifier.IsTransient(ex).Should().BeFalse();
    }

    [Fact]
    public void IsTransient_WithTransientHttpException_ReturnsTrue()
    {
        // Arrange
        var ex = new HttpRequestException("Gateway Timeout", null, HttpStatusCode.GatewayTimeout);

        // Act & Assert
        TransientExceptionClassifier.IsTransient(ex).Should().BeTrue();
    }

    [Fact]
    public void IsTransient_WithNonTransientHttpException_ReturnsFalse()
    {
        // Arrange
        var ex = new HttpRequestException("Bad Request", null, HttpStatusCode.BadRequest);

        // Act & Assert
        TransientExceptionClassifier.IsTransient(ex).Should().BeFalse();
    }
}
