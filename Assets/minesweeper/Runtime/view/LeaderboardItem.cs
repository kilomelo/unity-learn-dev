using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Kilomelo.minesweeper.Runtime
{
    public class LeaderboardItem : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public TextMeshProUGUI TimeLabel;
        public TextMeshProUGUI ThreebvLabel;
        public TextMeshProUGUI ThreebvsLabel;
        public TextMeshProUGUI DateLabel;

        internal Action OnClick;
        public void OnPointerDown(PointerEventData eventData)
        {
            Debug.Log($"LeaderboardItem.OnPointerDown, eventData.pointerId: {eventData.pointerId}");
            OnClick?.Invoke();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Debug.Log($"LeaderboardItem.OnPointerUp, eventData.pointerId: {eventData.pointerId}");
        }
    }
}