using FateBastion.Core;

namespace FateBastion.Player
{
    /// <summary>
    /// Esc / right mouse tap priority in the Summoner map (S3, TEAM_ASSIGNMENT 3.1 #6):
    /// cancel the top CancelStack entry; if nothing was open, Esc requests pause and a right tap does nothing.
    /// </summary>
    public static class CancelPressRouter
    {
        public static CancelPressResult Route(bool isEscape)
        {
            if (CancelStack.TryCancelTop())
            {
                return CancelPressResult.Cancelled;
            }

            return isEscape ? CancelPressResult.PauseRequested : CancelPressResult.Ignored;
        }
    }
}
