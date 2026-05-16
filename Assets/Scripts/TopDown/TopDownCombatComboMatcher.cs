using System.Collections.Generic;

public static class TopDownCombatComboMatcher
{
    public static bool TryFindBestMatch(
        IReadOnlyList<TopDownCombatInputEvent> inputHistory,
        IReadOnlyList<TopDownCombatComboDefinition> comboDefinitions,
        float currentTime,
        out TopDownCombatComboDefinition bestMatch)
    {
        bestMatch = null;

        if (inputHistory == null || comboDefinitions == null || inputHistory.Count == 0 || comboDefinitions.Count == 0)
        {
            return false;
        }

        int bestLength = -1;
        int bestPriority = int.MinValue;
        int bestIndex = int.MaxValue;

        for (int i = 0; i < comboDefinitions.Count; i++)
        {
            TopDownCombatComboDefinition combo = comboDefinitions[i];
            if (combo == null || combo.SequenceCount == 0)
            {
                continue;
            }

            if (!Matches(inputHistory, combo, currentTime))
            {
                continue;
            }

            int sequenceLength = combo.SequenceCount;
            if (sequenceLength > bestLength || (sequenceLength == bestLength && combo.Priority > bestPriority) || (sequenceLength == bestLength && combo.Priority == bestPriority && i < bestIndex))
            {
                bestMatch = combo;
                bestLength = sequenceLength;
                bestPriority = combo.Priority;
                bestIndex = i;
            }
        }

        return bestMatch != null;
    }

    static bool Matches(IReadOnlyList<TopDownCombatInputEvent> inputHistory, TopDownCombatComboDefinition combo, float currentTime)
    {
        int sequenceCount = combo.SequenceCount;
        if (inputHistory.Count < sequenceCount)
        {
            return false;
        }

        if (combo.MaxSequenceAgeSeconds > 0f && currentTime - inputHistory[inputHistory.Count - sequenceCount].Time > combo.MaxSequenceAgeSeconds)
        {
            return false;
        }

        for (int sequenceIndex = sequenceCount - 1, historyIndex = inputHistory.Count - 1; sequenceIndex >= 0; sequenceIndex--, historyIndex--)
        {
            if (inputHistory[historyIndex].Action != combo.Sequence[sequenceIndex])
            {
                return false;
            }

            if (sequenceIndex > 0)
            {
                float gap = inputHistory[historyIndex].Time - inputHistory[historyIndex - 1].Time;
                if (gap > combo.MaxGapSeconds)
                {
                    return false;
                }
            }
        }

        return true;
    }
}