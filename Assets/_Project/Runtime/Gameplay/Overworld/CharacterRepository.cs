using UnityEngine;

namespace OneStep.Gameplay.Overworld
{
    public interface ICharacterRepository
    {
        CharacterRosterData Load();
        void Save(CharacterRosterData roster);
    }

    public sealed class PlayerPrefsCharacterRepository : ICharacterRepository
    {
        private const string SaveKey = "OneStep.CharacterRoster.v1";

        public CharacterRosterData Load()
        {
            CharacterRosterData roster = null;
            if (PlayerPrefs.HasKey(SaveKey))
            {
                var json = PlayerPrefs.GetString(SaveKey, string.Empty);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    try
                    {
                        roster = JsonUtility.FromJson<CharacterRosterData>(json);
                    }
                    catch (System.Exception exception)
                    {
                        Debug.LogWarning($"Ignoring an unreadable character roster: {exception.Message}");
                    }
                }
            }

            roster ??= new CharacterRosterData();
            roster.Normalize();
            return roster;
        }

        public void Save(CharacterRosterData roster)
        {
            roster.Normalize();
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(roster));
            PlayerPrefs.Save();
        }
    }
}
