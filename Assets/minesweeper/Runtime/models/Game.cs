using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Random = System.Random;

namespace Kilomelo.minesweeper.Runtime
{
    internal class Game
    {
        private enum EGameState : byte
        {
            BeforeStart,
            Playing,
            Succeed,
            Failed
        }

        private Board _board;

        private EGameState _state;
        
        
        private int _randomSeed;
        private Random _rand;
        public Board Board => _board;

        internal Game()
        {
            _randomSeed = 0;
            _rand = new Random(_randomSeed);
            _board = new Board(8, 8, 1);
            
            Init();
            _state = EGameState.BeforeStart;
        }

        internal Game(int width, int height, int mineCnt, int randomSeed = 0)
        {
            _randomSeed = randomSeed;
            _rand = new Random(_randomSeed);
            _board = new Board(width, height, mineCnt);

            Init();
            _state = EGameState.BeforeStart;
        }

        private void Init()
        {
            _board.Init(_rand);
        }

        internal void Dig(int blockIdx)
        {
            Debug.Log($"Dig {blockIdx}'s block");
        }
        
        public override string ToString()
        {
            return _board?.ToString() ?? "Empty Game";
        }
    }
}
