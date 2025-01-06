using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Save
{
    public class SaveLoad
    {
        public static void Save(Logic.History.Turn turn)
        {
            Save save = new Save(turn);
            string json = JsonUtility.ToJson(save);
            string path = null;
            path = "Assets/Resources/MySaves/Save" + save.id + ".json";
            System.IO.File.WriteAllText(path, json);
            #if UNITY_EDitor
            UnityEditor.AssetDatabase.Refresh();
            #endif
        }
        public static bool HasSave()
        {
            string path = null;
            path = "Assets/Resources/MySaves/Save0.json";
            if (System.IO.File.Exists(path) == false)
            {
                return false;
            }
            return true;
        }
        public static Logic.History.Turn Load()
        {
            string path = null;
            path = "Assets/Resources/MySaves/Save0.json";
            string json = System.IO.File.ReadAllText(path);
            Save save = JsonUtility.FromJson(json, typeof(Save)) as Save;
            save.Load();
            return save.turn;
        }
    }
    [Serializable]
    public class Save
    {
        public int id;
        public Logic.History.Turn turn;
        public Save(Logic.History.Turn turn)
        {
            this.turn = turn;
        }
        public void Load()
        {
            Debug.Log(turn);
        }
    }
}

