using System;
using System.Collections.Generic;

namespace RollAndRuin.Core
{
    // DTO save run. Hanya field publik + [Serializable] supaya bisa dipakai Unity JsonUtility
    // tanpa Core bergantung ke UnityEngine.

    [Serializable]
    public sealed class RunSnapshot
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public int seed;
        /// <summary>State RNG sebagai teks (ulong tidak aman di semua serializer).</summary>
        public string rngState;
        public int floorIndex;
        public int turnNumber;

        public BoardData board = new BoardData();
        public ActorData player = new ActorData();
        public bool hasRival;
        public ActorData rival = new ActorData();
        public int rivalPersonality;
        public bool rivalReachedBossFirst;

        public List<RelicData> relics = new List<RelicData>();
        public List<string> relicPool = new List<string>();
        public List<string> dicePool = new List<string>();
        public int trapCharges;
        public int snakeCharms;
        public List<int> armedTraps = new List<int>();
        public StatsData stats = new StatsData();
        public List<ShopData> shops = new List<ShopData>();
    }

    [Serializable]
    public sealed class BoardData
    {
        public int size;
        public List<int> tiles = new List<int>();
        public List<JumpData> jumps = new List<JumpData>();
    }

    [Serializable]
    public sealed class JumpData
    {
        public int from;
        public int to;
        public int kind;
    }

    [Serializable]
    public sealed class ActorData
    {
        public int position;
        public int hp;
        public int maxHp;
        public int gold;
        public int poison;
        public int stun;
        public int lastRoll = -1;
        public List<DieData> dice = new List<DieData>();
    }

    [Serializable]
    public sealed class DieData
    {
        public string id;
        public List<int> values = new List<int>();
        public List<string> effects = new List<string>();
        public int cooldownTurns;
        public int currentCooldown;
        public int special;
        public int compassMin;
        public int compassMax;
    }

    [Serializable]
    public sealed class RelicData
    {
        public string id;
        public string state;
    }

    [Serializable]
    public sealed class StatsData
    {
        public int enemiesDefeated;
        public int snakesSlain;
        public int bossesDefeated;
        public int rivalDuelsWon;
        public int goldEarned;
    }

    [Serializable]
    public sealed class ShopData
    {
        public int tile;
        public List<ShopItemData> items = new List<ShopItemData>();
    }

    [Serializable]
    public sealed class ShopItemData
    {
        public int kind;
        public string id;
        public int price;
        public bool sold;
    }
}
