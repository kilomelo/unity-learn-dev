using System.Collections.Generic;

namespace Kilomelo.minesweeper.Runtime
{
    public class Recorder
    {
        private Dictionary<int, int> _openedBlocks = new Dictionary<int, int>();

        internal int SetOpened(int blockIdx)
        {
            _openedBlocks[blockIdx] = blockIdx;
            return _openedBlocks.Count;
        }

        internal bool IsOpened(int blockIdx)
        {
            return _openedBlocks.ContainsKey(blockIdx);
        }

        /// <summary>
        /// 检查是否opened，如果否，则记录open
        /// </summary>
        /// <param name="blockIdx"></param>
        /// <returns>是否open</returns>
        // internal bool CheckAndOpen(int blockIdx)
        // {
        //     if (_openedBlocks.ContainsKey(blockIdx)) return true;
        //     SetOpened(blockIdx);
        //     return false;
        // }
    }
}
