using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Kilomelo.minesweeper.Runtime
{
    public class Recorder
    {
        private Dictionary<int, int> _openedBlocks = new Dictionary<int, int>();
        private Dictionary<int, int> _openedMinimalClickBlocks = new Dictionary<int, int>();
        private string _trainingDatasetSavedDir {
            get {
                return Path.Combine(Application.persistentDataPath, "SavedTrainingDataset");
            }
        }
        // 初始化实例时的日期字符串
        private string _cachedInitData;

        internal int OpendMinimalClickCnt => _openedMinimalClickBlocks.Count;

        // 每次开启新一局时会调用
        internal void Init()
        {
            Debug.Log($"Recorder.Init");
            _openedBlocks.Clear();
            _openedMinimalClickBlocks.Clear();
            _cachedInitData = System.DateTime.Now.ToString("yyyyMMdd");
            Debug.Log($"Recorder.Init, _trainingDatasetSavedDir: {_trainingDatasetSavedDir}, _cachedInitData: {_cachedInitData}");
        }

        internal void OnGameOver()
        {
            SaveOpenedFiles(false);

        }

        internal void Clear()
        {
            Debug.Log($"Recorder.Clear");
            SaveOpenedFiles(true);
            _openedFileStream.Clear();
        }

        internal void SetOpened(int blockIdx)
        {
            _openedBlocks[blockIdx] = blockIdx;
        }

        internal void SetOpenedMinimal(int minimalBlockAreaIdx)
        {
            _openedMinimalClickBlocks[minimalBlockAreaIdx] = minimalBlockAreaIdx;
        }

        internal bool IsOpened(int blockIdx)
        {
            return _openedBlocks.ContainsKey(blockIdx) || _openedMinimalClickBlocks.ContainsKey(blockIdx);
        }

        private Dictionary<string, StreamWriter> _openedFileStream = new Dictionary<string, StreamWriter>();
        private void SaveOpenedFiles(bool closeFile)
        {
            // 保存已打开的文件
            using var itr = _openedFileStream.GetEnumerator();
            while (itr.MoveNext())
            {
                var sr = itr.Current.Value;
                if (closeFile) sr.Close();
                else sr.Flush();
            }
        }
        /// <summary>
        /// 获取数据集存储文件路径
        /// 以日期为一级目录，以结果（雷/非minimal方块/minimal方块）为二级目录
        /// </summary>
        /// <param name="boardWidth">盘面宽，包含在文件名中</param>
        /// <param name="boardHeight">盘面高，包含在文件名中</param>
        /// <param name="areaSize">数据收集尺寸，包含在文件名中</param>
        /// <param name="resultValue">数据结果，以此值划分二级目录，并包含在文件名中</param>
        /// <returns></returns>
        internal string GetTrainingDatasetSavedFilePath(int boardWidth, int boardHeight, int areaSize, int resultValue)
        {
            return Path.Combine(_trainingDatasetSavedDir, _cachedInitData, resultValue.ToString(),  $"{_cachedInitData}_{boardWidth}_{boardHeight}_{areaSize}_{resultValue}.csv");
        }

        internal void SaveTrainingDatasetWholeBoard(int xIdx, int yIdx, int boardWidth, int boardHeight, string boardStatusString, int resultValue)
        {
            // Debug.Log($"Recorder.SaveTrainingDatasetWholeBoard, xIdx: {xIdx}, yIdx: {yIdx}, boardWidth: {boardWidth}, boardHeight: {boardHeight}, boardStatusString: [{boardStatusString}], resultValue: {resultValue}");
            var data = $"{boardWidth},{boardHeight},{xIdx},{yIdx},{boardStatusString},{resultValue}";
            var datasetSavedFileDir = GetTrainingDatasetSavedFilePath(boardWidth, boardHeight, 0, resultValue);
            // Debug.Log($"Recorder.SaveTrainingDatasetWholeBoard, data: [{data}], GetTrainingDatasetSavedFilePath: {datasetSavedFileDir}");
            if (!_openedFileStream.TryGetValue(datasetSavedFileDir, out var sr))
            {
                var directory = Path.GetDirectoryName(datasetSavedFileDir);
                if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
                var fs = new FileStream(datasetSavedFileDir, FileMode.Append);
                sr = new StreamWriter(fs);
                _openedFileStream[datasetSavedFileDir] = sr;
            }
            sr.Write(data);
        }
        internal void SaveTrainingDataset(int xIdx, int yIdx, int boardWidth, int boardHeight, string statusString, int resultValue, int areaSize)
        {
            // Debug.Log($"Recorder.SaveTrainingDataset, xIdx: {xIdx}, yIdx: {yIdx}, boardWidth: {boardWidth}, boardHeight: {boardHeight}, statusString: [{statusString}], resultValue: {resultValue}, areaSize: {areaSize}");
            var data = $"{boardWidth},{boardHeight},{xIdx},{yIdx},{statusString},{resultValue}";
            var datasetSavedFileDir = GetTrainingDatasetSavedFilePath(boardWidth, boardHeight, areaSize, resultValue);
            // Debug.Log($"Recorder.SaveTrainingDataset, data: [{data}], GetTrainingDatasetSavedFilePath: {datasetSavedFileDir}");
            if (!_openedFileStream.TryGetValue(datasetSavedFileDir, out var sr))
            {
                var directory = Path.GetDirectoryName(datasetSavedFileDir);
                if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
                var fs = new FileStream(datasetSavedFileDir, FileMode.Append);
                sr = new StreamWriter(fs);
                _openedFileStream[datasetSavedFileDir] = sr;
            }
            sr.Write(data);
        }
    }
}
