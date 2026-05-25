using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PolyclinicApp.Services
{
    public static class CurrentUser
    {
        public static int UserId { get; set; }

        public static string Login { get; set; }

        public static int RoleId { get; set; }

        public static int? DoctorId { get; set; }

        public static bool IsAdmin
        {
            get { return RoleId == 1; }
        }

        public static bool IsRegistrar
        {
            get { return RoleId == 2; }
        }

        public static bool IsDoctor
        {
            get { return RoleId == 3; }
        }

        public static bool IsLaborant
        {
            get { return RoleId == 4; }
        }

        public static bool IsChiefDoctor
        {
            get { return RoleId == 5; }
        }

        public static string RoleName
        {
            get
            {
                switch (RoleId)
                {
                    case 1:
                        return "Администратор";
                    case 2:
                        return "Регистратор";
                    case 3:
                        return "Врач";
                    case 4:
                        return "Лаборант";
                    case 5:
                        return "Главный врач";
                    default:
                        return "Неизвестная роль";
                }
            }
        }
    }
}

