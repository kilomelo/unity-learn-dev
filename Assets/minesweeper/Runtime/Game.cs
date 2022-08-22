using System;

namespace Kilomelo.minesweeper.Runtime
{
    internal class Game
    {
        private enum EGameState
        {
            BeforeStart,
            Playing,
            Succeed,
            Failed
        }

        private enum EBlockState
        {
            Unknown,
            Open,
            Triggered
        }

        private EGameState _state;
        
        private bool[] _data;
        private int _width;
        private int _height;
        private int _mineCnt;
        private int _randomSeed;
        private Random _rand;

        internal Game()
        {
            _width = 8;
            _height = 8;
            _mineCnt = 10;
            _randomSeed = 0;
            _rand = new Random(_randomSeed);
            if (_mineCnt > _width * _height)
                throw new Exception("mine count out of range.");

            Init();
            _state = EGameState.BeforeStart;
        }
        internal void Init()
        {
            var data = new bool[_width * _height];
            for (var i = 0; i < _mineCnt; i++)
            {
                data[i] = true;
            }
            _rand.Shuffle(_data);
        }
    }
}
