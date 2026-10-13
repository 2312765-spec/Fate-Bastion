namespace FateBastion.Player
{
    /// <summary>Map and action names in FateBastion.inputactions; the router looks them up once by these names (S3).</summary>
    public static class InputActionNames
    {
        // Map names match ActionMapId so SetActionMap can find them.
        public const string SummonerMap = "Summoner";
        public const string UIMap = "UI";

        // Summoner map.
        public const string Move = "Move";
        public const string Sprint = "Sprint";
        /// <summary>Right mouse with Hold: performed after the hold time, cancelled on release.</summary>
        public const string Look = "Look";
        /// <summary>Mouse delta; turned into Look events only while Look is held.</summary>
        public const string LookDelta = "LookDelta";
        public const string Zoom = "Zoom";
        public const string Select = "Select";
        /// <summary>Right mouse Tap or Esc in the Summoner map; Esc only in the UI map.</summary>
        public const string Cancel = "Cancel";
        public const string Meteor = "Meteor";
        public const string Upgrade = "Upgrade";
        public const string Sell = "Sell";
        public const string Ultimate = "Ultimate";
        public const string SkipWave = "SkipWave";
        public const string Debug = "Debug";

        /// <summary>Deck1..Deck5, index = deck slot 0..4.</summary>
        public static readonly string[] DeckSlots = { "Deck1", "Deck2", "Deck3", "Deck4", "Deck5" };

        // UI map.
        public const string Navigate = "Navigate";
        public const string Submit = "Submit";
        public const string Point = "Point";
        public const string Click = "Click";
    }
}
