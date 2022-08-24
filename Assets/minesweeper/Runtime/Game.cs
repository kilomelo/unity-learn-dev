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

        private enum EBlockType : byte
        {
            Safe0 = 0,
            Safe1 = 1,
            Safe2 = 2,
            Safe3 = 3,
            Safe4 = 4,
            Safe5 = 5,
            Safe6 = 6,
            Safe7 = 7,
            Safe8 = 8,
            Mine = 9
        }

        private EGameState _state;
        
        private byte[] _data;
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
            var data = new byte[_width * _height];
            for (var i = 0; i < _mineCnt; i++)
            {
                data[i] = (byte)EBlockType.Mine;
            }

            _rand.Shuffle(data);
            for (var i = 0; i < data.Length; i++)
            {
                if ((byte) EBlockType.Mine == data[i]) continue;
                var neighborBlocks = NeighberIdxList(i);
                var sb = new StringBuilder();
                foreach (var b in neighborBlocks)
                {
                    sb.Append(b);
                    sb.Append('/');
                }
                Debug.Log($"block {i}, neighber: {sb}");
                // byte cnt = 0;
                // foreach (var neighbor in neighborBlocks)
                // {
                //     Debug.Log($"neighber: {neighbor}");
                //     if (neighbor >= 0 && data[neighbor] == (byte) EBlockType.Mine)
                //     {
                //         cnt++;
                //     }
                // }
                //
                // data[i] = cnt;
                data[i] = (byte)neighborBlocks.Count(neighbor => neighbor >= 0 && data[neighbor] == (byte) EBlockType.Mine);
            }

            _data = data;
        }
        internal void Init(int randSeed)
        {
            _randomSeed = randSeed;
            _rand = new Random(_randomSeed);
            Init();
        }

        private int UpIdx(int idx)
        {
            if (idx < 0 || idx >= _width * _height) return -1;
            if (idx < _width) return -1;
            return idx - _width;
        }

        private int DownIdx(int idx)
        {
            if (idx < 0 || idx >= _width * _height) return -1;
            if (idx >= _width * _height - _width) return -1;
            return idx + _width;
        }

        private int LeftIdx(int idx)
        {
            if (idx < 0 || idx >= _width * _height) return -1;
            if (idx % _width == 0) return -1;
            return idx - 1;
        }
        
        private int RightIdx(int idx)
        {
            if (idx < 0 || idx >= _width * _height) return -1;
            if ((idx + 1) % _width == 0) return -1;
            return idx + 1;
        }

        private int LeftUpIdx(int idx)
        {
            return LeftIdx(UpIdx(idx));
        }
        
        private int RightUpIdx(int idx)
        {
            return RightIdx(UpIdx(idx));
        }
        
        private int LeftDownIdx(int idx)
        {
            return LeftIdx(DownIdx(idx));
        }
        
        private int RightDownIdx(int idx)
        {
            return RightIdx(DownIdx(idx));
        }

        /// <summary>
        /// 返回序号为idx的地块的周围8个地块
        /// 返回对象为复用对象，不能持久化存储
        /// </summary>
        /// <param name="idx"></param>
        /// <returns></returns>
        private List<int> NeighberIdxList(int idx)
        {
            _cacheList8.Clear();
            _cacheList8.Add(UpIdx(idx));
            _cacheList8.Add(DownIdx(idx));
            _cacheList8.Add(LeftIdx(idx));
            _cacheList8.Add(RightIdx(idx));
            _cacheList8.Add(LeftUpIdx(idx));
            _cacheList8.Add(RightUpIdx(idx));
            _cacheList8.Add(LeftDownIdx(idx));
            _cacheList8.Add(RightDownIdx(idx));
            return _cacheList8;
        }

        private List<int> _cacheList8 = new List<int>(8);
        public override string ToString()
        {
            var sb = new StringBuilder();
            var i = 0;
            foreach (var block in _data)
            {
                i++;
                sb.Append(block);
                if (i % _width == 0) sb.Append('\n');
            }

            return sb.ToString();
        }
        
        
    }
}
