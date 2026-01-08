using System.Collections.Generic;

namespace BrenoMachado.ShopFlow.Helpers;

public static class EmojiConstants
{
    public static readonly List<string> ShopIcons = new()
    {
        // 1. Essentials
        "🛒", "🛍️", "🔖", "📝", "✅", "🏠", "🏪", "🎁", 
        
        // 2. Food & Drink
        "🍎", "🍌", "🍇", "🥑", "🥦", "🥕", "🌽", "🥔",
        "🍞", "🧀", "🥚", "🥞", "🥩", "🍗", "🍔", "🍕",
        "🥛", "☕", "🍺", "🍷", "🥤", "🧂", "🍬", "🍫",
        
        // 3. Household & Personal
        "🧻", "🧼", "🧴", "🪥", "🧹", "🧺", "💡", "🔋",
        "💊", "🩹", "👶", "🐱", "🐶", "🪴", "🛠️", "🚗",
        
        // 4. Occasions
        "🎂", "🎄", "🎃", "🏖️", "✈️", "⛺", "🍖", "🎉"
    };
}
