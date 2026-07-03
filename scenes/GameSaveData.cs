using Godot;
using System.Collections.Generic;

namespace EternalForest.Game
{
    public class GameSaveData
    {
        public Vector2 PlayerPosition { get; set; }
        public float PlayerHealth { get; set; }
        public string CurrentLevel { get; set; }
        public bool MapGenerated { get; set; }
        public bool FloorGenerated { get; set; }
        
        // Listes d'items de l'inventaire sérialisées sous forme de Dictionnaires Godot
        public List<Godot.Collections.Dictionary> Inventory { get; set; } = new List<Godot.Collections.Dictionary>();
        public List<Godot.Collections.Dictionary> Hotbar { get; set; } = new List<Godot.Collections.Dictionary>();
        
        public Godot.Collections.Dictionary ToJson()
        {
            var data = new Godot.Collections.Dictionary();
            
            data["player_position_x"] = PlayerPosition.X;
            data["player_position_y"] = PlayerPosition.Y;
            data["player_health"] = PlayerHealth;
            data["current_level"] = CurrentLevel;
            data["map_generated"] = MapGenerated;
            data["floor_generated"] = FloorGenerated;
            
            // Sérialiser l'inventaire principal
            var invArray = new Godot.Collections.Array();
            foreach (var item in Inventory)
            {
                invArray.Add(item);
            }
            data["inventory"] = invArray;

            // Sérialiser la hotbar
            var hbArray = new Godot.Collections.Array();
            foreach (var item in Hotbar)
            {
                hbArray.Add(item);
            }
            data["hotbar"] = hbArray;
            
            return data;
        }
        
        public static GameSaveData FromJson(Godot.Collections.Dictionary data)
        {
            var saveData = new GameSaveData();
            
            if (data.ContainsKey("player_position_x") && data.ContainsKey("player_position_y"))
            {
                saveData.PlayerPosition = new Vector2(
                    (float)data["player_position_x"],
                    (float)data["player_position_y"]
                );
            }
            
            if (data.ContainsKey("player_health"))
            {
                saveData.PlayerHealth = (float)data["player_health"];
            }
            
            if (data.ContainsKey("current_level"))
            {
                saveData.CurrentLevel = (string)data["current_level"];
            }
            
            if (data.ContainsKey("map_generated"))
            {
                saveData.MapGenerated = (bool)data["map_generated"];
            }
            
            if (data.ContainsKey("floor_generated"))
            {
                saveData.FloorGenerated = (bool)data["floor_generated"];
            }

            if (data.ContainsKey("inventory"))
            {
                var invArray = data["inventory"].AsGodotArray();
                foreach (var itemVal in invArray)
                {
                    if (itemVal.VariantType == Variant.Type.Dictionary)
                    {
                        saveData.Inventory.Add(itemVal.AsGodotDictionary());
                    }
                    else
                    {
                        saveData.Inventory.Add(null);
                    }
                }
            }

            if (data.ContainsKey("hotbar"))
            {
                var hbArray = data["hotbar"].AsGodotArray();
                foreach (var itemVal in hbArray)
                {
                    if (itemVal.VariantType == Variant.Type.Dictionary)
                    {
                        saveData.Hotbar.Add(itemVal.AsGodotDictionary());
                    }
                    else
                    {
                        saveData.Hotbar.Add(null);
                    }
                }
            }
            
            return saveData;
        }
    }
}