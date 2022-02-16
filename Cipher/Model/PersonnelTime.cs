using System;

namespace Common.Model
{
    public class PersonnelTime
    {
        public int ID { get; set; }
        public string CODE { get; set; }
        public int PersonnelID { get; set; }
        public string ZarrinPersonnelCode { get; set; }
        public string PersonnelName { get; set; }
        public DateTime Date { get; set; }
        public string ShamsiDate { get; set; }
        public string Time { get; set; }
        public int Status { get; set; }
    }
}