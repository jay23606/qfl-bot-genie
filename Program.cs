using System;
using System.Linq;
using XCommas.Net;
using XCommas.Net.Objects;
using System.Collections.Generic;
using System.Threading.Tasks;
using Skender.Stock.Indicators;
using System.Net;
using System.Net.Http;

namespace qflg
{
    class Program
    {
        //important constants
        static float RSI_UPPER = 70f; //if BTC 1d RSI >= 69 then we only open a single active qfl deal
        static float RSI_LOWER = 20f;
        static float RSI_SCALAR = 1.0f; //increasing will produce more deals and reducing will produce less deals
        static decimal QFLP_SCALAR = 3.5m; //factor we multiply by BTC 1h ATRp to determine QFL percentage
        static decimal TP_SCALAR = 2m; //factor we multiply by BTC 1h ATRp to determine Take Profit percentage

        //update with your 3c api key/secret and exchange info
        static string key = "xxx";
        static string secret = "xxx";
        static string market = "kucoin";
        static int accountId = 0;
        static int botId = 1234567;
        public static XCommasApi api;

        static void Main() { MainAsync().GetAwaiter().GetResult(); }
        static async Task MainAsync()
        {
            api = new XCommasApi(key, secret, default, UserMode.Real);
            var accts = await api.GetAccountsAsync();
            foreach (var acct in accts.Data) if (acct.MarketCode == market) { accountId = acct.Id; break; }

            while (true)
            {
                try
                {
                    List<Quote> quotesDay = GetCandles("BTCUSDT", "1d", 140, "binance").ToList();
                    List<Quote> quotesHour = GetCandles("BTCUSDT", "1h", 140, "binance").ToList();

                    List<RsiResult> resultsDay = quotesDay.GetRsi().ToList();
                    int lastDay = resultsDay.Count - 1;

                    float rsi = (float)resultsDay[lastDay].Rsi;
                    if (RSI_LOWER > rsi) rsi = RSI_LOWER;

                    int deals = (int)(RSI_SCALAR * (RSI_UPPER - rsi)); //maximum of 50 deals when RSI is 20
                    if (deals < 1) deals = 1;

                    Console.WriteLine($"BTC 1 day RSI @ {DateTime.UtcNow.ToShortTimeString()} UTC is {Decimal.Round((decimal)resultsDay[lastDay].Rsi, 2)} -> set MaxActiveDeals = {RSI_SCALAR}*({RSI_UPPER} - RSI) = {deals}");

                    List<AtrResult> resultsHour = quotesHour.GetAtr().ToList();
                    int lastHour = resultsDay.Count - 1;

                    decimal qflp = Math.Round((decimal)resultsHour[lastHour].Atrp * QFLP_SCALAR, 1);
                    if (qflp < 3m) qflp = 3m;
                    decimal tp = Math.Round((decimal)resultsHour[lastHour].Atrp * TP_SCALAR, 1);

                    Console.WriteLine($"BTC 1 hour ATRp @ {DateTime.UtcNow.ToShortTimeString()} UTC is {Decimal.Round((decimal)resultsHour[lastHour].Atrp, 2)} -> set QFL % = ATRp*{QFLP_SCALAR} = {qflp}, set TP % = ATRp*{TP_SCALAR} = {tp}");

                    var bots = await api.GetBotsAsync(limit: 1000, accountId: accountId, botId: botId);
                    Bot bot = null;
                    //passing botId to GetBotsAsync doesn't work so need to find the right bot by name I guess
                    foreach (Bot b in bots.Data) if (b.Name == "KC200 QFL") { bot = b; break; }

                    //loop through qfl timeframes so that there is more potential deals
                    foreach (BotStrategy bs in bot.Strategies)
                    {
                        if (bs is QflBotStrategy)
                        {
                            QflBotStrategy qfl = (QflBotStrategy)bs;
                            qfl.Options.Type++;
                            if ((int)qfl.Options.Type > 3) qfl.Options.Type = 0; //you could change this to 1 if you do not want Original 
                            qfl.Options.Percent = qflp;
                            break;
                        }
                    }

                    //set maxactivedeals based on 70 minus 1d RSI value
                    //we want to take on less risk as BTC becomes overbought
                    bot.MaxActiveDeals = deals;

                    //set take profit using 1.5 * ATRp
                    //idea: deals created during big dips should expect more profit
                    bot.TakeProfit = tp;

                    await api.UpdateBotAsync(botId, new BotUpdateData(bot));

                }
                catch(Exception ex) 
                {
                    Console.WriteLine("ERROR: " + ex.Message);

                    //maybe these failed, so get new instances
                    api = new XCommasApi(key, secret, default, UserMode.Real);
                    hc = new HttpClient();
                    
                }
                //update every few minutes
                await Task.Delay(1000 * 60 * 3);
            }
        }

        public static HttpClient hc = new HttpClient();
        public static string DATA(HttpResponseMessage res) => res.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        public static HttpResponseMessage GET(string uri) => hc.GetAsync(uri.Trim()).GetAwaiter().GetResult();
        public static DateTime BinanceTimeStampToUtcDateTime(string binanceTimeStamp) { return DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(binanceTimeStamp)).DateTime; }
        public static IEnumerable<Quote> GetCandles(string pair = "SHIBUSDT", string interval = "1h", int period = 21, string exchange = "binance", DateTimeOffset? startTime = null)
        {
            List<Quote> quotes = new List<Quote>();
            switch (exchange)
            {
                case "binance":
                    var res = GET("https://" + $"api.binance.com/api/v3/klines?symbol={pair}&interval={interval}&limit={period}{((startTime != null) ? "&startTime=" + ((DateTimeOffset)startTime).ToUnixTimeMilliseconds() : "")}");
                    if (res.StatusCode == HttpStatusCode.OK)
                    {
                        string output = DATA(res);
                        output = output.Substring(2, output.Length - 4);
                        string[] candles = output.Split(new string[] { "],[" }, StringSplitOptions.None);

                        foreach (string candle in candles)
                        {
                            string[] s = candle.Split(',');
                            Quote q = new Quote();
                            q.Date = BinanceTimeStampToUtcDateTime(s[0]);
                            q.Open = Convert.ToDecimal(s[1].Replace("\"", ""));
                            q.High = Convert.ToDecimal(s[2].Replace("\"", ""));
                            q.Low = Convert.ToDecimal(s[3].Replace("\"", ""));
                            q.Close = Convert.ToDecimal(s[4].Replace("\"", ""));
                            q.Volume = Convert.ToDecimal(s[5].Replace("\"", ""));
                            quotes.Add(q);
                        }
                    }
                    break;
            }
            return quotes;
        }
    }
}
