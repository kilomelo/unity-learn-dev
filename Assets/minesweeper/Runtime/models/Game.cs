using System;
using UnityEngine;
using Random = System.Random;

namespace Kilomelo.minesweeper.Runtime
{
    internal class Game
    {
        internal enum EGameState : byte
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

        private EGameState state
        {
            set
            {
                if (value == _state)
                {
                    Debug.LogWarning("Set Game state with same value.");
                    return;
                }
                _state = value;
                if (EGameState.Playing == value)
                {
                    _startTime = DateTime.Now;
                }
                GameStateChanged?.Invoke(_state);
            }
        }

        internal EGameState State => _state;
        
        
        private int _randomSeed;
        private Random _rand;
        private DateTime _startTime;
        internal Action<int> BockChanged;
        internal Action<EGameState> GameStateChanged;
        internal Action<int, int> GameProgressChanged;
        internal Board Board => _board;
        internal int CompleteMinimalClick => _recorder?.OpendMinimalClickCnt ?? 0;
        internal DateTime StartTime => _startTime;

        internal Game() : this(8, 8, 1) {}

        internal Game(int width, int height, int mineCnt, int randomSeed = 0)
        {
            _randomSeed = randomSeed;
            _rand = new Random(_randomSeed);
            _board = new Board(width, height, mineCnt);
            _recorder = new Recorder();
            _state = EGameState.NotInitialized;
        }

        private void Init()
        {
            _board.Init(_rand);
            _recorder.Init();
            GameProgressChanged?.Invoke(0, _board.ThreeBV);
        }

        internal void Ready2Go()
        {
            Init();
            state = EGameState.BeforeStart;
        }

        internal void Restart()
        {
            if (_state == EGameState.Playing || _state == EGameState.Win || _state == EGameState.GameOver)
            {
                Init();
                state = EGameState.BeforeStart;
            }
        }

        internal void Dig(int blockIdx)
        {
            if (EGameState.BeforeStart != _state && EGameState.Playing != _state) return;
            if (EGameState.BeforeStart == _state) state = EGameState.Playing;
            // Debug.Log($"Dig {blockIdx}'s block");
            var blockValue = _board.GetBlock(blockIdx);
            if ((int)Board.EBlockType.Mine != blockValue)
            {
                var blockOpened = _recorder.IsOpened(blockValue > 0 ? blockIdx : blockValue);
                if (blockOpened) return;
                if (Open(blockIdx, out var changedBlockIdx))
                {
                    // 记录
                    if (_board.IsMinimalBlock(changedBlockIdx))
                    {
                        _recorder.SetOpenedMinimal(changedBlockIdx);
                    }
                    else
                    {
                        _recorder.SetOpened(changedBlockIdx);
                    }
                    GameProgressChanged?.Invoke(_recorder.OpendMinimalClickCnt, _board.ThreeBV);
                    if (_recorder.OpendMinimalClickCnt == _board.ThreeBV)
                    {
                        Debug.Log("Win");
                        state = EGameState.Win;
                    }
                    // 更新view
                    BockChanged?.Invoke(changedBlockIdx);
                }
            }
            else
            {
                state = EGameState.GameOver;
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
        private bool Open(int blockIdx, out int changedBlockIdx)
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
