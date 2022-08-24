using System;
using UnityEngine;

namespace Kilomelo.minesweeper.Runtime
{
    public class GamePanel : MonoBehaviour
    {
        private Game _game;
        [SerializeField] private int randSeed = 0;

        private void Awake()
        {
            _game = new Game();
        }

        void OnEnable()
        {
            _game.Init(randSeed);
            Debug.Log(_game.ToString());
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}