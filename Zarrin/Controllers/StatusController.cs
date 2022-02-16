using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace Zarrin.Controllers
{
    public class StatusController : ApiController
    {
        // GET: api/Status
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET: api/Status/5
        public string Get(int id)
        {
            return "value";
        }

        // POST: api/Status
        //public void Post([FromBody]string value)
        //{


        //}
        public async Task<HttpResponseMessage> Post(HttpRequestMessage request)
        {
            var jsonContent = await request.Content.ReadAsStringAsync();

            List<PersonnelTime> personnels = JsonConvert.DeserializeObject<List<PersonnelTime>>(jsonContent);
            using (var db = new PersonnelTimeEntities())
            {
                foreach (var item in personnels)
                {
                    if (item.Status == 2)//duplicate
                    {
                        var p = db.PersonnelTimes.Where(a=>a.ID== item.ID).ToList();
                        if (p != null && p.Count>0)
                        {
                            int noOfRowUpdated = db.Database.ExecuteSqlCommand("Update PersonnelTime SET Status =1  WHERE ID=" + item.ID);
                        }
                    }
                    if (item.Status == 1)// new 
                    {
                        var p = db.PersonnelTimes.Where(a => a.ID == item.ID).ToList();
                        if (p != null && p.Count > 0)
                        {
                            int noOfRowUpdated = db.Database.ExecuteSqlCommand("Update PersonnelTime SET Status =1  WHERE ID=" + item.ID);
                        }
                    }
                }
                return Request.CreateResponse(HttpStatusCode.OK, "");
            }
        }

        // PUT: api/Status/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE: api/Status/5
        public void Delete(int id)
        {
        }
    }
}
