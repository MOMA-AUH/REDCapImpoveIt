using Logging;
using REDCapAPI;
using System.Data;

namespace REDCapIMPROVE
{
    class IMPROVEITHandler : IMPROVEHandler
    {
        public IMPROVEITHandler(API redcapAPI, Logger log) : base(redcapAPI, log)
        {
        }

        public string getIMPROVEID(string record)
        {
            DataTable dt = redcapAPI.GetTableFromRC("in_study_id", record, "in_study_id, redcap_event_name, in_improve_id, inclusion_complete", "", "", false, false);

            DataRow patientData = dt.Select("redcap_event_name = 'baseline_arm_1'")[0];

            return patientData["in_improve_id"].ToString();
        }
    }
}
