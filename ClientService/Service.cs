using Common.Model;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;

namespace ClientService
{
    public partial class Service : ServiceBase
    {
        private Timer Schedular;
        protected bool IsStopped = false;
        protected string EventName = "";
        protected string LogFileName = "";
        protected string SourceName = "";
        private static string SystemNames = ConfigurationManager.AppSettings["SystemNames"];

        public Service()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            Init();
            LogToFile("Started");
            ScheduleService();
        }

        protected override void OnStop()
        {
        }

        public void ScheduleService()
        {
            try
            {
                Schedular = new Timer(new TimerCallback(SchedularCallback));

                DateTime scheduledFromTime = DateTime.MinValue;
                DateTime scheduledToTime = DateTime.MinValue;
                DateTime scheduledTime = DateTime.MinValue;

                int EverySeconds = Convert.ToInt32(ConfigurationManager.AppSettings["EverySeconds"]);
                scheduledFromTime = DateTime.Parse(ConfigurationManager.AppSettings["ScheduledFromTime"]);
                scheduledToTime = DateTime.Parse(ConfigurationManager.AppSettings["ScheduledToTime"]);

                if (DateTime.Now > scheduledToTime)
                {
                    scheduledTime = scheduledFromTime.AddDays(1);
                }
                else
                {
                    scheduledTime = DateTime.Now.AddSeconds(EverySeconds);
                    if (DateTime.Now > scheduledTime)
                    {
                        scheduledTime = scheduledTime.AddSeconds(EverySeconds);
                    }
                }

                TimeSpan timeSpan = scheduledTime.Subtract(DateTime.Now);

                int dueTime = Convert.ToInt32(timeSpan.TotalMilliseconds);

                Schedular.Change(dueTime, Timeout.Infinite);
            }
            catch (Exception ex)
            {
                string message = "ScheduleService;" + ex.Message;
                if (ex.InnerException != null)
                {
                    message = ";innerMessage:" + ex.InnerException.Message;
                }

                LogToFile(message);

                using (ServiceController serviceController = new ServiceController("SimpleService"))
                {
                    serviceController.Stop();
                }
            }
        }

        private void SchedularCallback(object e)
        {
            var config = ConfigurationManager.AppSettings["config"];
            Execute(config);
            ScheduleService();
        }

        private Configure ReadServiceConfigure(string input)
        {
            try
            {
                Configure configure = new Configure();
                input = ConfigurationManager.AppSettings["Configure_" + input];
                var items = input.Split(';');
                foreach (var item in items)
                {
                    if (string.IsNullOrWhiteSpace(item))
                    {
                        continue;
                    }
                    var parts = item.Split('=');
                    if (parts.Length != 2)
                    {
                        continue;
                    }
                    if (parts[0].ToLower() == "username")
                    {
                        configure.UserName = parts[1];
                    }
                    else if (parts[0].ToLower() == "password")
                    {
                        configure.Password = parts[1];
                    }
                    else if (parts[0].ToLower() == "passkey")
                    {
                        configure.PassKey = parts[1];
                    }
                    else if (parts[0].ToLower() == "serviceurl")
                    {
                        configure.ServiceUrl = parts[1];
                    }
                }
                return configure;
            }
            catch (Exception ex)
            {
                string message ="Execption in configFile;"+ ex.Message;
                if (ex.InnerException != null)
                {
                    message = ";innerMessage:" + ex.InnerException.Message;
                }
                LogToFile(message);
                return null;
            }
        }

        public void Execute(string config)
        {
            try
            {
                var systemNames = SystemNames.Split(',');
                foreach (var systemName in systemNames)
                {
                    var configure = ReadServiceConfigure(systemName);
                    if (configure == null)
                    {
                        continue;
                    }
                    try
                    {
                        string url = configure.ServiceUrl + "/api/time";
                        User user = new User
                        {
                            PassWord = configure.Password,
                            UserName = configure.UserName
                        };
                        string content = SendToApi(url, JsonConvert.SerializeObject(user));

                        if (!string.IsNullOrWhiteSpace(content))
                        {
                            if (content.StartsWith("\"") && content.EndsWith("\""))
                            {
                                content = content.Trim('"');
                            }
                            string json = Common.Method.StringCipher.Decrypt(content, configure.PassKey);
                            if (!string.IsNullOrWhiteSpace(json))
                            {
                                List<PersonnelTime> personnelTimes = JsonConvert.DeserializeObject<List<PersonnelTime>>(json);
                                List<InputPersonnelTime> input = new List<InputPersonnelTime>();
                                foreach (var item in personnelTimes)
                                {
                                    using (var db = new PersonnelTimeEntities())
                                    {
                                        var personnel = db.PersonnelTimes.Where(a => a.ID == item.ID && a.Location== systemName).FirstOrDefault();
                                        if (personnel != null)
                                        {
                                            input.Add(new InputPersonnelTime
                                            {
                                                Id = item.ID,
                                                Status = 2
                                            });
                                        }
                                        else
                                        {
                                            db.PersonnelTimes.Add(new PersonnelTime
                                            {
                                                ID = item.ID,
                                                CODE = item.CODE,
                                                Date = item.Date,
                                                PersonnelID = item.PersonnelID,
                                                PersonnelName = item.PersonnelName,
                                                ShamsiDate = item.ShamsiDate,
                                                Status = item.Status,
                                                Time = item.Time,
                                                ZarrinPersonnelCode = item.ZarrinPersonnelCode,
                                                Location=systemName,
                                            });
                                            db.SaveChanges();
                                            input.Add(new InputPersonnelTime
                                            {
                                                Id = item.ID,
                                                Status = 1
                                            });
                                        }
                                    }
                                }
                                if (personnelTimes.Count > 0)
                                {
                                    url = configure.ServiceUrl + "/api/status";
                                    SendToApi(url, JsonConvert.SerializeObject(input));
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        string message = "InnerBlock;" + configure.ServiceUrl + ";" + ex.Message;
                        if (ex.InnerException != null)
                        {
                            message = ";innerMessage:" + ex.InnerException.Message;
                        }
                        LogToFile(message);
                    }
                }
            }
            catch (Exception ex)
            {
                string message = "MainBlock;"+ ex.Message;
                if (ex.InnerException != null)
                {
                    message = ";innerMessage:" + ex.InnerException.Message;
                }
                LogToFile(message);
            }
        }

        private string SendToApi(string url, string input)
        {
            try
            {
                var client = new RestClient(url);
                client.Timeout = -1;
                var request = new RestRequest(Method.POST);
                request.AddParameter("application/x-www-form-urlencoded", input, ParameterType.RequestBody);
                IRestResponse response = client.Execute(request);
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    return response.Content;
                }
                else
                {
                    LogToFile(url, (int)response.StatusCode,response.StatusDescription, 0);
                    return "";
                }

            }
            catch (Exception ex)
            {
                string message = "SendToApi;" + url + ";" + ex.Message;
                if (ex.InnerException != null)
                {
                    message = ";innerMessage:" + ex.InnerException.Message;
                }

                LogToFile(message);
                return "";
            }
        }

        protected virtual void LogToFile(string url, int statusCode,string statusDescription, long? responseTime)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("url:");
            sb.Append(url);
            sb.Append(";statusCode:");
            sb.Append(statusCode.ToString());
            sb.Append(";statusDescription:");
            sb.Append(statusDescription.ToString());
            sb.Append(";duration:");
            sb.Append(responseTime);

            LogToFile(sb.ToString());
        }
        protected virtual void LogToFile(string log)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + "\\Logs";
            log = "datetime:" + DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt") + ";" + log;
            StringBuilder pathBuilder = new StringBuilder(AppDomain.CurrentDomain.BaseDirectory);
            pathBuilder.Append("\\Logs");
            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                pathBuilder.Append("\\");
                pathBuilder.Append(LogFileName);
                pathBuilder.Append("_");
                pathBuilder.Append(DateTime.Now.ToString("yyyyMMdd"));
                pathBuilder.Append(".txt");
                string filepath = pathBuilder.ToString();

                if (!File.Exists(filepath))
                {
                    // Create a file to write to.   
                    using (StreamWriter sw = File.CreateText(filepath))
                    {
                        sw.WriteLine(log);
                    }
                }
                else
                {
                    using (StreamWriter sw = File.AppendText(filepath))
                    {
                        sw.WriteLine(log);
                    }
                }
            }
            catch { }

        }

        protected virtual void Init()
        {
            SourceName = this.GetType().Namespace;
            LogFileName = "Error";
        }

    }
}
