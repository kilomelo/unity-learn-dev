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
        private bool _valid;

        private void Awake()
        {
            if (null == _blockTemplate || null == _scrollRect || null == _blockLayout || null == _numberColor)
            {
                // todo exception
                throw new Exception("");
            }
            // // 只允许正方形格子
            // if (Math.Abs(_blockLayout.cellSize.x - _blockLayout.cellSize.y) > 0.01f) throw new Exception();
            // _blockSize = _blockLayout.cellSize.x;
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
                    Destroy(block.gameObject);
                    // block.gameObject.SetActive(false);
                    // _blockCache.Push(block);
                }
                _blocks = null;
            }
        }

        internal void SetData(Game game, Board board)
        {
            CheckValid();
            // todo exception
            _board = board ?? throw new NullReferenceException("");
            _game = game ?? throw new NullReferenceException("");
            _blockLayout.cellSize = Vector2.one * _scrollRect.viewport.rect.height / _board.Height;
            _blockSize = _blockLayout.cellSize.x;
            Clear();
            InitBlocks();
        }

        internal void BockChanged(int changedBlockIdx)
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
                        _blocks[blockIdx].Open();
                    }
                }
            }
        }

        internal void GameStateChanged(Game.EGameState gameState)
        {
            CheckValid();
            if (Game.EGameState.BeforeStart == gameState)
            {
                Clear();
                InitBlocks();
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
            return Instantiate(_blockTemplate);
            // if (_blockCache.Count == 0) return Instantiate(_blockTemplate);
            // var instance = _blockCache.Pop().gameObject;
            // instance.SetActive(true);
            // return instance;
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
            if (_lastPressedBlockIdx < 0) return;
            // Debug.Log($"BoardView.OnPointerUp, pos: {eventData.position}");
            if (EventData2BlockIdx(eventData, out var blockIdx))
            {
                if (_lastPressedBlockIdx == blockIdx)
                {
                    _blocks[blockIdx].OnPointerUp();
                    Vibration.Vibrate(50);
                    _audioSource.Play();
                }
                else _blocks[_lastPressedBlockIdx].OnPointerDragExit();
            }
            _lastPressedBlockIdx = -1;
        }

        public void OnPointerMove(PointerEventData eventData)
        {
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
    }
}