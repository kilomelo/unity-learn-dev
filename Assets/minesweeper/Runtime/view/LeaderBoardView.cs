using System;
using System.IO;
using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kilomelo.minesweeper.Runtime
{
    public class LeaderBoardView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _switchBtnText;
        [SerializeField] private Button _continueBtn;
        [SerializeField] private Button _switchBtn;
        [SerializeField] private VerticalLayoutGroup _layout;
        [SerializeField] private CanvasGroup _canvasGroup;

        [SerializeField] private LeaderboardItem[] _leaderboardItems;
        [SerializeField] private TextMeshProUGUI _noRecordTip;
        // [SerializeField] private ContentSizeFilter _sizeFilter;

        private readonly string[] _leaderBoardTitles = new string[] {
            "历史.时间记录", "历史.3BV记录", "近100局.时间记录", "近100局.3BV记录"
        };
        private readonly Type[] _leaderBoardTypes = new Type[] {
            typeof(TimeRecord), typeof(ThreeBVRecord), typeof(TimeRecordRecent), typeof(ThreeBVRecordRecent)
        };

        private Game _game;
        private int _leaderBoardIdx = 0;

        private void Awake()
        {
            _continueBtn.onClick.AddListener(() => {gameObject.SetActive(false);});
            _switchBtn.onClick.AddListener(() => {
                if (++_leaderBoardIdx >= _leaderBoardTitles.Length) _leaderBoardIdx = 0;
                RefreshContent();
            });
        }

        internal void SetData(Game game)
        {
            _game = game;
        }

        private void RefreshContent()
        {
            Debug.Log("LeaderBoardView.RefreshContent");
            var boardWidth = _game.CurBoard.Width;
            var boardHeight = _game.CurBoard.Height;
            var leaderBoardType = _leaderBoardTypes[_leaderBoardIdx];
            BestRecord record = null;
            if (leaderBoardType.IsSubclassOf(typeof(BestRecordRecent)))
            {
                record = (BestRecord)Activator.CreateInstance(leaderBoardType, boardWidth, boardHeight, 100);
            }
            else record = (BestRecord)Activator.CreateInstance(leaderBoardType, boardWidth, boardHeight);
            var recordCnt = record.IterateRecords((i, finishTime,threebv, date, boardData, playbackData) =>
            {
                if (i >= _leaderboardItems.Length)
                {
                    Debug.Log("LeaderBoardView.RefreshContent, i >= _leaderboardItems.Length");
                    return;
                }

                _leaderboardItems[i].TimeLabel.text = $"{finishTime * 0.001f:F1}";
                _leaderboardItems[i].ThreebvLabel.text = threebv.ToString();
                _leaderboardItems[i].ThreebvsLabel.text = $"{(float)threebv / finishTime * 1000:F1}";
                _leaderboardItems[i].DateLabel.text = date;
                _leaderboardItems[i].gameObject.SetActive(true);
                _leaderboardItems[i].OnClick += () =>
                {
                    _game.LoadBoard(boardData);
                };
            });
            Debug.Log($"LeaderBoardView.RefreshContent, recordCnt: {recordCnt}");

            for (var i = recordCnt; i < _leaderboardItems.Length; i++)
            {
                _leaderboardItems[i].gameObject.SetActive(false);
            }

            _noRecordTip.gameObject.SetActive(recordCnt == 0);
            _switchBtnText.text = _leaderBoardTitles[_leaderBoardIdx];
            StartCoroutine(RefreshContentSize());
        }

        private IEnumerator RefreshContentSize()
        {
            yield return 0;
            Canvas.ForceUpdateCanvases();
            _layout.enabled = false;
            _layout.enabled = true;
            yield return 0;
            _canvasGroup.alpha = 1f;
        }

        private void OnEnable()
        {
            RefreshContent();
            _canvasGroup.alpha = 0f;
            StartCoroutine(RefreshContentSize());
        }

        private void OnDisable()
        {
            _leaderBoardIdx = 0;
        }
    }
}