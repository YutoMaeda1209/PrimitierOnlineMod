using MelonLoader;

namespace YuchiGames.POM.Client
{
    public class Program : MelonMod
    {
        public override void OnInitializeMelon()
        {
            Settings.Initialize();
        }
    }
}