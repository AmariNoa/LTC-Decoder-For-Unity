using UnityEditor;
using UnityEngine;

namespace com.amari_noa.unity.ltc.decoder.editor
{
    [CustomEditor(typeof(LtcReceiver))]
    public class LtcReceiverEditor : Editor
    {
        private float _level;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var targetScript = (LtcReceiver)target;

            if (!Application.isPlaying)
            {
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("TimeCode");
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"{targetScript.TimeCode}");
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Level");
            EditorGUILayout.BeginVertical("box");
            _level = Mathf.Clamp01(targetScript.Rms * 10f); // 正規化して[0,1]範囲に
            var rect = GUILayoutUtility.GetRect(100, 10);
            EditorGUI.DrawRect(rect, Color.black);
            var fill = new Rect(rect.x, rect.y, rect.width * _level, rect.height);
            EditorGUI.DrawRect(fill, Color.green);
            EditorGUILayout.LabelField($"Gain: {targetScript.Gain:F2}");
            EditorGUILayout.LabelField($"db: {targetScript.Db:F2}");
            EditorGUILayout.LabelField($"RMS: {targetScript.Rms:F2}");
            EditorGUILayout.EndVertical();

            Repaint(); // 毎フレーム更新
        }
    }
}
