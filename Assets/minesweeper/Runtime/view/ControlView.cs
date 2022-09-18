using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kilomelo.minesweeper.Runtime
{
    public class ControlView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _gameProgressLabel;
        [SerializeField] private TextMeshProUGUI _timerLabel;
        [SerializeField] private Button _newGameBtn;
        [SerializeField] private Image _win;
        [SerializeField] private Image _gameOver;
        private Game _game;
        private void Awake()
        {
            if (null == _newGameBtn || null == _win || null == _gameOver || null == _gameProgressLabel || null == _timerLabel)
            {
                // todo exception
                throw new MissingComponentException("");
            }
            _newGameBtn.onClick.AddListener(NewGame);
        }

        private void Update()
        {
            if (null == _game) return;
            if (_game.State != Game.EGameState.Playing) return;
            var timeDelta = DateTime.Now - _game.StartTime;
            _timerLabel.text = string.Format("{0} s", timeDelta.Seconds);
        }

        internal void SetData(Game game)
        {
            _game = game;
            _game.GameStateChanged += GameStateChanged;
            _game.GameProgressChanged += GameProgressChanged;
        }

        private void NewGame()
        {
            Debug.Log("New Game");
            _game.Restart();
        }

        private void GameStateChanged(Game.EGameState gameState)
        {
            if (Game.EGameState.Win == gameState)
            {
                _win.enabled = true;
                _gameOver.enabled = false;
            }
            else if (Game.EGameState.GameOver == gameState)
            {
                _win.enabled = false;
                _gameOver.enabled = true;
            }
            else if (Game.EGameState.BeforeStart == gameState)
            {
                _timerLabel.text = string.Format("{0} s", 0);
                _win.enabled = false;
                _gameOver.enabled = false;
            }
            else
            {
                _win.enabled = false;
                _gameOver.enabled = false;
            }
        }

        private void GameProgressChanged(int current, int threeBV)
        {
            _gameProgressLabel.text = string.Format("{0} %", Mathf.FloorToInt(100f * current / threeBV));
        }
    }
}
