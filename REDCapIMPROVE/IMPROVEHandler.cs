using Logging;
using REDCapAPI;
using System.Collections.Generic;
using System.Data;

namespace REDCapIMPROVE
{
    class IMPROVEHandler
    {
        protected API redcapAPI;
        protected Logger log;

        public IMPROVEHandler(API redcapAPI, Logger log)
        {
            this.redcapAPI = redcapAPI;
            this.log = log;
        }

        public Dictionary<string, string> getData(List<string> vars, string record)
        {
            string fields = string.Join(",", vars.ToArray());

            fields = "record_id" + "," + fields;

            DataTable dt = redcapAPI.GetTableFromRC("record_id", record, "", "", "", false, false);

            DataRow dr = dt.Rows[0];

            Dictionary<string, string> result = new Dictionary<string, string>();

            foreach(string var in vars)
            {
                result.Add(var, dr[var].ToString());
            }

            return result;
        }

        public string getDAG(string record)
        {
            DataTable dt = redcapAPI.GetTableFromRC("record_id", record, "", "", "", false, true);
            
            return dt.Rows[0]["redcap_data_access_group"].ToString();
        }

        public string uploadCSV(string csv, bool overwrite = true)
        {
            return redcapAPI.RCImportCSVFlat(csv, overwrite);
        }
    }
}
