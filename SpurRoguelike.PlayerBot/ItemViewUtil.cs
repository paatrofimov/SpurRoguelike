using System.Collections.Generic;
using System.Linq;
using SpurRoguelike.Core.Views;

namespace SpurRoguelike.PlayerBot
{
    internal static class ItemViewUtil
    {
        public static double GetItemValue(this ItemView item)
        {
            return item.AttackBonus * 1.2 + item.DefenceBonus;
        }

        public static ItemView FindBestItem(this IEnumerable<ItemView> amongItems)
        {
            if (!amongItems.Any())
                return default(ItemView);

                return amongItems
                    .OrderByDescending(i => i.GetItemValue())
                    .FirstOrDefault();
        }
    }
}
