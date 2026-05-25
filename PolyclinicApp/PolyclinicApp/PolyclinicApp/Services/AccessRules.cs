using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PolyclinicApp.Services
{
    public static class AccessRules
    {
        public static bool CanEditPatients()
        {
            return CurrentUser.IsChiefDoctor || CurrentUser.IsRegistrar;
        }

        public static bool CanEditDoctors()
        {
            return CurrentUser.IsChiefDoctor;
        }

        public static bool CanEditAppointments()
        {
            return CurrentUser.IsChiefDoctor ||
                   CurrentUser.IsRegistrar ||
                   CurrentUser.IsDoctor;
        }

        public static bool CanEditMedicalRecords()
        {
            return CurrentUser.IsChiefDoctor || CurrentUser.IsDoctor;
        }

        public static bool CanEditPrescriptions()
        {
            return CurrentUser.IsChiefDoctor || CurrentUser.IsDoctor;
        }

        public static bool CanEditReferrals()
        {
            return CurrentUser.IsChiefDoctor ||
                   CurrentUser.IsRegistrar ||
                   CurrentUser.IsDoctor;
        }

        public static bool CanEditLaboratory()
        {
            return CurrentUser.IsChiefDoctor || CurrentUser.IsLaborant;
        }

        public static bool CanEditSchedule()
        {
            return CurrentUser.IsChiefDoctor ||
                   CurrentUser.IsDoctor ||
                   CurrentUser.IsLaborant;
        }

        public static bool CanEditCabinets()
        {
            return CurrentUser.IsChiefDoctor || CurrentUser.IsLaborant;
        }

        public static bool CanEditSpecialities()
        {
            return CurrentUser.IsChiefDoctor;
        }

        public static bool CanEditDiagnoses()
        {
            return CurrentUser.IsChiefDoctor;
        }

        public static bool CanManageUsers()
        {
            return CurrentUser.IsAdmin;
        }

        public static bool CanUseBackup()
        {
            return CurrentUser.IsAdmin;
        }

        public static bool CanViewAudit()
        {
            return CurrentUser.IsAdmin || CurrentUser.IsChiefDoctor;
        }

        public static bool CanEditAudit()
        {
            return CurrentUser.IsAdmin;
        }
    }
}