using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kilomelo.minesweeper.Runtime
{
    public class ControlView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _gameProgressLabel;
        [SerializeField] private Image _gameProgressImage;
        [SerializeField] private TextMeshProUGUI _timerLabel;
        [SerializeField] private TextMeshProUGUI _levelLabel;
        [SerializeField] private Button _newGameBtn;
        [SerializeField] private Button _changeLevelBtn;
        [SerializeField] private HorizontalLayoutGroup _changeLevelBtnLayout;
        // 打开排行榜按钮
        [SerializeField] private Button _leaderBoardBtn;
        [SerializeField] private GameObject _leaderBoardViewGameObject;
        // [SerializeField] private GameObject _win;
        // [SerializeField] private GameObject _gameOver;

        // private string[] _levelNames = new[] {"8*8", "M", "L"};
        
        private Game _game;
        private GameLevelControl _lvlCtrl;
        private void Awake()
        {
            if (null == _newGameBtn || null == _changeLevelBtn ||
                null == _levelLabel || null == _gameProgressLabel || null == _gameProgressImage || null == _timerLabel)
            {
                // todo exception
                throw new MissingComponentException("");
            }
            _newGameBtn.onClick.AddListener(NewGame);
            _changeLevelBtn.onClick.AddListener(ChangeLevel);
            _leaderBoardBtn.onClick.AddListener(OpenLeaderBoard);
        }

        private void Update()
        {
            if (null == _game) return;
            if (_game.State != Game.EGameState.Playing) return;
            var timeDelta = DateTime.Now - _game.StartTime;
            var displayTimeSeconds = Mathf.Floor((float)timeDelta.TotalSeconds);
            if (displayTimeSeconds > 999) displayTimeSeconds = 999;
            _timerLabel.text = $"{displayTimeSeconds} s";
        }

        internal void SetData(Game game, GameLevelControl levelCtrl)
        {
            _game = game;
            _lvlCtrl = levelCtrl;
            _game.GameStateChanged += GameStateChanged;
            _game.GameProgressChanged += GameProgressChanged;
            _game.BoardSizeChanged += BoardSizeChanged;
        }

        private void NewGame()
        {
            Debug.Log("New Game");
            _game.Restart();
        }

        private void ChangeLevel()
        {
            _lvlCtrl.ChangeToNextLevel();
        }

        private void OpenLeaderBoard()
        {
            _leaderBoardViewGameObject.SetActive(!_leaderBoardViewGameObject.activeSelf);
        }

        private void GameStateChanged(Game.EGameState gameState)
        {
            if (Game.EGameState.Win == gameState)
            {
                // _win.SetActive(true);
                // _gameOver.SetActive(false);
                _changeLevelBtn.interactable = true;
            }
            else if (Game.EGameState.GameOver == gameState)
            {
                // _win.SetActive(false);
                // _gameOver.SetActive(true);
                _changeLevelBtn.interactable = true;
            }
            else if (Game.EGameState.BeforeStart == gameState)
            {
                _timerLabel.text = string.Format("{0} s", 0);
                // _win.SetActive(false);
                // _gameOver.SetActive(false);
                _changeLevelBtn.interactable = true;
            }
            else
            {
                // _win.SetActive(false);
                // _gameOver.SetActive(false);
                _changeLevelBtn.interactable = false;
            }
        }
        
        private void BoardSizeChanged(int width, int height)
        {
            _levelLabel.text = $"{width}*{height}";//_levelNames[Math.Clamp(_lvlCtrl.CurrentLevelIdx, 0, _levelNames.Length - 1)];
            Canvas.ForceUpdateCanvases();
            _changeLevelBtnLayout.enabled = false;
            _changeLevelBtnLayout.enabled = true;
        }

        private void GameProgressChanged(int current, int threeBV)
        {
            var percentange = (float)current / threeBV;
            _gameProgressLabel.text = string.Format("{0} %", Mathf.FloorToInt(100f * percentange));
            _gameProgressImage.fillAmount = percentange;
        }
    }
}
