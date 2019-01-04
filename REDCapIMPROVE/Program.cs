using Logging;
using REDCapAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace REDCapIMPROVE
{
    class Program
    {
        private static Dictionary<string, string> postArgs;
        private static Dictionary<string, string> config;
        private static Dictionary<string, string> transferFields;
        private static Dictionary<string, string> instrumentDesignations;
        private static Dictionary<string, string> formDesignations;
        private static Dictionary<string, string> dags;
        private static Dictionary<string, string> instrumentFromForm;

        static void Main(string[] args)
        {
            string logPath = Path.Combine(Directory.GetCurrentDirectory(), "log");
            string instrumentDesignationsPath = Path.Combine(Directory.GetCurrentDirectory(), "instrumentDesignations.csv");
            string formDesignationsPath = Path.Combine(Directory.GetCurrentDirectory(), "formDesignations.csv");
            string dagsPath = Path.Combine(Directory.GetCurrentDirectory(), "dags.csv");
            string logName = DateTime.Now.Year.ToString() + DateTime.Now.Month.ToString().PadLeft(2, '0') + DateTime.Now.Day.ToString().PadLeft(2, '0') + ".log";
            Logger log = new Logger(logPath);

            postArgs = new Dictionary<string, string>();


            foreach (string argument in args)
            {
                string argumentClean = HttpUtility.UrlDecode(argument);

                var split = argumentClean.Split(new char[] { '=' }, 2, StringSplitOptions.RemoveEmptyEntries);

                if (split.Length >= 2) postArgs.Add(split[0], split[1]);
            }

            try
            {
                string configPath = Path.Combine(Environment.CurrentDirectory, "config.txt");
                config = LoadConfig(configPath);
                transferFields = LoadConfigTransfer(configPath);
                instrumentDesignations = loadConversionTable(instrumentDesignationsPath);
                formDesignations = loadConversionTable(formDesignationsPath);
                dags = loadConversionTable(dagsPath);
                instrumentFromForm = new Dictionary<string, string>();
                foreach(KeyValuePair<string, string> kvp in formDesignations)
                {
                    string form = kvp.Value;
                    string instrument = instrumentDesignations[kvp.Key];

                    if (!instrumentFromForm.ContainsKey(form)) instrumentFromForm.Add(form, instrument);
                }


                IMPROVEHandler improve = new IMPROVEHandler(new API(config["strURI"], config["strPostTokenIMPROVE"]), log);
                IMPROVEITHandler improveIt = new IMPROVEITHandler(new API(config["strURI"], config["strPostTokenIMPROVEIT"]), log);

                if (postArgs["instrument"].Equals("inclusion")) //Baseline
                {
                    log.Log("baseline_arm_1");

                    int improveID = Convert.ToInt32(improveIt.getIMPROVEID(postArgs["record"]));

                    Dictionary<string, string> improveData = improve.getData(transferFields.Keys.ToList(), improveID.ToString());
                    
                    improveData = (from kv in improveData
                                     where (kv.Value != "" && !kv.Key.Contains("___") && !kv.Key.Contains("complete")) || //Remove all blanks
                                     (kv.Key.Contains("___") && !kv.Value.Contains("0") && !kv.Key.Contains("complete")) || //Remove all unchecked multiple choice
                                     (!kv.Key.Contains("___") && kv.Key.Contains("complete") && !kv.Value.Contains("0")) //Remove all un-complete forms
                              select kv).ToDictionary(kv => kv.Key, kv => kv.Value);


                    string improveDAG = improve.getDAG(postArgs["record"]);

                    List<string> formsTouched = new List<string>();

                    foreach(KeyValuePair<string, string> kvp in improveData)
                    {
                        formsTouched.Add(formDesignations[transferFields[kvp.Key]]);

                        Dictionary<string, string> insertData = new Dictionary<string, string>();

                        insertData.Add("in_study_id", improveID.ToString());
                        insertData.Add("redcap_event_name", instrumentDesignations[transferFields[kvp.Key]]);
                        insertData.Add("redcap_data_access_group", dags[improveDAG]);
                        insertData.Add(transferFields[kvp.Key], kvp.Value);

                        string insertCSV = CSVFromDir(insertData);
                        log.Log(insertCSV);

                        improveIt.uploadCSV(insertCSV);
                    }

                    foreach(string form in formsTouched.Distinct().ToList())
                    {
                        Dictionary<string, string> insertData = new Dictionary<string, string>();

                        insertData.Add("in_study_id", improveID.ToString());
                        insertData.Add("redcap_event_name", instrumentFromForm[form]);
                        insertData.Add("redcap_data_access_group", dags[improveDAG]);
                        insertData.Add(form + "_complete", "0");

                        string insertCSV = CSVFromDir(insertData);
                        log.Log(insertCSV);

                        improveIt.uploadCSV(insertCSV, false);
                    }

                    int h = 0;
                }

            }
            catch (Exception ex)
            {
                log.insertSectionSeperator("ERROR OCCURED");
                log.Log(ex.ToString());
            }
            finally
            {
                log.Dispose();
            }
        }

        private static Dictionary<string, string> loadConversionTable(string tablePath)
        {
            Dictionary<string, string> conversionTable = new Dictionary<string, string>();
            using (StreamReader sr = new StreamReader(tablePath, System.Text.Encoding.Default))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    string[] parameter = line.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parameter.Length == 2)
                    {
                        conversionTable.Add(parameter[0], parameter[1]);
                    }

                }
            }
            return conversionTable;
        }

        /// <summary>
        /// Simply joins a directory into a csv string for REDCap import
        /// </summary>
        /// <param name="dir"></param>
        /// <returns></returns>
        private static string CSVFromDir(Dictionary<string, string> dir)
        {
            string line1 = string.Join(",", dir.Keys);
            string line2 = "\"" + string.Join("\",\"", dir.Values) + "\"";

            return line1 + "\n" + line2 + "\n";
        }

        private static Dictionary<string, string> LoadConfig(string configPath)
        {
            Dictionary<string, string> config = new Dictionary<string, string>();
            using (StreamReader sr = new StreamReader(configPath, System.Text.Encoding.Default))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (line.Contains("="))
                    {
                        string[] parameter = line.Split(new char[] { '=' }, 2, StringSplitOptions.RemoveEmptyEntries);

                        config.Add(parameter[0], parameter[1]);
                    }
                }
            }
            return config;
        }

        private static Dictionary<string, string> LoadConfigTransfer(string configPath)
        {
            Dictionary<string, string> config = new Dictionary<string, string>();
            using (StreamReader sr = new StreamReader(configPath, System.Text.Encoding.Default))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (line.Contains(">"))
                    {
                        string[] parameter = line.Split(new char[] { '>' }, 2, StringSplitOptions.RemoveEmptyEntries);

                        config.Add(parameter[0], parameter[1]);
                    }
                }
            }
            return config;
        }
    }
}
