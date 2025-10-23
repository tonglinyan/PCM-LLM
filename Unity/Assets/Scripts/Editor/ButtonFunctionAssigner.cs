using UnityEngine;
using Simulation;
//using LLM_DatasetGeneration;

namespace Assets.Scripts.Editor
{
#if UNITY_EDITOR
    using UnityEditor;
    [CustomEditor(typeof(Config))]
    public class SimulationConfigEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            Config config = (Config)target;

            if (GUILayout.Button("Update File"))
            {
                config.UpdateFile();
            }
            else if (GUILayout.Button("Update Scene"))
            {
                config.UpdateScene();
            }
        }
    }
#endif
}