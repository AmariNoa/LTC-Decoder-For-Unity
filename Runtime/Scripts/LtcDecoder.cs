// LTC Timecode Reader for Unity C#
// http://blog.mobilehackerz.jp/
// https://twitter.com/MobileHackerz

// https://note.com/hikohiro/n/n4c6a248f0910

// Modified by AmariNoa
// https://github.com/AmariNoa

using System;
using System.Linq;
using UnityEngine;

namespace com.amari_noa.unity.ltc.decoder
{
    public class LtcDecoder
    {
        // 内部で録音するバッファの長さ
        private const int DeviceRecLength = 10;

        public bool DecodeSucceededLastFrame { get; private set; }
        public string TimeCode { get; private set; } = "--:--:--;--";
        public double TimeSeconds { get; private set; }
        public float Gain { get; private set; }
        public float Rms { get; private set; }
        public float Db { get; private set; }

        private string _deviceName;

        private AudioClip _ltcAudioInput;
        private int _lastAudioPos;
        private int _sameAudioLevelCount;
        private int _lastAudioLevel;
        private int _lastBitCount;
        private string _bitPattern = "";


        public void StartDevice(string deviceName, int frequency) {
            _deviceName = "";

            foreach (var device in Microphone.devices)
            {
                if (!device.Equals(deviceName))
                {
                    continue;
                }

                _deviceName = device;
                break;
            }

            if (string.IsNullOrEmpty(_deviceName))
            {
                Debug.LogError($"Microphone device not found. ({deviceName})");
                return;
            }

            Debug.Log($"Microphone device found. ({_deviceName})");

            _ltcAudioInput = Microphone.Start(_deviceName, true, DeviceRecLength, frequency);
            if (_ltcAudioInput == null) {
                Debug.LogError("Microphone.Start failed.");
            }
        }

        // 現在までのオーディオ入力を取得しフレーム情報にデコードしていく
        public void DecodeAudioToTcFrames(float audioThreshold)
        {
            DecodeSucceededLastFrame = false;

            var waveData = GetUpdatedAudio(_ltcAudioInput);

            if (waveData.Length == 0)
            {
                Rms = 0f;
                Db = 0f;
                Gain = 0f;
                return;
            }

            // デシベル計算
            var sumSquares = waveData.Sum(s => s * s);
            Rms = Mathf.Sqrt(sumSquares / waveData.Length);
            Db = 20f * Mathf.Log10(Mathf.Max(Rms, 1e-6f));

            // ゲイン計算
            Gain = waveData.Select(Mathf.Abs).Sum() / waveData.Length;
            if (Gain < audioThreshold)
            {
                return;
            }

            var pos = 0;
            var bitThreshold = _ltcAudioInput.frequency / 3100; // 適当

            while (pos < waveData.Length) {
                var count = CheckAudioLevelChanged(waveData, ref pos, _ltcAudioInput.channels);
                if (count <= 0) continue;

                if (count < bitThreshold) {
                    // 「レベル変化までが短い」パターンが2回続くと1
                    if (_lastBitCount < bitThreshold) {
                        _bitPattern += "1";
                        _lastBitCount = bitThreshold; // 次はここを通らないように
                    } else {
                        _lastBitCount = count;
                    }
                } else {
                    // 「レベル変化までが長い」パターンは0
                    _bitPattern += "0";
                    _lastBitCount = count;
                }
            }

            // 1フレームぶん取れたかな？
            if (_bitPattern.Length >= 80) {
                var bPos = _bitPattern.IndexOf("0011111111111101", StringComparison.Ordinal); // SYNC WORD
                if (bPos > 0) {
                    var timeCodeBits = _bitPattern[..(bPos + 16)];
                    _bitPattern = _bitPattern[(bPos + 16)..];
                    if (timeCodeBits.Length >= 80) {
                        timeCodeBits = timeCodeBits[^80..];
                        (TimeCode, TimeSeconds) = DecodeBits(timeCodeBits, 30);
                    }
                }
            }

            // パターンマッチしなさすぎてビットパターンバッファ長くなっちゃったら削る
            if (_bitPattern.Length > 160) {
                _bitPattern = _bitPattern[80..];
            }

            DecodeSucceededLastFrame = true;
        }

        // マイク入力から録音データの生データを得る。
        // オーディオ入力が進んだぶんだけ処理して float[] に返す
        private float[] GetUpdatedAudio(AudioClip audioClip) {

            var nowAudioPos = Microphone.GetPosition(_deviceName);

            var waveData = Array.Empty<float>();

            if (_lastAudioPos < nowAudioPos) {
                var audioCount = nowAudioPos - _lastAudioPos;
                waveData = new float[audioCount];
                audioClip.GetData(waveData, _lastAudioPos);
            } else if (_lastAudioPos > nowAudioPos) {
                var audioBuffer = audioClip.samples * audioClip.channels;
                var audioCount = audioBuffer - _lastAudioPos;

                var wave1 = new float[audioCount];
                audioClip.GetData(wave1, _lastAudioPos);

                var wave2 = new float[nowAudioPos];
                if (nowAudioPos != 0) {
                    audioClip.GetData(wave2, 0);
                }

                waveData = new float[audioCount + nowAudioPos];
                wave1.CopyTo(waveData, 0);
                wave2.CopyTo(waveData, audioCount);
            }

            _lastAudioPos = nowAudioPos;

            return waveData;
        }

        // 録音データの生データから、0<1, 1>0の変化が発生するまでのカウント数を得る。
        // もしデータの最後に到達したら-1を返す。
        private int CheckAudioLevelChanged(float[] data, ref int pos, int channels) {

            while (pos < data.Length) {
                var nowLevel = Mathf.RoundToInt(Mathf.Sign(data[pos]));

                // レベル変化があった
                if (_lastAudioLevel != nowLevel) {
                    var count = _sameAudioLevelCount;
                    _sameAudioLevelCount = 0;
                    _lastAudioLevel = nowLevel;
                    return count;
                }

                // 同じレベルだった
                _sameAudioLevelCount++;
                pos += channels;
            }

            return -1;
        }


        private static int Decode1Bit(string b, int pos) {
            return int.Parse(b.Substring(pos, 1));
        }

        private static int Decode2Bits(string b, int pos) {
            var r = 0;
            r += Decode1Bit(b, pos);
            r += Decode1Bit(b, pos + 1) * 2;
            return r;
        }

        private static int Decode3Bits(string b, int pos) {
            var r = 0;
            r += Decode1Bit(b, pos);
            r += Decode1Bit(b, pos + 1) * 2;
            r += Decode1Bit(b, pos + 2) * 4;
            return r;
        }

        private static int Decode4Bits(string b, int pos) {
            var r = 0;
            r += Decode1Bit(b, pos);
            r += Decode1Bit(b, pos + 1) * 2;
            r += Decode1Bit(b, pos + 2) * 4;
            r += Decode1Bit(b, pos + 3) * 8;
            return r;
        }

        private static (string, double) DecodeBits(string bits, double frameRate)
        {
            // https://en.wikipedia.org/wiki/Linear_timecode

            var frames = Decode4Bits(bits, 0) + Decode2Bits(bits, 8) * 10;
            var secs = Decode4Bits(bits, 16) + Decode3Bits(bits, 24) * 10;
            var mins = Decode4Bits(bits, 32) + Decode3Bits(bits, 40) * 10;
            var hours = Decode4Bits(bits, 48) + Decode2Bits(bits, 56) * 10;

            return ($"{hours:D2}:{mins:D2}:{secs:D2};{frames:D2}", hours * 3600 + mins * 60 + secs + frames / frameRate);
        }

        /*
        private static string DecodeBitsToFrame(string bits) {
            // https://en.wikipedia.org/wiki/Linear_timecode

            var frames = Decode4Bits(bits, 0) + Decode2Bits(bits, 8) * 10;
            var secs = Decode4Bits(bits, 16) + Decode3Bits(bits, 24) * 10;
            var mins = Decode4Bits(bits, 32) + Decode3Bits(bits, 40) * 10;
            var hours = Decode4Bits(bits, 48) + Decode2Bits(bits, 56) * 10;

            return $"{hours:D2}:{mins:D2}:{secs:D2};{frames:D2}";
        }

        private static double DecodeBitsToSeconds(string bits, double frameRate)
        {
            var frames = Decode4Bits(bits, 0) + Decode2Bits(bits, 8) * 10;
            var secs   = Decode4Bits(bits, 16) + Decode3Bits(bits, 24) * 10;
            var mins   = Decode4Bits(bits, 32) + Decode3Bits(bits, 40) * 10;
            var hours  = Decode4Bits(bits, 48) + Decode2Bits(bits, 56) * 10;

            return hours * 3600 + mins * 60 + secs + frames / frameRate;
        }
        */
    }
}
