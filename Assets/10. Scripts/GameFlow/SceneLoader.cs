using UnityEngine.SceneManagement;

namespace PROJECT3D
{
    // 씬 이름을 한 곳에서 관리
    public static class SceneLoader
    {
        public const string TitleScene = "Title";
        public const string LobbyScene = "Robby";
        public const string InGameScene = "3D Game";

        public static void LoadTitle()
        {
            SceneManager.LoadScene(TitleScene);
        }

        public static void LoadLobby()
        {
            SceneManager.LoadScene(LobbyScene);
        }

        public static void LoadInGame()
        {
            SceneManager.LoadScene(InGameScene);
        }
    }
}