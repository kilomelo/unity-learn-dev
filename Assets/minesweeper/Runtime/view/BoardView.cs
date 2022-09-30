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
        private Board _board;
        private int _lastPressedBlockIdx = -1;

        private int _lastHeight = 0;
        private int _lastWidth = 0;

        private EMirrorOption _mirror = EMirrorOption.None;
        private EBoardState _state = EBoardState.Invalid;
        
        private void Awake()
        {
            if (null == _blockTemplate || null == _scrollRect || null == _blockLayout || null == _numberColor)
            {
                // todo exception
                throw new Exception("");
            }
            _state = EBoardState.Valid;

            // BlockIdxMirrorTransform_UniTest();
        }

        private void Update()
        {
            if (EBoardState.ResetBlocksByMirrorState == _state)
            {
                Debug.Log($"Set board view, _mirror: {_mirror}");
                for (var i = 0; i < _blocks.Length; i++)
                {
                    var dataIdx = BlockIdxMirrorTransform(i, _mirror, _board.Width, _board.Height);
                    var blockType = _board.GetBlock(dataIdx);
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
            _board = _game.Board ?? throw new NullReferenceException("");
            _blockLayout.cellSize = Vector2.one * _scrollRect.viewport.rect.height / _board.Height;
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
                var viewIdx = BlockIdxMirrorTransform(changedBlockIdx, _mirror, _board.Width, _board.Height);
                _blocks[viewIdx].Open();
            }
            // 如果是区域
            else
            {
                // 根据区域信息刷新block
                var blockList = _board.GetAreaBlockList(changedBlockIdx);
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
                        var viewIdx = BlockIdxMirrorTransform(blockIdx, _mirror, _board.Width, _board.Height);
                        _blocks[viewIdx].Open();
                    }
                }
            }
        }

        internal void GameStateChanged(Game.EGameState gameState)
        {
            CheckValid();
            if (Game.EGameState.BeforeStart == gameState)
            {
                CodeStopwatch.Start();
                _state = EBoardState.RefreshBlocks;
                if (_lastHeight != _board.Height || _lastWidth != _board.Width)
                {
                    Clear();
                    InitBlocks();
                    _lastWidth = _board.Width;
                    _lastHeight = _board.Height;
                }
                else
                {
                    for (var i = 0; i < _board.BlockCnt; i++)
                    {
                        var blockView = _blocks[i];
                        var blockType = _board.GetBlock(i);
                        blockView.SetData(_game, i, blockType, this);
                    }
                }
                _state = EBoardState.WaitFirstClick;
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
            _blockLayout.constraintCount = _board.Width;
            _blocks = new BlockView[_board.BlockCnt];
            for (var i = 0; i < _board.BlockCnt; i++)
            {
                var blockInstance = GetBlockInstance();
                blockInstance.transform.SetParent(_blockLayout.transform);
                var blockView = blockInstance.GetComponent<BlockView>();
                // todo exception
                if (null == blockView) throw new MissingComponentException("");
                var blockType = _board.GetBlock(i);
                
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
            var dataIdx = BlockIdxMirrorTransform(blockIdx, _mirror, _board.Width, _board.Height);
            Debug.Log($"BoardView.Dig({blockIdx}), dataIdx: {dataIdx}");
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
                        var succeed = true;
                        var mirroredBlockidx = blockIdx;
                        var maxTryCnt = 10;
                        var tryCnt = 0;
                        while (true)
                        {
                            // 检测点击地块是否是雷，如是则尝试镜像
                            while (_game.Board.GetBlock(mirroredBlockidx) > 0)
                            {
                                if (EMirrorOption.Both == _mirror)
                                {
                                    // todo 切换board
                                    succeed = false;
                                    break;
                                }
                                _mirror = (EMirrorOption) (int) _mirror + 1;
                                mirroredBlockidx = BlockIdxMirrorTransform(blockIdx, _mirror, _board.Width, _board.Height);
                            }
                            // loop
                            if (succeed) break;
                            
                            break;
                            tryCnt++;
                            if (tryCnt > maxTryCnt) break;
                        }

                        // _state = succeed ? EBoardState.ResetBlocksByMirrorState : EBoardState.Clean;
                        _state = EBoardState.ResetBlocksByMirrorState;
                        Debug.Log($"succeed: {succeed}, mirror: {_mirror}");
                        if (!succeed) _mirror = EMirrorOption.None;
                        _blocks[blockIdx].SetBlockType(_board.GetBlock(BlockIdxMirrorTransform(blockIdx, _mirror, _board.Width, _board.Height)), this);

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
            if (xIdx < 0 || xIdx >= _board.Width || yIdx < 0 || yIdx >= _board.Height)
            {
                return false;
            }
            blockIdx = xIdx + yIdx * _board.Width;
            if (blockIdx >= _blocks.Length)
            {
                // todo exception
                throw new IndexOutOfRangeException();
            }
            return true;
        }

        private int BlockIdxMirrorTransform(int blockIdx, EMirrorOption mirror, int width, int height)
        {
            if (width < 1 || height < 1 || blockIdx < 0 || blockIdx >= width * height)
            {
                // todo exception
                throw new ArgumentOutOfRangeException("");
            }
            var mirrorHorizontal = (mirror & EMirrorOption.Horizontal) != 0;
            var mirrorVertical = (mirror & EMirrorOption.Vertical) != 0;
            var y = blockIdx / width;
            var x = blockIdx - y * width;
            x = mirrorHorizontal ? width - x - 1 : x;
            y = mirrorVertical ? height - y - 1 : y;
            return y * width + x;
        }

        // private void BlockIdxMirrorTransform_UniTest()
        // {
        //     var pass = true;
        //     var resut = BlockIdxMirrorTransform(0, EMirrorOption.Horizontal, 3, 2);
        //     if (resut != 2)
        //     {
        //         pass = false;
        //         Debug.Log("Case 0 failed.");
        //     }
        //     
        //     resut = BlockIdxMirrorTransform(4, EMirrorOption.Horizontal, 4, 6);
        //     if (resut != 7)
        //     {
        //         pass = false;
        //         Debug.Log("Case 1 failed.");
        //     }
        //     
        //     resut = BlockIdxMirrorTransform(3, EMirrorOption.Vertical, 4, 3);
        //     if (resut != 11)
        //     {
        //         pass = false;
        //         Debug.Log("Case 2 failed.");
        //     }
        //     
        //     resut = BlockIdxMirrorTransform(4, EMirrorOption.Vertical, 3, 2);
        //     if (resut != 1)
        //     {
        //         pass = false;
        //         Debug.Log("Case 3 failed.");
        //     }
        //     
        //     resut = BlockIdxMirrorTransform(0, EMirrorOption.Horizontal | EMirrorOption.Vertical, 2, 3);
        //     if (resut != 5)
        //     {
        //         pass = false;
        //         Debug.Log("Case 4 failed.");
        //     }
        //     
        //     resut = BlockIdxMirrorTransform(12, EMirrorOption.Horizontal | EMirrorOption.Vertical, 5, 5);
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