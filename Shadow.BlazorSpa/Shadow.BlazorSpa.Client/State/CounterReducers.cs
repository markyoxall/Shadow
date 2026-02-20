using Fluxor;

namespace Shadow.BlazorSpa.Client.State;

public static class CounterReducers
{
    [ReducerMethod]
    public static CounterState ReduceIncrementAction(CounterState state, IncrementAction action)
        => state with { ClickCount = state.ClickCount + 1 };
}
