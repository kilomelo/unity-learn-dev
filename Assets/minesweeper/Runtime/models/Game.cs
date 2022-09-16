using System;
using UnityEngine;
using Random = System.Random;

namespace Kilomelo.minesweeper.Runtime
{
    internal class Game
    {
        private enum EGameState : byte
        {
            NotInitialized,
            BeforeStart,
            Playing,
            Win,
            GameOver
        }

        private readonly Board _board;
        private readonly Recorder _recorder;
        private EGameState _state = EGameState.NotInitialized;
        
        
        private int _randomSeed;
        private Random _rand;
        public Action<int> BockChanged;
        
        public Board Board => _board;

        internal Game()
        {
            _randomSeed = 0;
            _rand = new Random(_randomSeed);
            _board = new Board(8, 8, 1);
            _recorder = new Recorder();
            Init();
            _state = EGameState.BeforeStart;
        }

        internal Game(int width, int height, int mineCnt, int randomSeed = 0)
        {
            _randomSeed = randomSeed;
            _rand = new Random(_randomSeed);
            _board = new Board(width, height, mineCnt);
            _recorder = new Recorder();
            Init();
            _state = EGameState.BeforeStart;
        }

        private void Init()
        {
            _board.Init(_rand);
        }

        internal void Dig(int blockIdx)
        {
            if (EGameState.BeforeStart != _state && EGameState.Playing != _state) return;
            // Debug.Log($"Dig {blockIdx}'s block");
            var blockValue = _board.GetBlock(blockIdx);
            if ((int)Board.EBlockType.Mine != blockValue)
            {
                var blockOpened = _recorder.IsOpened(blockValue > 0 ? blockIdx : blockValue);
                if (blockOpened) return;
                if (Open(blockIdx, out var changedBlockIdx))
                {
                    // 记录
                    if (_recorder.SetOpened(blockValue > 0 ? blockIdx : blockValue) == _board.ThreeBV)
                    {
                        // Debug.Log("Win");
                        // _state = EGameState.Win;
                    }
                    // 更新view
                    BockChanged?.Invoke(changedBlockIdx);
                }
            }
            else
            {
                _state = EGameState.GameOver;
                Debug.Log("Game Over");
            }
        }
        
        /// <summary>
        /// 打开地块
        /// </summary>
        /// <param name="blockIdx">指定的地块索引</param>
        /// <param name="changedBlockIdx">打开动作作用的地块索引，如果为负值，则为作用的空白连通区域id（该值只有在打开动作合法时有意义）</param>
        /// <returns>指定地块打开动作是否合法</returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="KeyNotFoundException"></exception>
        internal bool Open(int blockIdx, out int changedBlockIdx)
        {
            changedBlockIdx = 0;
            if (0 > blockIdx || _board.BlockCnt <= blockIdx)
            {
                Debug.LogError("todo err");
                return false;
            }

            var blockValue = _board.GetBlock(blockIdx);
            if (blockValue == (int) Board.EBlockType.Mine)
            {
                Debug.LogError($"Can't open mine block at {blockIdx}");
                return false;
            }
            // 如果是数字地块
            if (blockValue > 0)
            {
                changedBlockIdx = blockIdx;
            }
            // 如果是空白地块
            if (blockValue < 0)
            {
                changedBlockIdx = blockValue;
            }
            return true;
        }
        
        public override string ToString()
        {
            return _board?.ToString() ?? "Empty Game";
        }
    }
}
