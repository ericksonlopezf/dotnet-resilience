// Copyright © Erickson Lopez. MIT License.
using System.Threading.Tasks;
using EricksonLopez.Mediator;
using EricksonLopez.Result;

namespace EricksonLopez.Resilience.NativeAotTests;

public struct AotMockNext : INext<Result<string>>
{
    public ValueTask<Result<string>> InvokeAsync()
    {
        return ValueTask.FromResult(Result<string>.Success("AotQuerySuccess"));
    }
}
