﻿using System.Net;
using Serilog;
using YuchiGames.POM.Server.Network.Listeners;

namespace YuchiGames.POM.Server.Network.Utilities
{
    public static class Utils
    {
        public static bool ContainAddress(IPEndPoint iPEndPoint)
        {
            try
            {
                for (int i = 0; i < Tcp.iPEndPoints.Length; i++)
                {
                    if (Tcp.iPEndPoints[i] == default)
                    {
                        continue;
                    }
                    if (Tcp.iPEndPoints[i].Address.Equals(iPEndPoint.Address))
                    {
                        return true;
                    }
                }
                return false;
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                return false;
            }
        }
    }
}