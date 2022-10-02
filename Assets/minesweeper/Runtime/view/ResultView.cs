using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kilomelo.minesweeper.Runtime
{
    public class ResultView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _resultLabel;
        [SerializeField] private TextMeshProUGUI _timeLabel;
        [SerializeField] private TextMeshProUGUI _3bvLabel;
        [SerializeField] private TextMeshProUGUI _3bvsLabel;
        [SerializeField] private Button _continueBtn;

        private Game _game;
        private void Awake()
        {
            if (null == _resultLabel || null == _timeLabel || null == _3bvLabel || null == _3bvsLabel ||
                null == _continueBtn)
            {
                // todo exception
                throw new MissingComponentException("");
            }
            _continueBtn.onClick.AddListener(Continue);
        }

        internal void SetData(Game game)
        {
            _game = game;
            _game.GameStateChanged += GameStateChanged;
        }

        private void Continue()
        {
            Debug.Log("Continue");
            _game.Restart();
        }

        private void GameStateChanged(Game.EGameState gameState)
        {
            if (Game.EGameState.Win == gameState || Game.EGameState.GameOver == gameState)
            {
                gameObject.SetActive(true);
                _resultLabel.text = $"Result: {(Game.EGameState.Win == gameState ? "Complete" : "Failed")}";
                var timeDelta = DateTime.Now - _game.StartTime;
                var duration = (int)(timeDelta.TotalMilliseconds * 0.001f * 10) * 0.1f;
                _timeLabel.text = $"Time: {duration} s";
                _3bvLabel.text = $"3BV: {_game.CompleteMinimalClick}/{_game.CurBoard.ThreeBV}";
                Debug.Log($"_game.CompleteMinimalClick: {_game.CompleteMinimalClick}");
                var threeBVPerSec = (int)((float) _game.CompleteMinimalClick / timeDelta.TotalMilliseconds * 1000 * 100) * 0.01f;
                _3bvsLabel.text = $"3BV/s: {threeBVPerSec}";
            }

            if (Game.EGameState.BeforeStart == gameState)
            {
                gameObject.SetActive(false);
            }
        }
    }
}