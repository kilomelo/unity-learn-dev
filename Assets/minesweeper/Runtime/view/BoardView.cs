using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Kilomelo.minesweeper.Runtime
{
    [RequireComponent(typeof(RectTransform))]
    public class BoardView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerMoveHandler
    {
        [SerializeField] private Color[] _numberColor;
        [SerializeField] private GameObject _blockTemplate;
        [SerializeField] private GridLayoutGroup _blockLayout;
        [SerializeField] private ScrollRect _scrollRect;
        private RectTransform _rectTrans;
        private float _blockSize;
        private BlockView[] _blocks;
        private Game _game;
        private Board _board;
        private int _lastPressedBlockIdx = -1;
        // /// <summary>
        // /// 同一空白连通区域地块列表缓存
        // /// </summary>
        // private Dictionary<int, List<int>> _blankAreaBlockListCache = new Dictionary<int, List<int>>();

        private bool _valid;

        private void Awake()
        {
            if (null == _blockTemplate || null == _scrollRect || null == _blockLayout || null == _numberColor)
            {
                // todo exception
                throw new Exception("");
            }
            _rectTrans = transform as RectTransform;
            // 只允许正方形格子
            if (Math.Abs(_blockLayout.cellSize.x - _blockLayout.cellSize.y) > 0.01f) throw new Exception();
            _blockSize = _blockLayout.cellSize.x;
            _valid = true;
        }

        private void CheckValid()
        {
            // todo exception
            if (!_valid) throw new Exception("");
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
                    Destroy(block);
                }
                _blocks = null;
            }
            // _blankAreaBlockListCache.Clear();
        }

        internal void SetData(Game game, Board board)
        {
            CheckValid();
            // todo exception
            _board = board ?? throw new NullReferenceException("");
            _game = game ?? throw new NullReferenceException("");
            Clear();
            InitBlocks();
        }

        internal void BlockChangedCallback(int changedBlockIdx)
        {
            // 如果是单个地块
            if (changedBlockIdx >= 0)
            {
                // todo exception
                if (changedBlockIdx > _blocks.Length - 1) throw new ArgumentOutOfRangeException("");
                _blocks[changedBlockIdx].Open();
            }
            // 如果是区域
            else
            {
                // todo 根据区域信息刷新block
                // if (_blankAreaBlockListCache.TryGetValue(changedBlockIdx, out var blockIdxList))
                // {
                //     foreach (var blockIdx in blockIdxList) _blocks[blockIdx].Open();
                // }
                // else
                // {
                //     Debug.LogError($"Update block view failed, area id: {changedBlockIdx}");
                // }
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
                // if (blockType < 0)
                // {
                //     List<int> listCache;
                //     if (!_blankAreaBlockListCache.TryGetValue(blockType, out listCache))
                //     {
                //         listCache = new List<int>();
                //         _blankAreaBlockListCache[blockType] = listCache;
                //     }
                //     listCache.Add(i);
                // }
                
                blockView.SetData(_game, i, blockType, this);
                _blocks[i] = blockView;
            }
            // break point todo 设置area信息
        }

        private GameObject GetBlockInstance()
        {
            // todo exception
            if (null == _blockTemplate) throw new NullReferenceException("");
            return Instantiate(_blockTemplate);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            // Debug.Log($"BoardView.OnPointerDown, pos: {eventData.position}, scrollPos: {_scrollRect.normalizedPosition}");
            if (EventData2BlockIdx(eventData, out var blockIdx))
            {
                _lastPressedBlockIdx = blockIdx;
                _blocks[_lastPressedBlockIdx].OnPointerDown();
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            // Debug.Log($"BoardView.OnPointerUp, pos: {eventData.position}");
            if (EventData2BlockIdx(eventData, out var blockIdx))
            {
                _lastPressedBlockIdx = -1;
                _blocks[blockIdx].OnPointerUp();
            }
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            // Debug.Log($"BoardView.OnPointerMove, pos: {eventData.position}");
            if (!eventData.dragging) return;
            if (EventData2BlockIdx(eventData, out var blockIdx))
            {
                if (blockIdx != _lastPressedBlockIdx)
                {
                    _blocks[blockIdx].OnPointerDragEnter();
                    _lastPressedBlockIdx = blockIdx;
                }
            }
            else
            {
                _lastPressedBlockIdx = -1;
            }
            if (_lastPressedBlockIdx > 0) _blocks[_lastPressedBlockIdx].OnPointerDragExit();
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
    }
}