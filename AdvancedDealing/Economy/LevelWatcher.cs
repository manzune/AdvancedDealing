using System;
using System.Collections.Generic;

namespace AdvancedDealing.Economy
{
    public class LevelWatcher
    {
        public const int MaxCustomersBase = 4;

        public const int ItemSlotsBase = 5;

        public const float SpeedMultiplierBase = 0.8f;

        public const float ContractCompleteXP = 15f;

        private readonly DealerManager _dealerManager;

        public LevelWatcher(DealerManager dealerManager)
        {
            _dealerManager = dealerManager;
        }

        public void AddXP(float amount)
        {
            if (!ModConfig.RealisticMode) return;

            _dealerManager.Experience += amount;
            int calculatedLevel = CalculateLevel(_dealerManager.Experience);

            if (calculatedLevel > _dealerManager.Level)
            {
                LevelUp(calculatedLevel);
            }

            Utils.Logger.Debug("LevelWatcher", $"XP added to {_dealerManager.Dealer.fullName}: {_dealerManager.Experience}");
        }

        public void LevelUp(int level) 
        {
            int multiplicator = level - 1;

            _dealerManager.Level = level;
            _dealerManager.MaxCustomers = MaxCustomersBase + (multiplicator * ModConfig.MaxCustomersPerLevel);
            _dealerManager.ItemSlots = ItemSlotsBase + (multiplicator * ModConfig.ItemSlotsPerLevel);
            _dealerManager.SpeedMultiplier = SpeedMultiplierBase + (multiplicator * ModConfig.SpeedIncreasePerLevel);

            _dealerManager.HasChanged = true;

            // TODO: Notify player
            // TODO: Experience Modificator

            Utils.Logger.Debug("LevelWatcher", $"{_dealerManager.Dealer.fullName} reached a new level: {level}");
        }

        public static int CalculateLevel(float experience)
        {
            int level = 0;
            bool levelFound = false;

            while (!levelFound)
            {
                level++;
                float neededExperience = (float)Math.Round(4 * Math.Pow(level + 1, 3) / 3);
                if (experience < neededExperience)
                {
                    levelFound = true;
                }
            }

            /*
            for (int i = levels.Count - 1; !levelFound && i >= 0; i--)
            {
                if (levels[i].RequiredExperience < experience)
                {
                    level++;
                }
                else
                {
                    levelFound = true;
                }
            }
            */

            return level;
        }
    }
}
