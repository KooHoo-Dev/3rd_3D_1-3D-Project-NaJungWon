using UnityEngine.SceneManagement;

namespace PROJECT3D
{
    // 씬 이름을 한 곳에서 관리
    public static class SceneLoader
    {
        public const string TitleScene = "Title";
        public const string LobbyScene = "Robby";
        public const string InGameScene = "3D Game";

        #region 외부 설정

        public static void LoadTitle()
        {
            SceneManager.LoadScene(TitleScene);
        }

        public static void LoadLobby()
        {
            GameSession.Instance.Save(); // 게임 종료 → 로비 복귀 시 저장
            SceneManager.LoadScene(LobbyScene);
        }

        public static void LoadInGame()
        {
            GameSession.Instance.Save(); // 인게임 진입 시 저장 (로비에서 바꾼 것들)
            SceneManager.LoadScene(InGameScene);
        }

        #endregion
    }
}