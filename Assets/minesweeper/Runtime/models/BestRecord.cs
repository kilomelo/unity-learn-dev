
using System.Collections.Generic;
using System.IO;
using Codice.Client.Common;
using Newtonsoft.Json;
using UnityEngine;

namespace Kilomelo.minesweeper.Runtime
{
    internal static class BestRecord
    {
        private static string GetRecordSaveFileName(int boardWidth, int boardHeight)
        {
            return Path.Combine(Application.persistentDataPath, $"BestRecord_{boardWidth}_{boardHeight}.txt");
        }
        internal class RecordData
        {
            public int BoardWidth;
            public int BoardHeight;
            public int RecordLimit;
            public List<SingleRecord> Records = new List<SingleRecord>();
            public RecordData(int boardWidth, int boardHeight)
            {
                BoardWidth = boardWidth;
                BoardHeight = boardHeight;
                RecordLimit = 10;
            }
            public class SingleRecord
            {
                public int FinishTime;
                public string PlaybackData;
            }
        }

        public static void SubmitRecord(int boardWidth, int boardHeight, int finishTime, string playbackData)
        {
            Debug.Log($"BestRecord.SubmitRecord, finishTime: {finishTime}, playbackData: {playbackData}");
            var saveFilePath = GetRecordSaveFileName(boardWidth, boardHeight);
            using var fs = new FileStream(saveFilePath, FileMode.OpenOrCreate);
            using var sr = new StreamReader(fs);
            var existData = sr.ReadToEnd();
            Debug.Log($"BestRecord.SubmitRecord, existData: {existData}");
            RecordData data;
            if (string.IsNullOrEmpty(existData))
            {
                Debug.Log($"BestRecord.SubmitRecord, Create record data");
                data = new RecordData(boardWidth, boardHeight);
            }
            else
            {
                Debug.Log($"BestRecord.SubmitRecord, Load record data");
                data = JsonUtility.FromJson<RecordData>(existData);
            }
            sr.Dispose();
            Debug.Log($"data:\n{JsonConvert.SerializeObject(data)}");

            // var insertPos = int.MaxValue;
            var i = 0;
            for (; i < data.Records.Count; i++)
            {
                if (data.Records[i].FinishTime > finishTime)
                {
                    // insertPos = i;
                    break;
                }
            }
            if (i < data.RecordLimit)
            {
                var newRecord = new RecordData.SingleRecord();
                newRecord.FinishTime = finishTime;
                newRecord.PlaybackData = playbackData;
                data.Records.Insert(i, newRecord);
                while(data.Records.Count > data.RecordLimit)
                {
                    data.Records.RemoveAt(data.Records.Count - 1);
                }
                var newRecordString = JsonConvert.SerializeObject(data);
                Debug.Log($"BestRecord.SubmitRecord, newRecordString: {newRecordString}");
                using var fs1 = new FileStream(saveFilePath, FileMode.Open);
                using var fr = new StreamWriter(fs1);
                fr.Write(newRecordString);
                fr.Dispose();
            }
            else
            {
                Debug.Log($"BestRecord.SubmitRecord, finish time bigger than {data.RecordLimit}'s record, ignore");
            }
        }
    }
}