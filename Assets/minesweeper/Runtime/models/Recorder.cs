using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Kilomelo.minesweeper.Runtime
{
    public class Recorder
    {
        private Dictionary<int, int> _openedBlocks = new Dictionary<int, int>();
        private Dictionary<int, int> _openedMinimalClickBlocks = new Dictionary<int, int>();

        private bool _firstClick = false;
        private DateTime _gameStartTime;
        private List<Vector2Int> _playbackData = new List<Vector2Int>();
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
            _firstClick = true;
            _playbackData.Clear();
        }
        // 游戏结束都会调用，不管胜利与否
        internal void OnGameOver(Game.EGameState eGameState, int boardWidth, int boardHeight)
        {
            Debug.Log($"Recorder.OnGameOver, eGameState: {eGameState}");
            SaveOpenedFiles(false);
            if (eGameState != Game.EGameState.Win) return;
            var sb = new StringBuilder();
            Debug.Log($"Recorder.OnGameOver, _playbackData.Count: {_playbackData.Count}");
            var i = 0;
            var finishTime = int.MaxValue;
            foreach(var playbackClick in _playbackData)
            {
                Debug.Log($"({playbackClick.x}, {playbackClick.y})");
                sb.Append($"{playbackClick.x}, {playbackClick.y}");
                if (i != _playbackData.Count - 1) sb.Append(',');
                else finishTime = playbackClick.x;
                i++;
            }
            BestRecord.SubmitRecord(boardWidth, boardHeight, finishTime, sb.ToString());
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

        /// <summary>
        /// 保存回放数据
        /// </summary>
        /// <param name="blockIdx"></param>
        internal void SavePlayBackAction(int blockIdx)
        {
            Debug.Log($"SavePlayBackAction, blockIdx: {blockIdx}");
            if (_firstClick)
            {
                _gameStartTime = DateTime.Now;
                _firstClick = false;
                _playbackData.Add(new Vector2Int(0, blockIdx));
                return;
            }
            _playbackData.Add(new Vector2Int((int)(DateTime.Now - _gameStartTime).TotalMilliseconds, blockIdx));
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
        /// <summary>
        /// 保存用于训练的数据（全盘面）到本地文件
        /// dataset文件目录结构：
        /// yyyyMMdd/result(0/1/2)/yyyyMMdd_boardWidth_boardHeight_xIdx_yIdx_areaSize_result.csv
        /// </summary>
        /// <param name="xIdx"></param>
        /// <param name="yIdx"></param>
        /// <param name="boardWidth"></param>
        /// <param name="boardHeight"></param>
        /// <param name="boardStatusString"></param>
        /// <param name="resultValue"></param>
        internal void SaveTrainingDatasetWholeBoard(int xIdx, int yIdx, int boardWidth, int boardHeight, string boardStatusString, int resultValue)
        {
            // Debug.Log($"Recorder.SaveTrainingDatasetWholeBoard, xIdx: {xIdx}, yIdx: {yIdx}, boardWidth: {boardWidth}, boardHeight: {boardHeight}, boardStatusString: [{boardStatusString}], resultValue: {resultValue}");
            var data = $"{boardWidth},{boardHeight},{xIdx},{yIdx},{boardStatusString},{resultValue}\r\n";
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
        /// <summary>
        /// 保存用于训练的数据（以指定方块为中心的一定区域）到本地文件
        /// dataset文件目录结构：
        /// yyyyMMdd/result(0/1/2)/yyyyMMdd_boardWidth_boardHeight_xIdx_yIdx_areaSize_result.csv
        /// </summary>
        /// <param name="xIdx"></param>
        /// <param name="yIdx"></param>
        /// <param name="boardWidth"></param>
        /// <param name="boardHeight"></param>
        /// <param name="statusString"></param>
        /// <param name="resultValue"></param>
        /// <param name="areaSize"></param>
        internal void SaveTrainingDataset(int xIdx, int yIdx, int boardWidth, int boardHeight, string statusString, int resultValue, int areaSize)
        {
            // Debug.Log($"Recorder.SaveTrainingDataset, xIdx: {xIdx}, yIdx: {yIdx}, boardWidth: {boardWidth}, boardHeight: {boardHeight}, statusString: [{statusString}], resultValue: {resultValue}, areaSize: {areaSize}");
            var data = $"{boardWidth},{boardHeight},{xIdx},{yIdx},{statusString},{resultValue}\r\n";
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
