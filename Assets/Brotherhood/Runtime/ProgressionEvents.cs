using System;
using UnityEngine;
namespace Brotherhood
{
    // Source Core.Events bridge: subscribers can observe the same named signal
    // without depending on the restored input, HUD or combat state machines.
    public sealed class ProgressionEvents:MonoBehaviour
    {
        BrotherhoodGame game;
        public event Action<string,string> Raised;
        public void Initialize(BrotherhoodGame owner){game=owner;}
        public void Raise(string name,string parameter="")
        {
            if(name=="PENANCE")game.progress.penanceUses++;
            Raised?.Invoke(name,parameter);
        }
    }
}
