using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace HotCookies
{
    public class ProfileManager
    {
        public static async Task<List<Profile>> GetProfiles()
        {
            string apiUrl = "http://local.adspower.com:50325/api/v1/user/list?page_size=100";
            var httpClient = new HttpClient();

            var response = await httpClient.GetAsync(apiUrl);
            string responseString = await response.Content.ReadAsStringAsync();

            JObject responseDataJson = JObject.Parse(responseString);

            int code = (int)responseDataJson["code"];
            if (code != 0)
            {
                // Handle the error case here
                string errorMsg = (string)responseDataJson["msg"];
                Console.WriteLine($"Failed to get profiles: {errorMsg}");
                return null;
            }

            List<Profile> profiles = new List<Profile>();
            JArray profilesJsonArray = (JArray)responseDataJson["data"]["list"];
            foreach (JToken profileJson in profilesJsonArray)
            {
                Profile profile = new Profile
                {
                    SerialNumber = (string?)profileJson["serial_number"],
                    UserId = (string?)profileJson["user_id"],
                    Name = (string?)profileJson["name"],
                    GroupId = (string?)profileJson["group_id"],
                    GroupName = (string?)profileJson["group_name"],
                    DomainName = (string?)profileJson["domain_name"],
                    Username = (string?)profileJson["username"],
                    Remark = (string?)profileJson["remark"],
                    CreatedTime = DateTimeOffset.FromUnixTimeSeconds((long)profileJson["created_time"]).DateTime,
                    IP = (string?)profileJson["ip"],
                    IPCountry = (string?)profileJson["ip_country"],
                    Password = (string?)profileJson["password"],
                    LastOpenTime = DateTimeOffset.FromUnixTimeSeconds((long)profileJson["last_open_time"]).DateTime
                };

                profiles.Add(profile);
            }

            return profiles;
        }
    }

    public class Profile
    {
        public string? SerialNumber { get; set; }
        public string? UserId { get; set; }
        public string? Name { get; set; }
        public string? GroupId { get; set; }
        public string? GroupName { get; set; }
        public string? DomainName { get; set; }
        public string? Username { get; set; }
        public string? Remark { get; set; }
        public DateTime CreatedTime { get; set; }
        public string? IP { get; set; }
        public string? IPCountry { get; set; }
        public string? Password { get; set; }
        public DateTime LastOpenTime { get; set; }
    }
}
