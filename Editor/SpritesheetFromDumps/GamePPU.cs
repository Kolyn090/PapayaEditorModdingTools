using System.Collections.Generic;
using UnityEngine;

namespace ModdingTool.Assets.Editor.SpritesheetFromDumps
{
    public class GamePPU
    {
        // =================================================================
        // To add a new game, make sure you extend GameOption and GameSettings.
        // =================================================================

        public enum GameOption
        {
            战魂铭人,
            元气骑士
        }

        public Dictionary<GameOption, int> GameSettings = new()
        {
            { GameOption.战魂铭人, 100 },
            { GameOption.元气骑士, 16 }
        };

        private GameOption _gameOption;
        public GameOption Option
        {
            get
            {
                return _gameOption;
            }
            set
            {
                _gameOption = value;
            }
        }

        public int GetPPU(GameOption gameOption)
        {
            if (!GameSettings.ContainsKey(gameOption))
            {
                Debug.LogError($"You have an unknown game option: {gameOption} to GamePPU. Please assign new key value pair.");
                Debug.LogError($"Resolving to use the default PPU value (100).");
                return 100;
            }

            return GameSettings[gameOption];
        }
    }
}