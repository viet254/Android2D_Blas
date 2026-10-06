using UnityEngine;
namespace Brotherhood
{
    public static class GameSettings
    {
        public const string ScreenShakeKey="BrotherhoodScreenShake";
        public static bool ScreenShake
        {
            get
            {
                if(!PlayerPrefs.HasKey(ScreenShakeKey)&&PlayerPrefs.HasKey("BrotherhoodShake"))
                    PlayerPrefs.SetInt(ScreenShakeKey,PlayerPrefs.GetInt("BrotherhoodShake",1));
                return PlayerPrefs.GetInt(ScreenShakeKey,1)==1;
            }
            set{PlayerPrefs.SetInt(ScreenShakeKey,value?1:0);PlayerPrefs.Save();}
        }
        public static bool AchievementPopups=>PlayerPrefs.GetInt("BrotherhoodAchievementPopups",1)==1;
        public static bool HowToPlay=>PlayerPrefs.GetInt("BrotherhoodHowToPlay",1)==1;
    }
}
