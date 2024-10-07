using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CounterStrikeSharp.API;
using IksAdminApi;

namespace IksAdmin.Functions;


public static class BlocksFunctions
{
    public static AdminApi AdminApi = Main.AdminApi!;

    public static void Ban(PlayerBan ban)
    {
        Task.Run(async () => {
            var result = await AdminApi.AddBan(ban);
                Server.NextFrame(() => {
                switch (result)
                {
                    case 0:
                        Helper.Print(ban.Admin?.Controller, AdminApi.Localizer["ActionSuccess.BanSuccess"]);
                        break;
                }
            });
        });
    }
}