using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using FlaUI.Core.Tools;

namespace BluetoothMonitor.E2E.Helpers;

public static class UiWait
{
    public static AutomationElement WaitFor(
        AutomationElement root,
        Func<ConditionFactory, ConditionBase> condition,
        TimeSpan? timeout = null)
    {
        var result = Retry.WhileNull(
            () => root.FindFirstDescendant(condition),
            timeout ?? TimeSpan.FromSeconds(20),
            TimeSpan.FromMilliseconds(200));

        if (!result.Success || result.Result is null)
        {
            throw new TimeoutException($"Element not found within {(timeout ?? TimeSpan.FromSeconds(20)).TotalSeconds}s.");
        }

        return result.Result;
    }

    public static void WaitUntil(
        Func<bool> condition,
        TimeSpan? timeout = null,
        string? description = null)
    {
        var result = Retry.WhileFalse(
            condition,
            timeout ?? TimeSpan.FromSeconds(20),
            TimeSpan.FromMilliseconds(200));

        if (!result.Success)
        {
            throw new TimeoutException(description ?? "Condition not met in time.");
        }
    }
}

public static class AutomationElementExtensions
{
    public static AutomationElement ById(this AutomationElement root, string automationId) =>
        UiWait.WaitFor(root, cf => cf.ByAutomationId(automationId));

    public static AutomationElement? TryById(this AutomationElement root, string automationId) =>
        root.FindFirstDescendant(cf => cf.ByAutomationId(automationId));

    public static void ClickId(this AutomationElement root, string automationId)
    {
        var el = root.ById(automationId);
        el.Focus();
        el.Click();
    }
}
