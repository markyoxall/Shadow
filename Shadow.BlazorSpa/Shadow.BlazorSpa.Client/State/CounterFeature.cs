using Fluxor;

namespace Shadow.BlazorSpa.Client.State;

public class CounterFeature : Feature<CounterState>
{
    public override string GetName() => "Counter";
    protected override CounterState GetInitialState() => new(0, false);
}
