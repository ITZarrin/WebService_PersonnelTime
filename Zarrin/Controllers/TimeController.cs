using Common.Method;
using Common.Model;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace Zarrin.Controllers
{
    public class TimeController : ApiController
    {

        private static string UserName = System.Configuration.ConfigurationManager.AppSettings["UserName"];
        private static string PassWord = System.Configuration.ConfigurationManager.AppSettings["PassWord"];
        private static string PassKey = System.Configuration.ConfigurationManager.AppSettings["PassKey"];
        private static string BulkSize = System.Configuration.ConfigurationManager.AppSettings["BulkSize"];
        // GET: api/Time
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET: api/Time/5
        public string Get(int id)
        {
            return "value";
        }
        public async Task<HttpResponseMessage> Post(HttpRequestMessage request)
        {
            var jsonContent = await request.Content.ReadAsStringAsync();
            User user = JsonConvert.DeserializeObject<User>(jsonContent);
            int bulkSize = 50;
            if (!int.TryParse(BulkSize, out bulkSize))
            {
                bulkSize = 50;
            }
            if (string.IsNullOrWhiteSpace(UserName) && string.IsNullOrWhiteSpace(PassWord))
            {
                return Request.CreateResponse(HttpStatusCode.Forbidden, "");
            }
            if (UserName != user.UserName || PassWord != user.PassWord)
            {
                return Request.CreateResponse(HttpStatusCode.Forbidden, "");
            }
            using (var db = new PersonnelTimeEntities())
            {
                var list = db.PersonnelTimes.Where(a => a.Status == 0).ToList();
                return Request.CreateResponse(HttpStatusCode.OK, StringCipher.Encrypt(JsonConvert.SerializeObject(list), PassKey));
            }
        }


        // PUT: api/Time/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE: api/Time/5
        public void Delete(int id)
        {
        }
    }
}
