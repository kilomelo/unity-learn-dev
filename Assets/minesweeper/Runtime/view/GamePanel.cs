using System;
using UnityEngine;

namespace Kilomelo.minesweeper.Runtime
{
    public class GamePanel : MonoBehaviour
    {
        private const string SavedLevel = "SavedLevel";
        [SerializeField] private BoardView _boardView;
        [SerializeField] private ControlView _controlView;
        [SerializeField] private ResultView _resultView;
        [SerializeField] private int randSeed = 0;

        [SerializeField] private Vector3Int[] _levelConfigs;

        private Game _game;
        private int _currentLevel;
        private GameLevelControl _lvlCtrl;
        void Awake()
        {
            if (null == _boardView || null == _controlView || null == _resultView)
            {
                Debug.LogError("view ref missing");
                return;
            }

            if (null == _levelConfigs)
            {
                _levelConfigs = new Vector3Int[1];
                _levelConfigs[0] = new Vector3Int(8, 8, 10);
            }

            Application.targetFrameRate = 60;
            randSeed = DateTime.Now.Millisecond;
            Init();
        }
        
        private void ChangeLevel(int level)
        {
            _currentLevel = level;
        }

        private void Init()
        {
            if (_currentLevel < 0) ChangeLevel(0);
            if (_currentLevel >= _levelConfigs.Length) ChangeLevel(_levelConfigs.Length - 1);
            var curLvlConfig = _levelConfigs[_currentLevel];
            _game = new Game(curLvlConfig.x, curLvlConfig.y, curLvlConfig.z, randSeed);
            _lvlCtrl = new GameLevelControl(_game, _levelConfigs, _currentLevel);
            _boardView.SetData(_game);
            _controlView.SetData(_game, _lvlCtrl);
            _resultView.SetData(_game);
            _game.Ready2Go();
            // Debug.Log(_game.ToString());
        }
    }

    internal class GameLevelControl
    {
        private Game _game;
        private Vector3Int[] _levelConfigs;
        private int _currentLevel;
        
        internal int CurrentLevelIdx => _currentLevel;

        private Vector3Int CurrentLevelCondig => _levelConfigs[_currentLevel];
        internal GameLevelControl(Game game, Vector3Int[] levelConfigs, int initialLevelIdx)
        {
            _game = game;
            _levelConfigs = levelConfigs ?? throw new NullReferenceException("");
            _currentLevel = initialLevelIdx;
            if (_currentLevel < 0 || _currentLevel >= _levelConfigs.Length)
            {
                Debug.LogError("initialLevelIdx out of range.");
                _currentLevel = 0;
            }
        }

        internal void ChangeToNextLevel()
        {
            _currentLevel++;
            if (_currentLevel >= _levelConfigs.Length) _currentLevel = 0;
            _game.ChangeBoardConfig(CurrentLevelCondig.x, CurrentLevelCondig.y, CurrentLevelCondig.z);
        }
    }
}