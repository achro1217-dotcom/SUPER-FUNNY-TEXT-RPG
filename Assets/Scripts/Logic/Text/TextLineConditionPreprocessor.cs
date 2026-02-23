using System;
using System.Collections.Generic;

public static class TextLineConditionPreprocessor
{
    public static void BuildCaches(IReadOnlyList<TextLine> lines)
    {
        if (lines == null)
        {
            throw new ArgumentNullException(nameof(lines));
        }

        for (int i = 0; i < lines.Count; i++)
        {
            TextLine line = lines[i];
            if (line == null)
            {
                continue;
            }

            line.BuildConditionCache();
        }
    }
}
