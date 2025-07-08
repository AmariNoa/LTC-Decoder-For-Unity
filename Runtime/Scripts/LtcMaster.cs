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
        // タイムコードの更新時に通知を受け取るイベント関数
        [SerializeField] private UnityEvent<string, double> onTimeCodeUpdated;
        [SerializeField] private bool debugLog;

        public string TimeCode { get; private set; }
        public double TimeSeconds { get; private set;}

        private LtcReceiver _currentReceiver;


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
                /*
                if (debugLog)
                {
                    Debug.LogWarning("No valid ltc signal.");
                }
                */

                TimeCode = timeCode;
                TimeSeconds = timeSeconds;
                return;
            }

            TimeCode = timeCode;
            TimeSeconds = timeSeconds;

            onTimeCodeUpdated.Invoke(TimeCode, TimeSeconds);
        }
    }
}
