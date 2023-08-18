
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace Kilomelo.minesweeper.Runtime
{
    internal class BestRecord
    {
        internal class RecordData
        {
            public int BoardWidth;
            public int BoardHeight;
            public int RecordLimit;
            public SingleRecord[] Records;
            public RecordData(int boardWidth, int boardHeight)
            {
                BoardWidth = boardWidth;
                BoardHeight = boardHeight;
                RecordLimit = 3;
                Records = new SingleRecord[0];
            }
            public class SingleRecord
            {
                public int FinishTime;
                public string Date;
                public int ThreeBV;
                public int[] BoardData;
                public string PlaybackData;
                public long GameUID;
            }
        }

        private int _boardWidth;
        private int _boardHeight;
        internal BestRecord(int boardWidth, int boardHeight)
        {
            _boardWidth = boardWidth;
            _boardHeight = boardHeight;
        }
        protected virtual string GetRecordSaveFileName()
        {
            return Path.Combine(Application.persistentDataPath, $"BestRecord_{_boardWidth}_{_boardHeight}.txt");
        }

        public virtual void SubmitRecord(long gameUID, int finishTime, string playbackData, Board board)
        {
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
                Debug.Log($"BestRecord.SubmitRecord, Load record data");
                data = JsonUtility.FromJson<RecordData>(existData);
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
            var i = 0;
            var recordList = new List<RecordData.SingleRecord>(data.Records);
            for (; i < recordList.Count; i++)
            {
                if (CompareRecord(data.Records[i], newSingleRecord))
                {
                    break;
                }
            }
            if (i < data.RecordLimit)
            {
                
                recordList.Insert(i, newSingleRecord);
                while(recordList.Count > data.RecordLimit)
                {
                    recordList.RemoveAt(recordList.Count - 1);
                }
                data.Records = recordList.ToArray();
                var newRecordString = JsonConvert.SerializeObject(data, Formatting.Indented);
                Debug.Log($"BestRecord.SubmitRecord, newRecordString: {newRecordString}");
                using var fs1 = new FileStream(saveFilePath, FileMode.Open);
                using var fr = new StreamWriter(fs1);
                fr.Write(newRecordString);
                fr.Dispose();
            }
            else
            {
                Debug.Log($"BestRecord.SubmitRecord, bad than {data.RecordLimit}'s record, ignore");
            }
        }

        protected virtual bool CompareRecord(RecordData.SingleRecord rec1, RecordData.SingleRecord rec2)
        {
            return rec1.FinishTime < rec2.FinishTime;
        }
    }
}