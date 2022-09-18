using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Kilomelo.minesweeper.Runtime
{
    public class BlockView : MonoBehaviour
    {
        private enum EBlockViewState
        {
            Fog,
            Pressed,
            Open
        }
        [SerializeField] private Image _fog;
        [SerializeField] private Image _fogPushDown;
        [SerializeField] private Image _zero;
        [SerializeField] private TextMeshProUGUI _number;

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
        internal void SetData(Game game, int blockIdx, int blockType, BoardView boardView)
        {
            // blank 或 雷
            if (9 == blockType)
            {
                _number.text = "0";
                _number.color = boardView.NumberColor(0);

            }
            else if (0 > blockType)
            {
                _number.text = string.Empty;
            }
            else
            {
                _number.text = blockType.ToString();
                _number.color = boardView.NumberColor(blockType);
            }
            _digEvent = game.Dig;
            _idx = blockIdx;
            _state = EBlockViewState.Fog;
            _fog.enabled = true;
            _fogPushDown.enabled = false;
        }
        
        internal void OnPointerDown()
        {
            Debug.Log($"blockView {_idx} OnPointerDown");
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
            Debug.Log($"blockView {_idx} OnPointerDragExit");
            state = EBlockViewState.Fog;
        }

        internal void Open()
        {
            state = EBlockViewState.Open;
        }
    }
}