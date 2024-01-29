namespace MyLab.LogAgent.LogFormats
{
    static class NetFormatLogic
    {
        public static void ExtractCategory(string logText, out string category, out string leftText)
        {
            int firstLineIndex = logText.IndexOf('\n');
            if (firstLineIndex == -1)
                throw new FormatException("Empty .NET log message");

            string categoryStr = logText.Remove(firstLineIndex).TrimEnd();

            if (categoryStr[^3] == '[' && char.IsDigit(categoryStr[^2]) && categoryStr[^1] == ']')
            {
                category = categoryStr.Remove(categoryStr.Length - 3);
            }
            else
            {
                category = categoryStr;
            }

            leftText = logText
                .Substring(firstLineIndex)
                .TrimStart('\n', '\r');
        }
    }
}
