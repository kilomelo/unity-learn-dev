using UnityEngine;

namespace Kilomelo.minesweeper.Runtime
{
    public class GamePanel : MonoBehaviour
    {
        private Game _game;
        [SerializeField] private BoardView _boardView;
        [SerializeField] private int randSeed = 0;
        [SerializeField] private int width = 8;
        [SerializeField] private int height = 8;
        [SerializeField] private int mineCnt = 8;
        private void Awake()
        {
            // _game = new Game(width, height, mineCnt, randSeed);
        }

        void OnEnable()
        {
            if (null == _boardView)
            {
                Debug.LogError("Board view ref missing");
                return;
            }
            _game = new Game(width, height, mineCnt, randSeed);
            Debug.Log(_game.ToString());
            _boardView.SetData(_game, _game.Board);
        }
    }
}