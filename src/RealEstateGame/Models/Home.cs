using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using RestSharp;

namespace RealEstateGame.Models
{
    public class Home
    {
        [Key]
        public int HomeId  { get; set; }
        [ForeignKey("Player")]
        public int PlayerId   { get;set;}
        // Value of the home
        public double Value    { get;set;}
        // Street Address, 123 Example St
        public string Address { get;set;}
        // 1 if true 0 if not
        public int ForSale  { get;set;}
        public int ForRent  { get;set;}
        public int Rented   { get;set;}
        public virtual Renter renter { get; set; }
        public virtual Loan loan { get;set; }
        public int Owned     { get; set; }
        // amount of rent gained per month
        public double Rent     { get;set;}
        // amount asking for sale price
        public double Asking   { get;set;}
        // 0 is broken 10 is fully upgraded
        public int Condition { get;set;}

        public static List<Home> GenerateHomes(int playerId, int amount = 50)
        {
            List<Home> output = new List<Home>();
            Random rand = new Random();
            /*
            var zws_id = "X1-ZWz1f9dgwvdq8b_44w9p";
            var client = new RestClient("http://www.zillo.com/webservice/");
            var baseRequestUri = "GetDeepComps.htm?zws-id=" + zws_id + "&count=25&zpid=";
            List<string> zids = new List<string>();
            zids.Add("33567706");
            zids.Add("14494167");
            zids.Add("53845195");
            zids.Add("53932054");
            zids.Add("53953387");
            foreach (var zid in zids)
            {
                var req = new RestRequest(baseRequestUri + zid);
                var response = client.Execute(req);
                if (response.Content.Contains("Request successfully processed"))
                    output.AddRange(ParseZillowResponse(playerId, response.Content, rand));
            }
            */
            if (output.Count < 50)
            {
                for (var i = 0; i < amount; i++)
                {
                    output.Add(Home.GenerateRandomHome(playerId, rand));
                }
            }
            return output;
        }

        private static IEnumerable<Home> ParseZillowResponse(int playerId, string xml, Random rand = null)
        {
            if(rand == null) rand = new Random();
            // Get the original house
            var tag = "principal";
            List<Home> output = new List<Home>();
            output.Add(GetHomeFromZillowXml(GetCodeSection(xml, GetOpen(tag), GetClose(tag)), playerId, rand));
            xml = CutCode(xml, GetClose(tag));
            tag = "comp";
            List<string> xmlHomes = new List<string>();
            // Get the comps sections
            while (xml.IndexOf(GetClose(tag)) > 0)
            {
                xmlHomes.Add(GetCodeSection(xml, GetClose(tag)));
                xml = CutCode(xml, GetClose(tag));
            }

            // loop through comps 
            foreach (var xmlHome in xmlHomes)
            {
                output.Add(GetHomeFromZillowXml(xmlHome, playerId, rand));
            }
            return output;
        }

        private static Home GetHomeFromZillowXml(string xml, int playerId, Random rand)
        {
            var tag = "street";
            var address = GetCodeSection(xml, GetOpen(tag), GetClose(tag));
            var valueString = GetCodeSection(xml, GetOpen("amount currency=\"USD\""), GetClose("amount"));
            double value = 0;
            double.TryParse(valueString, out value);
            return MakeHome(address, value, playerId, rand);
            // generate home
        }

        private static string GetOpen(string tag)
        {
            return "<"+tag+">";
        }

        private static string GetClose(string tag)
        {
            return GetOpen("/"+tag);
        }

        private static string GetCodeSection(string code, string start, string end)
        {
            var indexStart = code.IndexOf(start) + start.Length;
            var length = code.IndexOf(end, indexStart) - indexStart;
            return code.Substring(indexStart, length);
        }

        private static string GetCodeSection(string code, string find)
        {
            return code.Contains(find) ? code.Substring(0, code.IndexOf(find)) : code;
        }

        private static string CutCode(string code, string cut)
        {
            return code.Substring(code.IndexOf(cut) + cut.Length);
        }

        public static Home GenerateRandomHome(int playerId, Random rand)
        {
            int homeNum = rand.Next(1000, 40000);
            string Street = "MX-5 Drive";
            if (homeNum > 35000) Street = Street;
            else if (homeNum > 30000) Street = "Miata Plaza";
            else if (homeNum > 25000) Street = "GT40 Way";
            else if (homeNum > 20000) Street = "Mazda Point";
            else if (homeNum > 15000) Street = "Audi Road";
            else if (homeNum > 10000) Street = "Flat-4 Trail";
            else if (homeNum > 5000) Street = "Volvo Ave";
            else if (homeNum > 1000) Street = "BelAir Thruway";
            string Address = homeNum + " " + Street + " Vancouver WA";
            int Condition = rand.Next(10, 20);
            int Value = 50000 + homeNum*Condition;
            if (homeNum > 30000) Value = Value*2 - Value/2;
            return MakeHome(Address, Value, playerId, rand);
        }

        public static Home MakeHome(string address, double value, int playerId, Random rand)
        {
            if (rand == null) rand = new Random();
            int condition = rand.Next(1, 10);
            return new Home()
            {
                Address = address,
                Value = value,
                PlayerId = playerId,
                ForSale = rand.Next(0,1),
                ForRent = 0,
                Rented = 0,
                Rent = 0,
                Asking = value,
                Condition = condition,
                Owned = 0
            };
        }

        public void Revalue(double city, double country, Random rand)
        {
            var local = (rand.NextDouble() - .3) / 10;

            Value = (int)Math.Floor(Value * (1 + (city + country + local)));
            if (Asking > Value)
            {
                Asking = Asking - (Asking - Value) / 2;
            }
            else
            {
                Asking = Value;
            }
        }

        public double GetCostImprovement()
        {
            if (Condition < 10)
                return Value/100 * Condition;
            return 0;
        }

        public void Improve(Random rand = null)
        {
            if (Condition < 10)
            {
                if (rand == null)
                {
                    rand = new Random();
                }
                double random = rand.Next(0, 10);
                double variance = 1 + random/100;
                double multi = (1 + (Condition * (variance) / 100));
                Value = Value*multi;
                Condition = Condition + 1;
            }
        }

        public double CostToCondition(int target)
        {
            if (Condition < target)
            {
                double cost = 0;
                double lastCost = 0;
                double val = Value;
                for (int i = Condition; i < target; i++)
                {
                    lastCost = GetCostImprovement();
                    Value = Value + lastCost;
                    cost += lastCost;
                }
                Value = val;
                return cost;
            }
            return 0;
        }

        public void ImproveToCondition(int target)
        {
            Random rand = new Random();
            while (Condition < target)
            {
                Improve(rand);
            }
        }

        public double GetDownPayment()
        {
            return Asking*.2;
        }

        public double GetFHADownPayment()
        {
            return Asking*.035;
        }

        public double GetRent()
        {
            return Value*.0075;
        }

        public void Degrade(Random rand = null)
        {
            if (Condition > 0)
            {
                if(rand == null) rand = new Random();
                double random = rand.Next(0, 20) - 10;
                if (random < -3) random += 3;
                double variance = 1 - random / 100;
                double multi = (1 - (Condition * (variance) / 100));
                Value = Value * multi;
                Condition = Condition - 1;
            }
        }
    }
}
