namespace FateBastion.Editor.Heroes
{
    /// <summary>One finding of Tools > Validate Hero Prefabs; Context is the asset selected when the row is clicked.</summary>
    public readonly struct HeroValidationIssue
    {
        public readonly string HeroId;
        public readonly HeroValidationSeverity Severity;
        public readonly string Message;
        public readonly UnityEngine.Object Context;

        public HeroValidationIssue(string heroId, HeroValidationSeverity severity, string message, UnityEngine.Object context)
        {
            HeroId = heroId;
            Severity = severity;
            Message = message;
            Context = context;
        }
    }
}
