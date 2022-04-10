using UnityEngine;
using UnityEngine.Networking;
using R2API.Networking.Interfaces;

namespace EphemeralCoins
{
    public class CoinStorage
    {
        public NetworkInstanceId user;
        public string name;
        public uint ephemeralCoinCount;
    }

    public class SyncCoinStorage : INetMessage
    {
        public NetworkInstanceId user;
        public string name;
        public uint ephemeralCoinCount;

        public SyncCoinStorage(){}

        public SyncCoinStorage(NetworkInstanceId user, string name, uint ephemeralCoinCount)
        {
            this.user = user;
            this.name = name;
            this.ephemeralCoinCount = ephemeralCoinCount;
        }

        public void Deserialize(NetworkReader reader)
        {
            user = reader.ReadNetworkId();
            name = reader.ReadString();
            ephemeralCoinCount = reader.ReadUInt32();
        }

        public void OnReceived()
        {
            if (NetworkServer.active)
            {
                EphemeralCoins.Logger.LogWarning("SyncCoinStorage: Host ran this. Skipping.");
                return;
            }
            CoinStorage newPlayer = new CoinStorage();
            newPlayer.user = user;
            newPlayer.name = name;
            newPlayer.ephemeralCoinCount = ephemeralCoinCount;

            bool flag = false;
            foreach (CoinStorage player in EphemeralCoins.instance.coinCounts)
            {
                if (player.user.Equals(newPlayer.user))
                {
                    player.name = newPlayer.name;
                    player.ephemeralCoinCount = newPlayer.ephemeralCoinCount;
                    flag = true;
                    break;
                }
            }
            if (!flag) EphemeralCoins.instance.coinCounts.Add(newPlayer);
        }

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(user);
            writer.Write(name);
            writer.Write(ephemeralCoinCount);
        }
    }
}
