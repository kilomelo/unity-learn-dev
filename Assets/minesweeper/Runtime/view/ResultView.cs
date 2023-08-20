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
        [SerializeField] private TextMeshProUGUI _recordLabel;
        [SerializeField] private Button _continueBtn;
        [SerializeField] private VerticalLayoutGroup _layout;
        // [SerializeField] private Canvas _canvas;

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
            _game.Recorder.GameResultEvent += GameResult;
        }

        private void Continue()
        {
            Debug.Log("Continue");
            _game.Restart();
        }

        private void GameResult(bool win, long timeMilliseconnds, string recordInfo)
        {
            Debug.Log($"ResultView.GameResult, win: {win}, timeMilliseconnds: {timeMilliseconnds}, recordInfo: {recordInfo}");
            _resultLabel.text = $"{(win ? "完成" : "未完成")}";
            // var timeDelta = DateTime.Now - _game.StartTime;
            _timeLabel.text = $"{timeMilliseconnds * 0.001f:F1} s";
            _3bvLabel.text = $"{_game.CompleteMinimalClick} / {_game.CurBoard.ThreeBV}";
            _3bvsLabel.text = $"{(float)_game.CompleteMinimalClick / timeMilliseconnds * 1000:F1}";
            _recordLabel.enabled = !string.IsNullOrEmpty(recordInfo);
            _recordLabel.text = recordInfo;
            gameObject.SetActive(true);
            Canvas.ForceUpdateCanvases();
            _layout.enabled = false;
            _layout.enabled = true;
        }
        private void GameStateChanged(Game.EGameState eGameState)
        {
            if (Game.EGameState.BeforeStart == eGameState)
            {
                gameObject.SetActive(false);
            }
        }
    }
}