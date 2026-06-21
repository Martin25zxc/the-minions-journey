using System;

public sealed class MissionObjectiveRuntimeState
{
    public MissionObjectiveRuntimeState(MissionObjectiveDefinition definition)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        CurrentAmount = 0;
        IsCompleted = false;
        CompletionEventEmitted = false;
    }

    public MissionObjectiveDefinition Definition { get; }
    public int CurrentAmount { get; private set; }
    public bool IsCompleted { get; private set; }
    public bool CompletionEventEmitted { get; private set; }

    public string ObjectiveId => Definition.ObjectiveId;
    public int RequiredAmount => Math.Max(1, Definition.RequiredAmount);
    public bool IsRequired => Definition.Importance == ObjectiveImportance.Required;
    public bool IsBonus => Definition.Importance == ObjectiveImportance.Bonus;

    public bool AddProgress(int amount, out bool completedNow)
    {
        completedNow = false;

        if (amount <= 0)
        {
            return false;
        }

        if (IsCompleted)
        {
            return false;
        }

        int previousAmount = CurrentAmount;
        int nextAmount = Math.Min(CurrentAmount + amount, RequiredAmount);

        if (nextAmount == previousAmount)
        {
            return false;
        }

        CurrentAmount = nextAmount;

        if (CurrentAmount >= RequiredAmount)
        {
            IsCompleted = true;
            completedNow = true;
        }

        return true;
    }

    public bool SetProgress(int amount, out bool completedNow)
    {
        completedNow = false;

        if (IsCompleted)
        {
            return false;
        }

        int nextAmount = Clamp(amount, 0, RequiredAmount);

        if (nextAmount == CurrentAmount)
        {
            return false;
        }

        CurrentAmount = nextAmount;

        if (CurrentAmount >= RequiredAmount)
        {
            IsCompleted = true;
            completedNow = true;
        }

        return true;
    }

    public bool TryConsumeCompletionEvent()
    {
        if (!IsCompleted || CompletionEventEmitted)
        {
            return false;
        }

        CompletionEventEmitted = true;
        return true;
    }

    public string GetProgressText()
    {
        return $"{CurrentAmount}/{RequiredAmount}";
    }

    private static int Clamp(int value, int min, int max)
    {
        if (value < min)
        {
            return min;
        }

        if (value > max)
        {
            return max;
        }

        return value;
    }
}
