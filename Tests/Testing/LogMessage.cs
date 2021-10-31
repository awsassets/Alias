    public class LogMessage
    {
        public LogMessage(string text, object messageImportance)
        {
            Text = text;
            MessageImportance = messageImportance;
        }

        public string Text { get; }
        public object MessageImportance { get; }
    }
