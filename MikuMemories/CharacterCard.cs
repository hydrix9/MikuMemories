using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MikuMemories
{
    public class CharacterCard
    {

    public string name { get; set; }
    public string description { get; set; }
    public string personality { get; set; }
    public string first_mes { get; set; }
    public string avatar { get; set; }
    public string chat { get; set; }
    public string mes_example { get; set; }
    public string scenario { get; set; }
    public string create_date { get; set; }
    public string char_greeting { get; set; }
    public string example_dialogue { get; set; }
    public string world_scenario { get; set; }
    public string char_persona { get; set; }
    public string char_name { get; set; }
        public static CharacterCard FromJson(JObject jsonData)
        {
            return JsonConvert.DeserializeObject<CharacterCard>(jsonData.ToString());
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

}
