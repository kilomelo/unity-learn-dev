using System;
using System.IO;
using UnityEngine;

namespace Kilomelo.minesweeper.Runtime
{
    internal class TimeRecord : BestRecord
    {
        public TimeRecord(int boardWidth, int boardHeight) : base(boardWidth, boardHeight)
        {
        }

        protected override string GetRecordSaveFileName()
        {
            return Path.Combine(Application.persistentDataPath, $"BestRecord_time_{_boardWidth}_{_boardHeight}.txt");
        }

        // 比较用时的大小，数值小的优
        protected override bool CompareRecord(RecordData.SingleRecord rec1, RecordData.SingleRecord rec2)
        {
            return rec1.FinishTime < rec2.FinishTime;
        }

        protected override string ResultString(int idx)
        {
            return $"历史 时间记录: 第 {idx + 1}";
        }
    }

    internal class TimeRecordRecent : BestRecordRecent
    {
        public TimeRecordRecent(int boardWidth, int boardHeight, int gameUIDLimit) : base(boardWidth, boardHeight, gameUIDLimit)
        {
        }

        protected override string GetRecordSaveFileName()
        {
            return Path.Combine(Application.persistentDataPath, $"BestRecord_time_recent{_gameUIDLimit}_{_boardWidth}_{_boardHeight}.txt");
        }

        // 比较用时的大小，数值小的优
        protected override bool CompareRecord(RecordData.SingleRecord rec1, RecordData.SingleRecord rec2)
        {
            return rec1.FinishTime < rec2.FinishTime;
        }

        protected override string ResultString(int idx)
        {
            return $"近{_gameUIDLimit}局 时间记录: 第 {idx + 1}";
        }
    }
}