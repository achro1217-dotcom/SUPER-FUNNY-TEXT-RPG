using System.Collections.Generic;

public sealed class TextLineListData : IdIndexedListData<TextLine>
{
    public IReadOnlyList<TextLine> TextLines => Items;

    public TextLineListData(IReadOnlyList<TextLine> textLines)
        : base(textLines, line => line.Id, "TextLine")
    {
    }
}
