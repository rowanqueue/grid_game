using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Save
{
    public class SaveLoad
    {
        public static int version = 1;
        public static void Save(int id,Logic.History.Turn turn)
        {
            Save save = new Save(turn);
            save.version = version;
            save.id = id;
            string json = JsonUtility.ToJson(save);
            string path = null;
#if UNITY_EDITOR
            path = "Assets/Resources/Saves";
#elif UNITY_ANDROID
            path = Application.persistentDataPath;
#endif
            path += "/Save" + save.id + ".json";
            System.IO.File.WriteAllText(path, json);
            #if UNITY_EDitor
            UnityEditor.AssetDatabase.Refresh();
            #endif
        }
        public static bool HasSave(int id)
        {
            string path = null;
#if UNITY_EDITOR
            path = "Assets/Resources/Saves";
#elif UNITY_ANDROID
            path = Application.persistentDataPath;
#endif
            path += "/Save"+id.ToString()+".json";
            if (System.IO.File.Exists(path) == false)
            {
                return false;
            }
            return true;
        }
        public static void DeleteSave(int id)
        {
            string path = null;
#if UNITY_EDITOR
            path = "Assets/Resources/Saves";
#elif UNITY_ANDROID
            path = Application.persistentDataPath;
#endif
            path += "/Save" + id.ToString() + ".json";
            System.IO.File.Delete(path);
        }
        public static Logic.History.Turn Load(int id)
        {
            string path = null;
#if UNITY_EDITOR
            path = "Assets/Resources/Saves";
#elif UNITY_ANDROID
            path = Application.persistentDataPath;
#endif
            path += "/Save"+id+".json";
            string json = System.IO.File.ReadAllText(path);
            Save save = JsonUtility.FromJson(json, typeof(Save)) as Save;
            if(save.version != version)
            {
                return null;
            }
            save.Load();
            return save.turn;
        }
    }
    [Serializable]
    public class Save
    {
        public int version;
        public int id;
        public Logic.History.Turn turn;
        public List<TileFlowers> flowers = new List<TileFlowers>();
        public Save(Logic.History.Turn turn)
        {
            this.turn = turn;
            foreach(Tile tile in Services.GameController.flowers.Keys)
            {
                flowers.Add(new TileFlowers(tile.tile.pos, Services.GameController.flowers[tile]));
            }
        }
        public void Load()
        {
            Debug.Log(turn);
            foreach(TileFlowers f in flowers)
            {
                f.Load();
            }
        }
    }
    [Serializable]
    public class TileFlowers
    {
        public Vector2 pos;
        public int[] flowers = new int[3];
        public TileFlowers(Vector2 p, List<Flower> _flowers)
        {
            pos = p;
            foreach(Flower f in _flowers)
            {
                flowers[(int)f.tokenColor]++;
            }
        }
        public void Load()
        {

        }
    }
}

