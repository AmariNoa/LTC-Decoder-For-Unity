using UnityEditor;
using UnityEngine;

namespace com.amari_noa.unity.ltc.decoder.editor
{
    [CustomEditor(typeof(LtcMaster))]
    public class LtcMasterEditor : Editor
    {
        private float _level;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var targetScript = (LtcMaster)target;

            if (!Application.isPlaying)
            {
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("TimeCode");
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"{targetScript.TimeCode}");
            EditorGUILayout.LabelField($"{targetScript.TimeSeconds:F2}sec");
            EditorGUILayout.EndVertical();

            Repaint(); // 毎フレーム更新
        }
    }
}
