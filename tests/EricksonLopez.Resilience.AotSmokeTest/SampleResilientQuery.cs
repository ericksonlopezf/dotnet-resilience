// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.Mediator;
using EricksonLopez.Resilience.Mediator.Contracts;
using EricksonLopez.Result;

namespace EricksonLopez.Resilience.NativeAotTests;

public sealed record SampleResilientQuery() : IQuery<Result<string>>, IResilientRequest
{
    public string ResiliencePolicy => "query-policy";
}
