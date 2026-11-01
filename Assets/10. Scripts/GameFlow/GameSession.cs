using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Jay;
using UnityEngine;

namespace PROJECT3D
{
    // 방 안의 플레이어 한 명
    // 지금은 솔로라 1명. 멀티가 붙으면 네트워크 코드가 이 목록을 채움
    public class PlayerSlot
    {
        public int PlayerID;
        public string Name;
        public int UnitID;          // 고른 캐릭터
        public StatClass ShopStat;  // 상점에서 산 스탯
        public bool IsLocal;        // 내 캐릭터인지
        public bool IsReady;        // 준비 완료 (멀티용)
    }

    // 씬이 바뀌어도 유지되는 데이터
    // - Data  : 내 저장 데이터 (골드, 캐릭터, 상점 구매 단계)
    // - Slots : 이번 방의 참가자들
    public class GameSession : SingletonBase<GameSession>
    {
        public PlayerData Data { get; private set; }
        public List<PlayerSlot> Slots { get; private set; } = new List<PlayerSlot>();

        // 골드/구매/캐릭터가 바뀌었을 때 (로비 UI 갱신용)
        public event Action OnDataChanged;

        protected override void OnInitialize()
        {
            Data = SaveService.Load();
        }

        public void Save()
        {
            SaveService.Save(Data);
        }
        
        // 게임을 끌 때 저장 (로비 상점 구매 등)
        // 인게임 도중에 끄면 저장하지 않는다 (이번 판 골드는 로비로 돌아가야 저장됨)
        protected override void OnApplicationQuit()
        {
            if (SceneManager.GetActiveScene().name != SceneLoader.InGameScene) Save();
            base.OnApplicationQuit();
        }

        #region 방

        // 솔로 방 만들기 : 내 슬롯 하나
        public void CreateSoloRoom()
        {
            Slots.Clear();

            PlayerSlot slot = new PlayerSlot();
            slot.Name = "Player";
            slot.IsLocal = true;
            slot.IsReady = true;
            Slots.Add(slot);

            RefreshLocalSlot();
        }

        public PlayerSlot GetLocalSlot()
        {
            for (int i = 0; i < Slots.Count; i++)
            {
                if (Slots[i].IsLocal) return Slots[i];
            }
            return null;
        }

        // 내 저장 데이터(캐릭터, 상점 구매)를 내 슬롯에 반영
        public void RefreshLocalSlot()
        {
            PlayerSlot slot = GetLocalSlot();
            if (slot == null) return;

            slot.UnitID = Data.SelectedUnitID;
            slot.ShopStat = TableLoader.Instance.ShopTable.BuildShopStat(Data.ShopLevels);
        }

        #endregion

        #region 골드

        // 골드 얻기 (게임 종료 정산, 테스트)
        public void AddGold(int amount)
        {
            if (amount <= 0) return;

            Data.Gold += amount;
            OnDataChanged?.Invoke();
        }

        #endregion

        #region 로비 (캐릭터 선택, 상점)

        public void SelectUnit(int unitID)
        {
            Data.SelectedUnitID = unitID;
            RefreshLocalSlot();
            OnDataChanged?.Invoke();
        }

        // 상점 구매. 최대 단계거나 골드가 부족하면 false
        public bool TryBuyShop(StatType type)
        {
            ShopTable shopTable = TableLoader.Instance.ShopTable;

            int level = Data.GetShopLevel(type);
            if (level >= shopTable.GetMaxLevel(type)) return false;

            int cost = shopTable.Get(type, level + 1).Cost;
            if (Data.Gold < cost)
            {
                Debug.Log($"[GameSession] 골드가 부족합니다. 필요 : {cost}, 보유 : {Data.Gold}");
                return false;
            }

            Data.Gold -= cost;
            Data.ShopLevels[(int)type]++;

            RefreshLocalSlot();
            OnDataChanged?.Invoke();
            return true;
        }

        #endregion
    }
}