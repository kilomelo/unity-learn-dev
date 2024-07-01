using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = System.Random;

namespace Kilomelo.minesweeper.Runtime
{
    internal class Game
    {
        /// <summary>
        /// 棋盘队列初始大小
        /// </summary>
        private readonly int BoardQueueInitialSize = 5;
        internal enum EGameState : byte
        {
            NotInitialized,
            BeforeStart,
            Playing,
            Win,
            GameOver
        }

        /// <summary>
        /// 一次挖掘动作的结果
        /// </summary>
        internal enum EDigResult : byte
        {
            // 无效操作
            InvalidOperation,
            // 地块已挖开
            Open,
            // 地块为雷
            Mine,
            // 地块正常挖开，且为最小必挖地块
            MinimalBlock,
            // 地块正常挖开，不是最小必挖地块
            NormalBlock
        }

        private readonly List<Board> _boards;
        private Recorder _recorder;

        internal Recorder Recorder => _recorder;
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
                    CurBoard.SetUsed();
                }
                else if (EGameState.GameOver == value || EGameState.Win == value)
                {
                    _recorder.OnGameOver(value, CurBoard);
                }
                GameStateChanged?.Invoke(_state);
            }
        }

        internal EGameState State => _state;
        
        
        private int _randomSeed;
        private Random _rand;
        private DateTime _startTime;
        internal Action<int> BlockChanged;
        internal Action<EGameState> GameStateChanged;
        internal Action<int, int> GameProgressChanged;
        internal Action<int, int> BoardSizeChanged;
        internal Board CurBoard => _boards[0];
        internal int CompleteMinimalClick => _recorder?.OpendMinimalClickCnt ?? 0;
        internal DateTime StartTime => _startTime;

        internal Game() : this(8, 8, 1) {}

        internal Game(int width, int height, int mineCnt, int randomSeed = 0)
        {
            _randomSeed = randomSeed;
            _rand = new Random(_randomSeed);
            _boards = new List<Board>(BoardQueueInitialSize);
            for (var i = 0; i < BoardQueueInitialSize; i++)
            {
                var board = new Board(width, height, mineCnt);
                _boards.Add(board);
            }
            _recorder = new Recorder();
            _state = EGameState.NotInitialized;
        }

        private void Init()
        {
            Debug.Log("Game.Init");
            foreach (var board in _boards)
            {
                board.Init(_rand);
                // Debug.Log(board);
            }
            _recorder.Init();
            BoardSizeChanged?.Invoke(CurBoard.Width, CurBoard.Height);
        }

        public void Clear()
        {
            _recorder.Clear();
        }

        internal void Ready2Go()
        {
            Init();
            state = EGameState.BeforeStart;
            GameProgressChanged?.Invoke(0, CurBoard.ThreeBV);
        }

        internal void Restart()
        {
            var tmp = CurBoard;
            _boards.RemoveAt(0);
            _boards.Add(tmp);
            foreach (var board in _boards.Where(board => board.Used))
            {
                board.Init(_rand);
            }
            _recorder.Init();
            state = EGameState.BeforeStart;
            GameProgressChanged?.Invoke(0, CurBoard.ThreeBV);
            // Restart(CurBoard.Width, CurBoard.Height, CurBoard.MineCnt);
        }

        // 载入一组盘面数据设为当前盘面
        internal void LoadBoard(int[] boardData)
        {
            CurBoard.LoadFromRawData(boardData);
            _recorder.Init();
            state = EGameState.BeforeStart;
            GameProgressChanged?.Invoke(0, CurBoard.ThreeBV);
        }

        internal void ChangeBoardConfig(int width, int height, int mineCnt)
        {
            Debug.Log($"Game.ChangeBoardConfig width: {width}, height: {height}, mineCnt: {mineCnt}, _state: {_state}");
            if (_state != EGameState.BeforeStart && _state != EGameState.Win && _state != EGameState.GameOver) return;
            if (width == CurBoard.Width && height == CurBoard.Height && mineCnt == CurBoard.MineCnt) return;
            Debug.Log("Regenerate boards");
            _boards.Clear();
            for (var i = 0; i < BoardQueueInitialSize; i++)
            {
                var board = new Board(width, height, mineCnt);
                board.Init(_rand);
                Debug.Log($"Create board instance, hash code: {board.GetHashCode()}");
                _boards.Add(board);
            }
            BoardSizeChanged?.Invoke(width, height);
        }

        /// <summary>
        /// 确保游戏开局第一个点为open area
        /// </summary>
        internal void EnsureGameOpen(int openIdx, out bool horizontalSwap, out bool verticalSwap)
        {
            if (EGameState.BeforeStart != _state)
            {
                // todo exception
                throw new InvalidOperationException("");
            }
            var mirroredBlockidx = openIdx;
            // var tryCnt = 10;
            var slowInitCnt = 0;
            horizontalSwap = false;
            verticalSwap = false;
            var i = 0;
            while (true)
            {
                i++;
                if (i > 99)
                {
                    Debug.Log("dead loop 1");
                    break;
                }
                var swapSuccess = true;
                // 检测点击地块是否是雷，如是则尝试镜像
                while (CurBoard.GetBlock(mirroredBlockidx) > 0)
                {
                    // 先尝试水平翻转，再尝试垂直翻转，最后尝试双翻转
                    // 0 / 0 -> 1 / 0
                    // 1 / 0 -> 0 / 1
                    // 0 / 1 -> 1 / 1
                    // 1 / 1 -> change board
                    if (!horizontalSwap)
                    {
                        // Debug.Log($"try {(verticalSwap ? '3' : '1')}");
                        horizontalSwap = true;
                    }
                    else if (!verticalSwap)
                    {
                        // Debug.Log("try 2");
                        horizontalSwap = false;
                        verticalSwap = true;
                    }
                    else
                    {
                        swapSuccess = false;
                        break;
                    }
                    mirroredBlockidx = BlockIdxMirrorTransform(openIdx, horizontalSwap, verticalSwap, CurBoard.Width, CurBoard.Height);
                }
                // Debug.Log($"End of inner loop, swapSuccess: {swapSuccess}");
                // loop
                if (swapSuccess) break;
                horizontalSwap = false;
                verticalSwap = false;
                mirroredBlockidx = openIdx;
                // 切换棋盘
                // Debug.Log("Change board");
                var tmp = CurBoard;
                tmp.SetUsed();
                _boards.RemoveAt(0);
                _boards.Add(tmp);
                if (CurBoard.Used)
                {
                    // Debug.Log("Slow init");
                    slowInitCnt++;
                    CurBoard.Init(_rand);
                    // Debug.Log(CurBoard);
                    if (slowInitCnt > 99)
                    {
                        Debug.Log("dead loop");
                        break;
                    }
                }
            }
            Debug.Log($"Slow init cnt: {slowInitCnt}");
        }

        /// <summary>
        /// 挖开一个地块
        /// </summary>
        /// <param name="blockIdx">地块唯一id</param>
        /// <returns>挖掘结果</returns>
        internal EDigResult Dig(int blockIdx)
        {
            if (EGameState.BeforeStart != _state && EGameState.Playing != _state) return EDigResult.InvalidOperation;
            if (EGameState.BeforeStart == _state) state = EGameState.Playing;
            Debug.Log($"Game.Dig {blockIdx}'s block");
            // Debug.Log(CurBoard);
            var blockValue = CurBoard.GetBlock(blockIdx);
            Debug.Log($"Game.Dig, blockValue: {blockValue}");
            if ((int)Board.EBlockType.Mine != blockValue)
            {
                var blockOpened = _recorder.IsOpened(blockValue > 0 ? blockIdx : blockValue);
                if (blockOpened) return EDigResult.InvalidOperation;
                if (Open(blockIdx, out var changedBlockIdx))
                {
                    var isMinimalBlock = CurBoard.IsMinimalBlock(changedBlockIdx);
                    // 记录
                    if (isMinimalBlock)
                    {
                        Debug.Log($"{changedBlockIdx} is minimal block");
                        _recorder.SetOpenedMinimal(changedBlockIdx);
                    }
                    else
                    {
                        Debug.Log($"{changedBlockIdx} is NOT minimal block");
                        _recorder.SetOpened(changedBlockIdx);
                    }
                    GameProgressChanged?.Invoke(_recorder.OpendMinimalClickCnt, CurBoard.ThreeBV);
                    if (_recorder.OpendMinimalClickCnt == CurBoard.ThreeBV)
                    {
                        Debug.Log($"Win, 3bv: {CurBoard.ThreeBV}");
                        state = EGameState.Win;
                    }

                    // 记录连续空白方块周边一圈联动打开的方块的打开状态
                    if (changedBlockIdx < 0)
                    {
                        var blockList = CurBoard.GetAreaBlockList(changedBlockIdx);
                        if (null == blockList)
                        {
                            Debug.LogError($"Game.Dig, get area block list {changedBlockIdx} failed, can't find area");
                        }
                        else
                        {
                            foreach (var idx in blockList)
                            {
                                // todo exception
                                if (0 > idx || idx > CurBoard.BlockCnt - 1) throw new ArgumentOutOfRangeException("");
                                BlockChanged?.Invoke(idx);
                                _recorder.SetOpened(idx);
                            }
                        }
                    }
                    else
                    {
                        // 更新view
                        BlockChanged?.Invoke(changedBlockIdx);
                    }
                    _recorder.SavePlayBackAction(blockIdx);
                    return isMinimalBlock ? EDigResult.MinimalBlock : EDigResult.NormalBlock;
                }
                return EDigResult.InvalidOperation;
            }
            else
            {
                _recorder.SavePlayBackAction(blockIdx);
                state = EGameState.GameOver;
                Debug.Log("Game Over");
                return EDigResult.Mine;
            }
        }

        internal int BlockIdxMirrorTransform(int blockIdx, bool horizontalSwap, bool verticalSwap, int width, int height)
        {
            if (width < 1 || height < 1 || blockIdx < 0 || blockIdx >= width * height)
            {
                // todo exception
                throw new ArgumentOutOfRangeException("");
            }
            var y = blockIdx / width;
            var x = blockIdx - y * width;
            x = horizontalSwap ? width - x - 1 : x;
            y = verticalSwap ? height - y - 1 : y;
            return y * width + x;
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
            // Debug.Log($"Open blockIdx: {blockIdx}");
            if (0 > blockIdx || CurBoard.BlockCnt <= blockIdx)
            {
                Debug.LogError("todo err");
                return false;
            }

            var blockValue = CurBoard.GetBlock(blockIdx);
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
            return CurBoard?.ToString() ?? "Empty Game";
        }
    }
}
