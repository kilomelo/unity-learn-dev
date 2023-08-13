using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Kilomelo.minesweeper.Runtime
{
    [RequireComponent(typeof(RectTransform))]
    public class BoardView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerMoveHandler, IPointerExitHandler
    {
        private enum EBoardState : byte
        {
            Invalid = 0,
            Valid = 1,
            DataReady = 2,
            RefreshBlocks = 3,
            WaitFirstClick = 4,
            ResetBlocksByMirrorState = 5,
            Clean = 6,
        }

        // 布局规则，盘面宽度与view宽度适配还是盘面高度与view宽度适配
        private enum ELayoutRule : byte
        {
            FitWidth = 0,
            FitHeight = 1,
        }
        
        [SerializeField] private Color[] _numberColor;
        [SerializeField] private GameObject _blockTemplate;
        [SerializeField] private GridLayoutGroup _blockLayout;
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private AudioSource _audioSourceMininalBlockOpen;
        [SerializeField] private AudioSource _audioSourceNormalBlockOpen;
        
        private float _blockSize;
        private BlockView[] _blocks;
        // 复用的地块实例池
        private Stack<BlockView> _blockCache = new Stack<BlockView>();
        private Game _game;
        private int _lastPressedBlockIdx = -1;

        // private int _lastHeight = 0;
        // private int _lastWidth = 0;
        /// <summary>
        /// View旋转设置，为确保开局不踩雷View可能会再Data的基础上作出旋转
        /// </summary>
        private bool _horizontalSwap = false;
        private bool _verticalSwap = false;
        private EBoardState _state = EBoardState.Invalid;
        
        private void Awake()
        {
            if (null == _blockTemplate || null == _scrollRect || null == _blockLayout || null == _numberColor)
            {
                // todo exception
                throw new Exception("");
            }
            _state = EBoardState.Valid;

            // _game.BlockIdxMirrorTransform_UniTest();
        }

        private void Update()
        {
            if (EBoardState.ResetBlocksByMirrorState == _state)
            {
                // Debug.Log($"Set board view, horizontalSwap: {_horizontalSwap}, verticalSwap: {_verticalSwap}");
                for (var i = 0; i < _blocks.Length; i++)
                {
                    var dataIdx = _game.BlockIdxMirrorTransform(i, _horizontalSwap, _verticalSwap, _game.CurBoard.Width, _game.CurBoard.Height);
                    var blockType = _game.CurBoard.GetBlock(dataIdx);
                    // Debug.Log($"view idx: {i}, data idx: {dataIdx}, blockType: {blockType}");
                    _blocks[i].SetBlockType(blockType, this);
                }

                _state = EBoardState.Clean;
            }
        }

        private void CheckValid()
        {
            // todo exception
            if (EBoardState.Invalid == _state) throw new Exception("");
        }

        internal Color NumberColor(int num)
        {
            CheckValid();
            // todo exception
            if (num < 0 || num > _numberColor.Length - 1) throw new ArgumentOutOfRangeException("");
            return _numberColor[num];
        }

        private void Clear()
        {
            CheckValid();
            if (null != _blocks)
            {
                foreach (var block in _blocks)
                {
                    ReturnBlockInstance(block);
                }
                _blocks = null;
            }

            _state = EBoardState.DataReady;
        }

        internal void SetData(Game game)
        {
            CheckValid();
            // todo exception
            _game = game ?? throw new NullReferenceException("");
            if (null == _game.CurBoard) throw new NullReferenceException("");

            var layoutRule = _game.CurBoard.Width <= 8 ? ELayoutRule.FitWidth : ELayoutRule.FitHeight;
            Debug.Log($"BoardView.SetData, layoutRule: {layoutRule}");
            if (ELayoutRule.FitWidth == layoutRule) _blockLayout.cellSize = Vector2.one * _scrollRect.viewport.rect.width / _game.CurBoard.Width;
            else _blockLayout.cellSize = Vector2.one * _scrollRect.viewport.rect.height / _game.CurBoard.Height;

            _blockSize = _blockLayout.cellSize.x;
            
            _game.BlockChanged += BlockChanged;
            _game.GameStateChanged += GameStateChanged;
            _game.BoardSizeChanged += BoardSizeChanged;

            _state = EBoardState.DataReady;
        }

        private void BlockChanged(int changedBlockIdx)
        {
            // Debug.Log($"BoardView.BlockChanged({changedBlockIdx})");
            CheckValid();
            // 如果是单个地块
            if (changedBlockIdx >= 0)
            {
                // todo exception
                if (changedBlockIdx > _blocks.Length - 1) throw new ArgumentOutOfRangeException("");
                var viewIdx = _game.BlockIdxMirrorTransform(changedBlockIdx, _horizontalSwap, _verticalSwap, _game.CurBoard.Width, _game.CurBoard.Height);
                _blocks[viewIdx].Open();
            }
            // 如果是区域
            else
            {
                Debug.LogError($"BoardView.BlockChanged, invalid changedBlockIdx: {changedBlockIdx}");
                // // 根据区域信息刷新block
                // var blockList = _game.CurBoard.GetAreaBlockList(changedBlockIdx);
                // if (null == blockList)
                // {
                //     Debug.LogError($"Refresh area {changedBlockIdx} failed, can't find area");
                // }
                // else
                // {
                //     foreach (var blockIdx in blockList)
                //     {
                //         // todo exception
                //         if (0 > blockIdx || blockIdx > _blocks.Length - 1) throw new ArgumentOutOfRangeException("");
                //         var viewIdx = _game.BlockIdxMirrorTransform(blockIdx, _horizontalSwap, _verticalSwap, _game.CurBoard.Width, _game.CurBoard.Height);
                //         _blocks[viewIdx].Open();
                //     }
                // }
            }
        }

        private void GameStateChanged(Game.EGameState gameState)
        {
            Debug.Log($"BoardView.GameStateChanged, gameState: {gameState}");
            CheckValid();
            if (Game.EGameState.BeforeStart == gameState)
            {
                CodeStopwatch.Start();
                _state = EBoardState.RefreshBlocks;
                // Debug.Log($"_lastHeight: {_lastHeight}, _lastWidth: {_lastWidth}, _game.CurBoard.Height: {_game.CurBoard.Height}, _game.CurBoard.Width: {_game.CurBoard.Width}");
                
                for (var i = 0; i < _game.CurBoard.BlockCnt; i++)
                {
                    var blockView = _blocks[i];
                    blockView.SetData(i, this);
                }
                _state = EBoardState.WaitFirstClick;
                _horizontalSwap = false;
                _verticalSwap = false;
                _scrollRect.normalizedPosition = new Vector2(0f, 1f);
                // var time = CodeStopwatch.ElapsedMilliseconds();
                // Debug.Log($"BoardView init time cost: {time}");
            }

            if (Game.EGameState.GameOver == gameState)
            {
                foreach (var block in _blocks)
                {
                    block.Open();
                }
            }
        }

        private void BoardSizeChanged(int width, int height)
        {
            Debug.Log("BoardView.BoardSizeChanged");
            Clear();
            InitBlocks();
            var layoutRule = width <= 8 ? ELayoutRule.FitWidth : ELayoutRule.FitHeight;
            Debug.Log($"BoardView.BoardSizeChanged, layoutRule: {layoutRule}");
            if (ELayoutRule.FitWidth == layoutRule) _blockLayout.cellSize = Vector2.one * _scrollRect.viewport.rect.width / _game.CurBoard.Width;
            else _blockLayout.cellSize = Vector2.one * _scrollRect.viewport.rect.height / _game.CurBoard.Height;
            _blockSize = _blockLayout.cellSize.x;
            _scrollRect.normalizedPosition = new Vector2(0f, 1f);

            _state = EBoardState.RefreshBlocks;
            // Debug.Log($"_lastHeight: {_lastHeight}, _lastWidth: {_lastWidth}, _game.CurBoard.Height: {_game.CurBoard.Height}, _game.CurBoard.Width: {_game.CurBoard.Width}");
            
            for (var i = 0; i < _game.CurBoard.BlockCnt; i++)
            {
                var blockView = _blocks[i];
                blockView.SetData(i, this);
            }
            _state = EBoardState.WaitFirstClick;
            _horizontalSwap = false;
            _verticalSwap = false;
        }

        private void InitBlocks()
        {
            CheckValid();
            _blockLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            _blockLayout.constraintCount = _game.CurBoard.Width;
            _blocks = new BlockView[_game.CurBoard.BlockCnt];
            for (var i = 0; i < _game.CurBoard.BlockCnt; i++)
            {
                var blockInstance = GetBlockInstance();
                blockInstance.transform.SetParent(_blockLayout.transform);
                var blockView = blockInstance.GetComponent<BlockView>();
                // todo exception
                if (null == blockView) throw new MissingComponentException("");
                blockView.SetData(i, this);
                _blocks[i] = blockView;
            }
        }

        private GameObject GetBlockInstance()
        {
            // todo exception
            if (null == _blockTemplate) throw new NullReferenceException("");
            // return Instantiate(_blockTemplate);
            if (_blockCache.Count == 0) return Instantiate(_blockTemplate);
            var instance = _blockCache.Pop().gameObject;
            instance.SetActive(true);
            return instance;
        }

        private void ReturnBlockInstance(BlockView instance)
        {
            // todo exception
            if (null == instance) throw new NullReferenceException("");
            // Destroy(instance.gameObject);
            instance.transform.SetParent(null);
            instance.gameObject.SetActive(false);
            _blockCache.Push(instance);
        }

        internal void Dig(int blockIdx)
        {
            var dataIdx = _game.BlockIdxMirrorTransform(blockIdx, _horizontalSwap, _verticalSwap, _game.CurBoard.Width, _game.CurBoard.Height);
            Debug.Log($"BoardView.Dig({blockIdx}), dataIdx: {dataIdx}, horizontalSwap: {_horizontalSwap}, verticalSwap: {_verticalSwap}, _state: {_state}, [{this.GetHashCode()}]");

            // 收集点击前用户视角盘面状态，记录成数据集供训练
            var wholeBoardStatusBeforeDig = SerializeWholeBoardStatus();
            // 收集三个尺寸的局部数据
            var statusBeforeDigAreaSize1 =  SerializeBlockStatus(blockIdx, 1);
            Debug.Log("SerializeBlockStatus: " + statusBeforeDigAreaSize1);
            var statusBeforeDigAreaSize2 =  SerializeBlockStatus(blockIdx, 2);
            var statusBeforeDigAreaSize3 =  SerializeBlockStatus(blockIdx, 3);

            var digResult = _game.Dig(dataIdx);
            if (Game.EDigResult.Mine == digResult)
            {
                PlayVibration(200);
            }
            else if (Game.EDigResult.MinimalBlock == digResult)
            {
                PlayVibration(40);
                _audioSourceMininalBlockOpen.Play();
            }
            else if (Game.EDigResult.NormalBlock == digResult)
            {
                PlayVibration(40);
                _audioSourceNormalBlockOpen.Play();
            }
            // 第一次点击不记录数据，因为第一次点击前状态为全盘未知，无法作为训练数据
            if (EBoardState.Clean != _state)
            {
                return;
            }
            var yIdx = blockIdx / _game.CurBoard.Width;
            var xIdx = blockIdx - yIdx * _game.CurBoard.Width;
            
            Debug.Log($"BoardView.Dig, digResult: {digResult}");
            if (Game.EDigResult.Mine == digResult || Game.EDigResult.MinimalBlock == digResult || Game.EDigResult.NormalBlock == digResult)
            {
                var resultValue = 0;
                if ( Game.EDigResult.NormalBlock == digResult) resultValue = 1;
                if (Game.EDigResult.MinimalBlock == digResult) resultValue = 2;
                _game.Recorder.SaveTrainingDataset(xIdx, yIdx, _game.CurBoard.Width, _game.CurBoard.Height, statusBeforeDigAreaSize1, resultValue, 1);
                _game.Recorder.SaveTrainingDataset(xIdx, yIdx, _game.CurBoard.Width, _game.CurBoard.Height, statusBeforeDigAreaSize2, resultValue, 2);
                _game.Recorder.SaveTrainingDataset(xIdx, yIdx, _game.CurBoard.Width, _game.CurBoard.Height, statusBeforeDigAreaSize3, resultValue, 3);
                _game.Recorder.SaveTrainingDatasetWholeBoard(xIdx, yIdx, _game.CurBoard.Width, _game.CurBoard.Height, wholeBoardStatusBeforeDig, resultValue);
                
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            CheckValid();
            // Debug.Log($"BoardView.OnPointerDown, pos: {eventData.position}, scrollPos: {_scrollRect.normalizedPosition}");
            if (EventData2BlockIdx(eventData, out var blockIdx))
            {
                _lastPressedBlockIdx = blockIdx;
                _blocks[_lastPressedBlockIdx].OnPointerDown();
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            CheckValid();
            if (_lastPressedBlockIdx < 0) return;
            // Debug.Log($"BoardView.OnPointerUp, pos: {eventData.position}");
            if (EventData2BlockIdx(eventData, out var blockIdx))
            {
                if (_lastPressedBlockIdx == blockIdx)
                {
                    if (EBoardState.WaitFirstClick == _state)
                    {
                        Debug.Log($"EnsureGameOpen, blockIdx: {blockIdx}");
                        _game.EnsureGameOpen(blockIdx, out _horizontalSwap, out _verticalSwap);
                        
                        _state = EBoardState.ResetBlocksByMirrorState;
                        // Debug.Log($"horizontalSwap: {_horizontalSwap}, verticalSwap: {_verticalSwap}, [{this.GetHashCode()}]");
                        var mirroredBlockidx = _game.BlockIdxMirrorTransform(blockIdx, _horizontalSwap, _verticalSwap, _game.CurBoard.Width, _game.CurBoard.Height);
                        _blocks[blockIdx].SetBlockType(_game.CurBoard.GetBlock(mirroredBlockidx), this);
                    }
                    
                    _blocks[blockIdx].OnPointerUp();
                }
                else _blocks[_lastPressedBlockIdx].OnPointerDragExit();
            }
            _lastPressedBlockIdx = -1;
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            CheckValid();
            // Debug.Log($"BoardView.OnPointerMove, pos: {eventData.position}");
            if (_lastPressedBlockIdx < 0) return;
            if (eventData.dragging)
            {
                _blocks[_lastPressedBlockIdx].OnPointerDragExit();
                _lastPressedBlockIdx = -1;
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            CheckValid();
            if (_lastPressedBlockIdx < 0) return;
            _blocks[_lastPressedBlockIdx].OnPointerDragExit();
            _lastPressedBlockIdx = -1;
        }

        private bool EventData2BlockIdx(PointerEventData eventData, out int blockIdx)
        {
            blockIdx = 0;
            // Debug.Log($"eventData.position: {eventData.position}");
            var viewRect = _scrollRect.viewport.rect;
            var contentRect = _scrollRect.content.rect;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_scrollRect.viewport, eventData.position,
                eventData.pressEventCamera, out var localPos);
            // Debug.Log($"localPos: {localPos}");
            // Debug.Log($"NormalizedPosition: {_scrollRect.normalizedPosition}");
            var scrollPosX = _scrollRect.normalizedPosition.x * (contentRect.width - viewRect.width);
            var scrollPosY = (contentRect.height > viewRect.height ? 1 - _scrollRect.normalizedPosition.y : _scrollRect.normalizedPosition.y) * (contentRect.height - viewRect.height);
            // Debug.Log($"scrollPosX: {scrollPosX}, scrollPosY: {scrollPosY}");
            
            var xPosInBoard = localPos.x + scrollPosX;
            var yPosInBoard = -localPos.y + scrollPosY;
            // Debug.Log($"xPosInBoard: {xPosInBoard}, yPosInBoard: {yPosInBoard}");
            var xIdx = Mathf.FloorToInt(xPosInBoard / _blockSize);
            var yIdx = Mathf.FloorToInt(yPosInBoard / _blockSize);
            // Debug.Log($"xIdx: {xIdx}, yIdx: {yIdx}");
            if (xIdx < 0 || xIdx >= _game.CurBoard.Width || yIdx < 0 || yIdx >= _game.CurBoard.Height)
            {
                return false;
            }
            blockIdx = xIdx + yIdx * _game.CurBoard.Width;
            if (blockIdx >= _blocks.Length)
            {
                // todo exception
                throw new IndexOutOfRangeException();
            }
            return true;
        }

        private StringBuilder _sb = new StringBuilder();
        /// <summary>
        /// 序列化以指定block为中心，一定范围内的盘面状态
        /// 用于保存区域盘面数据，构建训练数据集
        /// </summary>
        /// <param name="blockIdx">位于中心的地块id</param>
        /// <param name="areaSize">大于0的值，为1时表示3*3大小区域，2时表示5*5大小区域</param>
        /// <returns>
        /// 以','分割的字符串，每一格数据为整数意义如下：
        /// 超过盘面边界：-2；
        /// 未打开，-1；
        /// 已打开为周围雷数0-8
        /// </returns>
        public string SerializeBlockStatus(int centerBlockIdx, int areaSize)
        {
            // ShaderVariantCollection blockIdx = xIdx + yIdx * _game.CurBoard.Width;
            var yIdx = centerBlockIdx / _game.CurBoard.Width;
            var xIdx = centerBlockIdx - yIdx * _game.CurBoard.Width;
            _sb.Clear();
            for (var y = yIdx - areaSize; y <= yIdx + areaSize; y++)
            {
                for (var x = xIdx - areaSize; x <= xIdx + areaSize; x++)
                {
                    if (x < 0 || x >= _game.CurBoard.Width || y < 0 || y >= _game.CurBoard.Height)
                    {
                        _sb.Append($"-2");
                    }
                    else
                    {
                        var blockIdx = x + y * _game.CurBoard.Width;
                        _sb.Append(_blocks[blockIdx].GetRuntimeSerializeNumber());
                    }
                    if (x != xIdx + areaSize || y != yIdx + areaSize) _sb.Append(',');
                }
            }

            return _sb.ToString();
        }

        /// <summary>
        /// 序列化整个盘面状态
        /// 用于保存盘面数据，构建训练数据集
        /// </summary>
        /// <returns>
        /// 以','分割的字符串，每一格数据为整数意义如下：
        /// 未打开，-1；
        /// 已打开为周围雷数0-8
        /// </returns>
        public string SerializeWholeBoardStatus ()
        {
            _sb.Clear();
            for (var i = 0; i < _blocks.Length; i++)
            {
                _sb.Append(_blocks[i].GetRuntimeSerializeNumber());
                if (i != _blocks.Length - 1) _sb.Append(',');
            }
            return _sb.ToString();
        }

        private void PlayVibration(int timeMiniseconds)
        {
#if UNITY_EDITOR
            Vibration.Vibrate(timeMiniseconds);
#endif
        }

        // private void _game.BlockIdxMirrorTransform_UniTest()
        // {
        //     var pass = true;
        //     var resut = _game.BlockIdxMirrorTransform(0, EMirrorOption.Horizontal, 3, 2);
        //     if (resut != 2)
        //     {
        //         pass = false;
        //         Debug.Log("Case 0 failed.");
        //     }
        //     
        //     resut = _game.BlockIdxMirrorTransform(4, EMirrorOption.Horizontal, 4, 6);
        //     if (resut != 7)
        //     {
        //         pass = false;
        //         Debug.Log("Case 1 failed.");
        //     }
        //     
        //     resut = _game.BlockIdxMirrorTransform(3, EMirrorOption.Vertical, 4, 3);
        //     if (resut != 11)
        //     {
        //         pass = false;
        //         Debug.Log("Case 2 failed.");
        //     }
        //     
        //     resut = _game.BlockIdxMirrorTransform(4, EMirrorOption.Vertical, 3, 2);
        //     if (resut != 1)
        //     {
        //         pass = false;
        //         Debug.Log("Case 3 failed.");
        //     }
        //     
        //     resut = _game.BlockIdxMirrorTransform(0, EMirrorOption.Horizontal | EMirrorOption.Vertical, 2, 3);
        //     if (resut != 5)
        //     {
        //         pass = false;
        //         Debug.Log("Case 4 failed.");
        //     }
        //     
        //     resut = _game.BlockIdxMirrorTransform(12, EMirrorOption.Horizontal | EMirrorOption.Vertical, 5, 5);
        //     if (resut != 12)
        //     {
        //         pass = false;
        //         Debug.Log("Case 5 failed.");
        //     }
        //     
        //     if (pass) Debug.Log("Pass");
        // }
    }
}