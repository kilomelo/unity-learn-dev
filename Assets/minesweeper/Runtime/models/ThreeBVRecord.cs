using System;
using System.IO;
using UnityEngine;

namespace Kilomelo.minesweeper.Runtime
{
    internal class ThreeBVRecord : BestRecord
    {
        public ThreeBVRecord(int boardWidth, int boardHeight) : base(boardWidth, boardHeight)
        {
        }

        protected override string GetRecordSaveFileName()
        {
            return Path.Combine(Application.persistentDataPath, $"BestRecord_3bv_{_boardWidth}_{_boardHeight}.txt");
        }

        // 比较3bv/s的大小，数值大的优
        protected override bool CompareRecord(RecordData.SingleRecord rec1, RecordData.SingleRecord rec2)
        {
            return (float)rec1.ThreeBV / rec1.FinishTime > (float)rec2.ThreeBV /rec2.FinishTime;
        }

        protected override string ResultString(int idx)
        {
            return $"历史 3bv/s记录: 第 {idx + 1}";
        }
    }

    internal class ThreeBVRecordRecent : BestRecordRecent
    {
        public ThreeBVRecordRecent(int boardWidth, int boardHeight, int gameUIDLimit) : base(boardWidth, boardHeight, gameUIDLimit)
        {
        }

        protected override string GetRecordSaveFileName()
        {
            return Path.Combine(Application.persistentDataPath, $"BestRecord_3bv_recent{_gameUIDLimit}_{_boardWidth}_{_boardHeight}.txt");
        }

        // 比较3bv/s的大小，数值大的优
        protected override bool CompareRecord(RecordData.SingleRecord rec1, RecordData.SingleRecord rec2)
        {
            return (float)rec1.ThreeBV / rec1.FinishTime > (float)rec2.ThreeBV /rec2.FinishTime;
        }

        protected override string ResultString(int idx)
        {
            return $"近{_gameUIDLimit}局 3bv/s记录: 第 {idx + 1}";
        }
    }
}