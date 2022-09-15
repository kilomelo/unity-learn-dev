using System;
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

        public Color NumberColor(int num)
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
                blockView.SetData(_game, i, _board.GetBlock(i), this);
                _blocks[i] = blockView;
            }
        }

        private GameObject GetBlockInstance()
        {
            // todo exception
            if (null == _blockTemplate) throw new NullReferenceException("");
            return Instantiate(_blockTemplate);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            Debug.Log($"BoardView.OnPointerDown, pos: {eventData.position}, scrollPos: {_scrollRect.normalizedPosition}");
            _lastPressedBlockIdx = EventData2BlockIdx(eventData);
            _blocks[_lastPressedBlockIdx].OnPointerDown();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Debug.Log($"BoardView.OnPointerUp, pos: {eventData.position}");
            _lastPressedBlockIdx = -1;
            _blocks[EventData2BlockIdx(eventData)].OnPointerUp();

        }

        public void OnPointerMove(PointerEventData eventData)
        {
            Debug.Log($"BoardView.OnPointerMove, pos: {eventData.position}");
            if (!eventData.dragging) return;
            var currentPressedBlock = EventData2BlockIdx(eventData);
            if (currentPressedBlock != _lastPressedBlockIdx && _lastPressedBlockIdx != -1)
            {
                _blocks[_lastPressedBlockIdx].OnPointerDragExit();
            }
            _blocks[currentPressedBlock].OnPointerDragEnter();
            _lastPressedBlockIdx = currentPressedBlock;
        }

        private int EventData2BlockIdx(PointerEventData eventData)
        {
            // Debug.Log($"scrollRect.contentSize: {_scrollRect.content.rect}");
            var viewRect = _scrollRect.viewport.rect;
            var contentRect = _scrollRect.content.rect;
            var scrollPosX = _scrollRect.normalizedPosition.x * (contentRect.width - viewRect.width);
            var scrollPosY = (1f - _scrollRect.normalizedPosition.y) * (contentRect.height - viewRect.height);
            var xPosInBoard = eventData.position.x + scrollPosX;
            var yPosInBoard = viewRect.height - eventData.position.y + scrollPosY;
            // Debug.Log($"scrollPosX: {scrollPosX}, scrollPosY: {scrollPosY}");
            var xIdx = Mathf.FloorToInt(xPosInBoard / _blockSize);
            var yIdx = Mathf.FloorToInt(yPosInBoard / _blockSize);
            // Debug.Log($"xIdx: {xIdx}, yIdx: {yIdx}");
            if (xIdx < 0 || xIdx >= _board.Width || yIdx < 0 || yIdx >= _board.Height)
            {
                // todo exception
                throw new Exception();
            }
            var blockIdx = xIdx + yIdx * _board.Width;
            if (blockIdx >= _blocks.Length)
            {
                // todo exception
                throw new IndexOutOfRangeException();
            }
            return blockIdx;
        }
    }
}