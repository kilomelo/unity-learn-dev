using System;
using UnityEngine;

namespace Kilomelo.minesweeper.Runtime
{
    public class GamePanel : MonoBehaviour
    {
        private Game _game;
        [SerializeField] private BoardView _boardView;
        [SerializeField] private ControlView _controlView;
        [SerializeField] private int randSeed = 0;
        [SerializeField] private int width = 8;
        [SerializeField] private int height = 8;
        [SerializeField] private int mineCnt = 8;

        void Awake()
        {
            if (null == _boardView || null == _controlView)
            {
                Debug.LogError("view ref missing");
                return;
            }

            Application.targetFrameRate = 60;
            randSeed = DateTime.Now.Millisecond;
            Init();
        }

        private void Init()
        {
            _game = new Game(width, height, mineCnt, randSeed);
            _game.BlockChanged += _boardView.BlockChanged;
            _game.GameStateChanged += _boardView.GameStateChanged;
            _boardView.SetData(_game);
            _controlView.SetData(_game);
            _game.Ready2Go();
            // Debug.Log(_game.ToString());
        }
    }
}