using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace com.amari_noa.unity.ltc.decoder
{
    public class LtcMaster : MonoBehaviour
    {
        // リストの上にある(index番号の小さい)ものが優先して処理される
        [SerializeField] private List<LtcReceiver> ltcReceivers;
        // LTC信号が止まってからタイムアウト(停止扱いと)するまでの時間
        [SerializeField] private float timeoutSeconds = 0.2f;
        [Space]
        // タイムコードの更新時に通知を受け取るイベント関数
        [SerializeField] private UnityEvent<string, double> onTimeCodeUpdated;
        // タイムコードの停止時に通知を受け取るイベント関数
        [SerializeField] private UnityEvent onTimeCodeStopped;
        // デバッグログの出力フラグ
        [SerializeField] private bool debugLog;

        public string TimeCode { get; private set; }
        public double TimeSeconds { get; private set;}

        private LtcReceiver _currentReceiver;
        private float _timeoutTimer;
        private bool _stopped;


        private bool TryGetTimeCode(out string timeCode, out double timeSeconds)
        {
            foreach (var receiver in ltcReceivers.Where(receiver => receiver.HasValidSignal()))
            {
                if (receiver != _currentReceiver)
                {
                    if (debugLog)
                    {
                        Debug.Log($"Current receiver changed. ({(_currentReceiver ? _currentReceiver.name : "(No device)")} -> {receiver.name})");
                    }

                    _currentReceiver = receiver;
                }

                // 取得に成功した一番最初のレシーバーの値を使う
                timeCode = receiver.TimeCode;
                timeSeconds = receiver.TimeSeconds;

                return true;
            }

            // デコードに成功した最後のタイムコードを返す(内部処理が壊れないようにするため)
            timeCode = TimeCode;
            timeSeconds = TimeSeconds;

            return false;
        }

        private void Start()
        {
            if (debugLog)
            {
                Debug.Log("Microphone Devices:");
                foreach (var device in Microphone.devices) {
                    Debug.Log($"{device}");
                }
            }
        }

        private void Update()
        {
            if (!TryGetTimeCode(out var timeCode, out var timeSeconds))
            {
                // 信号停止中
                TimeCode = timeCode;
                TimeSeconds = timeSeconds;

                // タイムアウト系の処理
                if (!_stopped)
                {
                    // タイムアウトタイマー加算
                    _timeoutTimer += Time.deltaTime;
                    if (_timeoutTimer >= timeoutSeconds)
                    {
                        // 閾値を超えたらタイムアウト(停止)扱いとする
                        _stopped = true;
                        // 停止通知
                        onTimeCodeStopped.Invoke();

                        if (debugLog)
                        {
                            Debug.Log("LTC signal lost.");
                        }
                    }
                }

                return;
            }

            // 信号受信中
            TimeCode = timeCode;
            TimeSeconds = timeSeconds;
            // デコード結果を通知
            onTimeCodeUpdated.Invoke(TimeCode, TimeSeconds);

            // タイムアウト扱いの諸々を消し飛ばす
            _stopped = false;
            _timeoutTimer = 0f;
        }
    }
}
