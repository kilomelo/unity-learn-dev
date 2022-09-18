using System.Collections.Generic;

namespace Kilomelo.minesweeper.Runtime
{
    public class Recorder
    {
        private Dictionary<int, int> _openedBlocks = new Dictionary<int, int>();
        private Dictionary<int, int> _openedMinimalClickBlocks = new Dictionary<int, int>();

        internal int OpendMinimalClickCnt => _openedMinimalClickBlocks.Count;
        internal void Init()
        {
            _openedBlocks.Clear();
            _openedMinimalClickBlocks.Clear();
        }
        internal void SetOpened(int blockIdx)
        {
            _openedBlocks[blockIdx] = blockIdx;
        }

        internal void SetOpenedMinimal(int blockIdx)
        {
            _openedMinimalClickBlocks[blockIdx] = blockIdx;
        }

        internal bool IsOpened(int blockIdx)
        {
            return _openedBlocks.ContainsKey(blockIdx) || _openedMinimalClickBlocks.ContainsKey(blockIdx);
        }
    }
}
