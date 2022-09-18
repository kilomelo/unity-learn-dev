
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Random = System.Random;

namespace Kilomelo.minesweeper.Runtime
{
    internal class Board
    {
        internal enum EBlockType : byte
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
        
        private int[] _data;
        private int _width;
        private int _height;
        private int _mineCnt;
        private int _3bv;
        /// <summary>
        /// 同一空白连通区域地块列表缓存
        /// key: area idx or block idx, value: list of blockIdx or null(when single block)
        /// </summary>
        private Dictionary<int, List<int>> _3bvBlockListDic;

        public int ThreeBV => _3bv;
        public int Width => _width;
        public int Height => _height;
        public int MinCnt => _mineCnt;
        public int BlockCnt => _width * _height;

        internal int GetBlock(int idx)
        {
            if (idx < 0 || idx >= BlockCnt)
                throw new Exception("block idx out of range.");
            return _data[idx];
        }

        internal Board(int width, int height, int mineCnt)
        {
            if (mineCnt > width * height)
                throw new Exception("mine count out of range.");
            _width = width;
            _height = height;
            _mineCnt = mineCnt;
        }

        internal void Init(Random rand)
        {
            var data = new int[_width * _height];
            for (var i = 0; i < _mineCnt; i++)
            {
                data[i] = (int)EBlockType.Mine;
            }

            rand.Shuffle(data);
            
            // 标记周围地雷数量
            for (var i = 0; i < data.Length; i++)
            {
                if ((int) EBlockType.Mine == data[i]) continue;
                var neighborBlocks = NeighberIdxList(i);
                data[i] = neighborBlocks.Count(neighbor => neighbor >= 0 && data[neighbor] == (int) EBlockType.Mine);
            }
            // 标记连续空白
            _3bvBlockListDic = new Dictionary<int, List<int>>();
            var areaIdx = -1;
            List<int> listOfBlockIdx = new List<int>();
            for (var i = 0; i < data.Length; i++)
            {
                var areaSize = FillBlank(data, i, areaIdx, listOfBlockIdx);
                if (areaSize > 0)
                {
                    Debug.Log($"Area {areaIdx} has {areaSize} blocks");
                    _3bvBlockListDic[areaIdx] = listOfBlockIdx;
                    areaIdx--;
                    listOfBlockIdx = new List<int>();
                }
                // todo exception
                if (int.MinValue == areaIdx) throw new IndexOutOfRangeException("");
            }
            // 统计3bv
            _3bv = -areaIdx - 1;
            for (var i = 0; i < data.Length; i++)
            {
                if ((int) EBlockType.Mine == data[i] || data[i] < 0) continue;
                var neighborBlocks = NeighberIdxList(i);
                if (neighborBlocks.Any(neighbor => neighbor  >= 0 && data[neighbor] < 0)) continue;
                _3bvBlockListDic[i] = null;
                _3bv++;
            }
            Debug.Log($"3bv: {_3bv}, count of _3bvBlockListDic: {_3bvBlockListDic.Count}");

            _data = data;
        }

        internal List<int> GetAreaBlockList(int areaIdx)
        {
            return _3bvBlockListDic.TryGetValue(areaIdx, out var list) ? list : null;
        }

        internal bool IsMinimalBlock(int blockIdx)
        {
            return _3bvBlockListDic.ContainsKey(blockIdx);
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

        /// <summary>
        /// 填充空白为连通区域编号
        /// </summary>
        /// <param name="data"></param>
        /// <param name="seedIdx"></param>
        /// <param name="areaIdx"></param>
        /// <param name="blockList"></param>
        /// <param name="depth"></param>
        /// <returns>返回连通区域尺寸</returns>
        private int FillBlank(IList<int> data, int seedIdx, int areaIdx, List<int> blockList, int depth = 0)
        {
            if (depth > 999 * 999) return 0;
            if (0 > seedIdx || data.Count - 1 < seedIdx) return 0;
            if ((int) EBlockType.Mine == data[seedIdx] || 0 > data[seedIdx]) return 0;
            if (0 < data[seedIdx])
            {
                if (0 == depth) return 0;
                blockList.Add(seedIdx);
                return 1;
            }
            data[seedIdx] = areaIdx;
            blockList.Add(seedIdx);
            var areaSize = 1;
            // todo optimize
            areaSize += FillBlank(data, UpIdx(seedIdx), areaIdx, blockList, depth + 1);
            areaSize += FillBlank(data, DownIdx(seedIdx), areaIdx, blockList, depth + 1);
            areaSize += FillBlank(data, LeftIdx(seedIdx), areaIdx, blockList, depth + 1);
            areaSize += FillBlank(data, RightIdx(seedIdx), areaIdx, blockList, depth + 1);
            areaSize += FillBlank(data, LeftUpIdx(seedIdx), areaIdx, blockList, depth + 1);
            areaSize += FillBlank(data, LeftDownIdx(seedIdx), areaIdx, blockList, depth + 1);
            areaSize += FillBlank(data, RightUpIdx(seedIdx), areaIdx, blockList, depth + 1);
            areaSize += FillBlank(data, RightDownIdx(seedIdx), areaIdx, blockList, depth + 1);
            return areaSize;
        }

        private readonly List<int> _cacheList8 = new List<int>(8);
        private string Data2String(int[] data, int width)
        {
            var sb = new StringBuilder();
            var i = 0;
            foreach (var block in data)
            {
                i++;
                sb.Append(block);
                if (i % width == 0) sb.Append('\n');
            }

            return sb.ToString();
        }
        public override string ToString()
        {
            return Data2String(_data, _width);
        }
    }
}