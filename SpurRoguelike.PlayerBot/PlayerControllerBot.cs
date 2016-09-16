using SpurRoguelike.Core;
using SpurRoguelike.Core.Primitives;
using SpurRoguelike.Core.Views;

namespace SpurRoguelike.PlayerBot
{
    public class BotPlayerController : IPlayerController
    {
        private PlayerBot playerBot;
        public Turn MakeTurn(LevelView levelView, IMessageReporter messageReporter)
        {
            if (playerBot == null)
                playerBot = new PlayerBot();

            playerBot.Refresh(levelView);
            playerBot.Tick();

            return playerBot.NextTurn;
        }
    }
}