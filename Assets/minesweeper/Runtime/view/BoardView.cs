using System;
using System.Collections.Generic;
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
        [Flags]
        private enum EMirrorOption : byte
        {
            None = 0,
            Horizontal = 1,
            Vertical = 2,
            Both = 3,
        }
        
        [SerializeField] private Color[] _numberColor;
        [SerializeField] private GameObject _blockTemplate;
        [SerializeField] private GridLayoutGroup _blockLayout;
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private AudioSource _audioSource;
        
        private float _blockSize;
        private BlockView[] _blocks;
        private Stack<BlockView> _blockCache = new Stack<BlockView>();
        private Game _game;
        private int _lastPressedBlockIdx = -1;

        private int _lastHeight = 0;
        private int _lastWidth = 0;
        // private bool _horizontalSwap = false;
        private bool _horizontalSwap
        {
            get { return __horizontalSwap; }
            set {
                Debug.Log($"Set horizontalSwap to {value}");
                __horizontalSwap = value;
            }
        }
        private bool __horizontalSwap = false;
        private bool _verticalSwap
        {
            get { return __verticalSwap; }
            set {
                Debug.Log($"Set verticalSwap to {value}");
                __verticalSwap = value;
            }
        }
        private bool __verticalSwap = false;
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
                Debug.Log($"Set board view, horizontalSwap: {_horizontalSwap}, verticalSwap: {_verticalSwap}");
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
            _blockLayout.cellSize = Vector2.one * _scrollRect.viewport.rect.height / _game.CurBoard.Height;
            _blockSize = _blockLayout.cellSize.x;

            _state = EBoardState.DataReady;
        }

        internal void BlockChanged(int changedBlockIdx)
        {
            Debug.Log($"BoardView.BlockChanged({changedBlockIdx})");
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
                // 根据区域信息刷新block
                var blockList = _game.CurBoard.GetAreaBlockList(changedBlockIdx);
                if (null == blockList)
                {
                    Debug.LogError($"Refresh area {changedBlockIdx} failed, can't find area");
                }
                else
                {
                    foreach (var blockIdx in blockList)
                    {
                        // todo exception
                        if (0 > blockIdx || blockIdx > _blocks.Length - 1) throw new ArgumentOutOfRangeException("");
                        var viewIdx = _game.BlockIdxMirrorTransform(blockIdx, _horizontalSwap, _verticalSwap, _game.CurBoard.Width, _game.CurBoard.Height);
                        _blocks[viewIdx].Open();
                    }
                }
            }
        }

        internal void GameStateChanged(Game.EGameState gameState)
        {
            Debug.Log("BoardView.GameStateChanged");
            CheckValid();
            if (Game.EGameState.BeforeStart == gameState)
            {
                CodeStopwatch.Start();
                _state = EBoardState.RefreshBlocks;
                if (_lastHeight != _game.CurBoard.Height || _lastWidth != _game.CurBoard.Width)
                {
                    Clear();
                    InitBlocks();
                    _lastWidth = _game.CurBoard.Width;
                    _lastHeight = _game.CurBoard.Height;
                }
                else
                {
                    for (var i = 0; i < _game.CurBoard.BlockCnt; i++)
                    {
                        var blockView = _blocks[i];
                        var blockType = _game.CurBoard.GetBlock(i);
                        blockView.SetData(_game, i, blockType, this);
                    }
                }
                _state = EBoardState.WaitFirstClick;
                _horizontalSwap = false;
                _verticalSwap = false;
                _scrollRect.normalizedPosition = new Vector2(0f, 1f);
                var time = CodeStopwatch.ElapsedMilliseconds();
                Debug.Log($"BoardView init time cost: {time}");
            }

            if (Game.EGameState.GameOver == gameState)
            {
                foreach (var block in _blocks)
                {
                    block.Open();
                }
            }
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
                var blockType = _game.CurBoard.GetBlock(i);
                
                blockView.SetData(_game, i, blockType, this);
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
            Debug.Log($"BoardView.Dig({blockIdx}), dataIdx: {dataIdx}, horizontalSwap: {_horizontalSwap}, verticalSwap: {_verticalSwap}");
            _game.Dig(dataIdx);
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
                        _game.EnsureGameOpen(blockIdx, out var _horizontalSwap, out var _verticalSwap);
                        
                        _state = EBoardState.ResetBlocksByMirrorState;
                        Debug.Log($"horizontalSwap: {_horizontalSwap}, verticalSwap: {_verticalSwap}");
                        _blocks[blockIdx].SetBlockType(_game.CurBoard.GetBlock(_game.BlockIdxMirrorTransform(blockIdx, _horizontalSwap, _verticalSwap, _game.CurBoard.Width, _game.CurBoard.Height)), this);
                    }
                    
                    _blocks[blockIdx].OnPointerUp();
#if !UNITY_EDITOR
                    Vibration.Vibrate(50);
#endif
                    _audioSource.Play();
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