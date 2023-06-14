using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace HotCookies
{
    public class GroupManager
    {
        public static async Task<List<Group>> GetGroups()
        {
            string apiUrl = "http://local.adspower.com:50325/api/v1/group/list?page_size=100";
            var httpClient = new HttpClient();

            var response = await httpClient.GetAsync(apiUrl);
            string responseString = await response.Content.ReadAsStringAsync();

            JObject responseDataJson = JObject.Parse(responseString);

            int code = (int)responseDataJson["code"];
            if (code != 0)
            {
                // Handle the error case here
                string errorMsg = (string?)responseDataJson["msg"];
                Console.WriteLine($"Failed to get groups: {errorMsg}");
                return null;
            }

            List<Group> groups = new List<Group>();
            JArray groupsJsonArray = (JArray)responseDataJson["data"]["list"];
            foreach (JToken groupJson in groupsJsonArray)
            {
                Group group = new Group
                {
                    GroupId = (string?)groupJson["group_id"],
                    GroupName = (string?)groupJson["group_name"]
                };

                groups.Add(group);
            }

            return groups;
        }
    }

    public class Group
    {
        public string? GroupId { get; set; }
        public string? GroupName { get; set; }
    }
}
