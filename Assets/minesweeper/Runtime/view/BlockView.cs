using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Kilomelo.minesweeper.Runtime
{
    public class BlockView : MonoBehaviour
    {
        private enum EBlockViewState : byte
        {
            Fog,
            Pressed,
            Open
        }

        [SerializeField] private Image _fog;
        [SerializeField] private Image _fogPushDown;
        [SerializeField] private TextMeshProUGUI _numberLabel;

        // 实际的周围雷数，9表示该方块为雷
        private int _numberCache;

        private EBlockViewState _state = EBlockViewState.Fog;

        private EBlockViewState state
        {
            set
            {
                if (EBlockViewState.Open == _state) return;
                if (EBlockViewState.Open == value)
                {
                    _fog.enabled = false;
                    _fogPushDown.enabled = false;
                }
                else if (EBlockViewState.Pressed == value)
                {
                    _fog.enabled = false;
                    _fogPushDown.enabled = true;
                }
                else if (EBlockViewState.Fog == value)
                {
                    _fog.enabled = true;
                    _fogPushDown.enabled = false;
                }
                _state = value;
            }
        }
        private int _idx;
        private Action<int> _digEvent;

        internal void SetData(int blockIdx, BoardView boardView)
        {
            
            _digEvent = boardView.Dig;
            _idx = blockIdx;
            _state = EBlockViewState.Fog;
            _fog.enabled = true;
            _fogPushDown.enabled = false;
        }

        internal void SetBlockType(int blockType, BoardView boardView)
        {
            // blank 或 雷
            if (9 == blockType)
            {
                _numberLabel.text = "0";
                _numberLabel.color = boardView.NumberColor(0);
                _numberCache = 9;
            
            }
            else if (0 > blockType)
            {
                _numberLabel.text = string.Empty;
                _numberCache = 0;
            }
            else
            {
                _numberLabel.text = blockType.ToString();
                _numberLabel.color = boardView.NumberColor(blockType);
                _numberCache = blockType;
            }
        }
        
        internal void OnPointerDown()
        {
            // Debug.Log($"blockView {_idx} OnPointerDown");
            state = EBlockViewState.Pressed;
        }

        internal void OnPointerUp()
        {
            state = EBlockViewState.Fog;
            _digEvent?.Invoke(_idx);
        }

        internal void OnPointerDragEnter()
        {
            state = EBlockViewState.Pressed;
        }
        
        internal void OnPointerDragExit()
        {
            // Debug.Log($"blockView {_idx} OnPointerDragExit");
            state = EBlockViewState.Fog;
        }

        internal void Open()
        {
            // Debug.Log($"BlockView.Open, idx: {_idx}");
            state = EBlockViewState.Open;
        }

        /// <summary>
        /// 获取用于序列化盘面数据的数字
        /// </summary>
        /// <returns></returns>
        internal int GetRuntimeSerializeNumber()
        {
            if (EBlockViewState.Fog == _state) return -1;
            return _numberCache;
        }
    }
}