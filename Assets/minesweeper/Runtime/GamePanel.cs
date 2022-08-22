using UnityEngine;

namespace Kilomelo.minesweeper.Runtime
{
    public class GamePanel : MonoBehaviour
    {
        private Game _game;
        // Start is called before the first frame update
        void Start()
        {
            _game = new Game();
            _game.Init();
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}