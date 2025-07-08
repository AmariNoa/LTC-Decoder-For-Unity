using UnityEngine;

namespace com.amari_noa.unity.ltc.decoder
{
    public class LtcReceiver : MonoBehaviour
    {
        [SerializeField] private string deviceName;
        [SerializeField] private int sampleRate = 48000;
        [SerializeField] private int ltcFrameRate = 30;
        [SerializeField, Range(0.0f, 1.0f)] private float audioGainThreshold = 0.5f;

        private LtcDecoder _ltcDecoder;

        public float Rms => _ltcDecoder?.Rms ?? 0f;
        public float Db => _ltcDecoder?.Db ?? 0f;
        public float Gain => _ltcDecoder?.Gain ?? 0f;
        public string TimeCode => _ltcDecoder?.TimeCode ?? "";
        public double TimeSeconds => _ltcDecoder?.TimeSeconds ?? 0f;


        public void SetAndStartDevice(string audioDeviceName, int audioSampleRate)
        {
            _ltcDecoder.StartDevice(audioDeviceName, audioSampleRate);
        }

        public bool HasValidSignal()
        {
            return _ltcDecoder != null && _ltcDecoder.Gain > audioGainThreshold && _ltcDecoder.DecodeSucceededLastFrame;
        }

        private void Awake()
        {
            _ltcDecoder = new LtcDecoder();
        }

        private void Start()
        {
            if (string.IsNullOrEmpty(deviceName))
            {
                return;
            }

            SetAndStartDevice(deviceName, sampleRate);
        }

        private void OnDestroy()
        {
            _ltcDecoder = null;
        }

        private void Update() {
            _ltcDecoder.DecodeAudioToTcFrames(audioGainThreshold, ltcFrameRate);
        }
    }
}
