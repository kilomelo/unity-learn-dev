using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Kilomelo.minesweeper.Runtime
{
    public class BlockView : MonoBehaviour
        // IPointerDownHandler, IPointerUpHandler,
        // IPointerEnterHandler, IPointerExitHandler,
        // IPointerClickHandler, IPointerMoveHandler,
        // IEndDragHandler
    {
        private enum EBlockViewState
        {
            Fog,
            Pressed,
            Digged
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
                if (EBlockViewState.Digged == _state) return;
                if (EBlockViewState.Digged == value)
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
            if (9 == blockType || 0 > blockType)
            {
                _number.enabled = false;
            }
            else
            {
                _number.text = blockType.ToString();
                _number.color = boardView.NumberColor(blockType);
            }
            _digEvent = game.Dig;
            _idx = blockIdx;
            
            _fog.enabled = true;
            _fogPushDown.enabled = true;
        }

        internal void OnPointerDown()
        {
            state = EBlockViewState.Pressed;
        }

        internal void OnPointerUp()
        {
            state = EBlockViewState.Pressed;
            _digEvent?.Invoke(_idx);
        }

        internal void OnPointerDragEnter()
        {
            state = EBlockViewState.Pressed;
        }
        
        internal void OnPointerDragExit()
        {
            state = EBlockViewState.Fog;
        }
    }
}