using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

namespace Kilomelo.minesweeper.Runtime
{
    internal abstract class BestRecord
    {
        internal class RecordData
        {
            public int BoardWidth;
            public int BoardHeight;
            public int RecordLimit;
            
            public List<SingleRecord> Records;
            public RecordData(int boardWidth, int boardHeight)
            {
                BoardWidth = boardWidth;
                BoardHeight = boardHeight;
                RecordLimit = 10;
                Records = new List<SingleRecord>();
            }
            internal class SingleRecord
            {
                public int FinishTime;
                public string Date;
                public int ThreeBV;
                public int[] BoardData;
                public string PlaybackData;
                public long GameUID;

                public string DisplayString()
                {
                    return $"时间: {FinishTime * 0.001f:###.##} s 3BV: {ThreeBV:###}, 3BV/S: {ThreeBV * 1000f / FinishTime:##.##}, Date: {Date}";
                }
            }

            public string RecordListString()
            {
                if (Records.Count == 0) return "无记录";
                var sb = new StringBuilder();
                for (var i = 0; i < Records.Count; i++)
                {
                    sb.Append($"{i + 1:##} {Records[i].DisplayString()}");
                    if (i != Records.Count - 1) sb.Append("\n");
                }
                return sb.ToString();
            }
        }

        protected int _boardWidth;
        protected int _boardHeight;
        internal BestRecord(int boardWidth, int boardHeight)
        {
            _boardWidth = boardWidth;
            _boardHeight = boardHeight;
        }
        protected virtual string GetRecordSaveFileName()
        {
            return Path.Combine(Application.persistentDataPath, $"BestRecord_{_boardWidth}_{_boardHeight}.txt");
        }

        public virtual string SubmitRecord(long gameUID, int finishTime, string playbackData, Board board)
        {
            var result = string.Empty;
            Debug.Log($"BestRecord.SubmitRecord, finishTime: {finishTime}, playbackData: {playbackData}");
            var saveFilePath = GetRecordSaveFileName();
            using var fs = new FileStream(saveFilePath, FileMode.OpenOrCreate);
            using var sr = new StreamReader(fs);
            var existData = sr.ReadToEnd();
            Debug.Log($"BestRecord.SubmitRecord, existData: {existData}");
            RecordData data;
            if (string.IsNullOrEmpty(existData))
            {
                Debug.Log($"BestRecord.SubmitRecord, Create record data");
                data = new RecordData(_boardWidth, _boardHeight);
            }
            else
            {
                data = JsonConvert.DeserializeObject<RecordData>(existData);
                Debug.Log($"BestRecord.SubmitRecord, Load record data, record cnt: {data.Records.Count}");
                PostLoadRecordData(ref data);
            }
            sr.Dispose();
            Debug.Log($"data:\n{JsonConvert.SerializeObject(data)}");
            var newSingleRecord = new RecordData.SingleRecord
            {
                FinishTime = finishTime,
                PlaybackData = playbackData,
                Date = DateTime.Now.ToString("yyMMddHHmm"),
                ThreeBV = board.ThreeBV,
                BoardData = board.Data,
                GameUID = gameUID
            };
            if (data.Records.Count == 0)
            {
                data.Records.Add(newSingleRecord);
                result = ResultString(0);
            }
            else
            {
                var i = data.Records.Count - 1;
                for (; i >= 0 ; i--)
                {
                    Debug.Log($"data.Records[i].Time: {data.Records[i]}, newRecordTime: {finishTime}");
                    if (CompareRecord(data.Records[i], newSingleRecord))
                    {
                        break;
                    }
                }
                if (i < data.RecordLimit - 1)
                {
                    var insertPos = i + 1;
                    result = ResultString(insertPos);
                    Debug.Log($"BestRecord.SubmitRecord, insert to {insertPos}'s pos");

                    data.Records.Insert(insertPos, newSingleRecord);
                    while(data.Records.Count > data.RecordLimit)
                    {
                        data.Records.RemoveAt(data.Records.Count - 1);
                    }
                }
                else
                {
                    Debug.Log($"BestRecord.SubmitRecord, bad than {data.RecordLimit}'s record, ignore");
                    return string.Empty;
                }
            }
            var newRecordString = JsonConvert.SerializeObject(data, Formatting.Indented);
                    Debug.Log($"BestRecord.SubmitRecord, newRecordString: {newRecordString}");
                    using var fs1 = new FileStream(saveFilePath, FileMode.Open);
                    fs1.SetLength(0);
                    using var fr = new StreamWriter(fs1);
                    fr.Write(newRecordString);
                    fr.Dispose();
            return result;
        }

        // public string GetDisplayRecordListString()
        // {
        //     var saveFilePath = GetRecordSaveFileName();
        //     using var fs = new FileStream(saveFilePath, FileMode.OpenOrCreate);
        //     using var sr = new StreamReader(fs);
        //     var existData = sr.ReadToEnd();
        //     sr.Dispose();
        //     RecordData data;
        //     if (string.IsNullOrEmpty(existData))
        //     {
        //         data = new RecordData(boardWidth: _boardWidth, _boardHeight);
        //     }
        //     else
        //     {
        //         data = JsonConvert.DeserializeObject<RecordData>(existData);
        //     }
        //     return data.RecordListString();
        // }

        public int IterateRecords(Action<int, int, int, string, int[], string> func)
        {
            if (null == func)
            {
                Debug.LogError("BestRecord.IterateRecords, func is null");
                return 0;
            }
            var saveFilePath = GetRecordSaveFileName();
            using var fs = new FileStream(saveFilePath, FileMode.OpenOrCreate);
            using var sr = new StreamReader(fs);
            var existData = sr.ReadToEnd();
            sr.Dispose();
            RecordData data;
            if (string.IsNullOrEmpty(existData))
            {
                data = new RecordData(boardWidth: _boardWidth, _boardHeight);
            }
            else
            {
                data = JsonConvert.DeserializeObject<RecordData>(existData);
            }

            for (var i = 0; i < data.Records.Count; i++)
            {
                var rec = data.Records[i];
                func.Invoke(i, rec.FinishTime, rec.ThreeBV, rec.Date, rec.BoardData, rec.PlaybackData);
            }
            return data.Records.Count;
        }

        protected virtual void PostLoadRecordData(ref RecordData data)
        {

        }

        protected virtual bool CompareRecord(RecordData.SingleRecord rec1, RecordData.SingleRecord rec2)
        {
            return true;
        }

        protected virtual string ResultString(int idx)
        {
            return $"第{idx + 1}";
        }
    }

    // 最近N局游戏的记录
    internal abstract class BestRecordRecent : BestRecord
    {
        protected int _gameUIDLimit;
        protected long _submitGameUID;
        public BestRecordRecent(int boardWidth, int boardHeight, int gameUIDLimit) : base(boardWidth, boardHeight)
        {
            _gameUIDLimit = gameUIDLimit;
        }

        public override string SubmitRecord(global::System.Int64 gameUID, global::System.Int32 finishTime, global::System.String playbackData, Board board)
        {
            _submitGameUID = gameUID;
            return base.SubmitRecord(gameUID, finishTime, playbackData, board);
        }

        protected override void PostLoadRecordData(ref RecordData data)
        {
            base.PostLoadRecordData(ref data);
            data.Records.RemoveAll(x => x.GameUID <= _submitGameUID - _gameUIDLimit);
        }

    }
}