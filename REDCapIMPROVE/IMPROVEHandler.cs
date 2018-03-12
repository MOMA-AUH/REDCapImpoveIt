using CMCCColonrektal;
using Logging;
using REDCapAPI;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace REDCapIMPROVE
{
    class IMPROVEHandler
    {
        private API redcapAPI;
        private Logger log;
        private SQLinteracter cmccInteractor;

        public IMPROVEHandler(API redcapAPI, SQLinteracter cmccInteractor, Logger log)
        {
            this.redcapAPI = redcapAPI;
            this.cmccInteractor = cmccInteractor;
            this.log = log;
        }

        public void handlePatient(string improveID)
        {
            DataTable dt = redcapAPI.GetTableFromRC("bl_study_id", improveID, "bl_study_id, bl_patient_cpr, bl_patient_forename, bl_patient_surname, crf_baseline_complete, ", "", "", false, false);

            DataRow patientData = dt.Rows[0];

            Patient p = null;
            try
            {
                p = new Patient(patientData["bl_patient_forename"].ToString(), patientData["bl_patient_surname"].ToString(), patientData["bl_patient_cpr"].ToString());
            }
            catch (Exception ex)
            {
                //Probably not done, so just be silent
            }

            //if(p != null & p.passModulus())
            if (p != null)
            {
                p.PatientID = cmccInteractor.insertPatient(p);
            }
            else
            {
                //TODO: raise flag for modulus
            }

            //TODO: Update improve project ID
        }
    }
}
