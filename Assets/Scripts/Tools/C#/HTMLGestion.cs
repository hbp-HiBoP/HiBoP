public static class HTMLTools
{
    public static string SplitByTag(string stringToExamine, string tag)
    { 
        string startTag = "<" + tag + ">";
        string endTag = "</" + tag + ">";
        int startIndex = stringToExamine.IndexOf(startTag) + startTag.Length;
        int endIndex = stringToExamine.IndexOf(endTag);
        return stringToExamine.Substring(startIndex, endIndex - startIndex);
    }
}
