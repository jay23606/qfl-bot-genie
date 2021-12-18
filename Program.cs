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
        static string key = "xxx";
        static string secret = "xxx";
        static string market = "kucoin";
        static int accountId = 0;
        static int botId = 6486100;
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
                    int deals = 70 - (int)resultsDay[lastDay].Rsi;

                    Console.WriteLine($"BTC 1 day RSI @ {DateTime.UtcNow.ToShortTimeString()} UTC is {Decimal.Round((decimal)resultsDay[lastDay].Rsi, 8)} -> set deals = 70-rsi = {deals}\n");

                    List<AtrResult> resultsHour = quotesHour.GetAtr().ToList();
                    int lastHour = resultsDay.Count - 1;

                    decimal qflp = Math.Round((decimal)resultsHour[lastHour].Atrp * 5m, 1);
                    decimal tp = Math.Round((decimal)resultsHour[lastHour].Atrp * 1.5m, 1);

                    Console.WriteLine($"BTC 1 hour nATR @ {DateTime.UtcNow.ToShortTimeString()} UTC is {Decimal.Round((decimal)resultsHour[lastHour].Atrp, 8)} -> set qfl% = nATR*5 = {qflp}, set tp% = nATR*1.5 = {tp}\n\n");

                    var bots = await api.GetBotsAsync(limit: 1, accountId: accountId, botId: botId);

                    Bot bot = bots.Data[0];

                    //loop through qfl timeframes so that there might be more potential deals
                    QflBotStrategy qfl = (QflBotStrategy)bot.Strategies[0];
                    qfl.Options.Type++;
                    if ((int)qfl.Options.Type > 3) qfl.Options.Type = 0;

                    //set qfl percent based on 5 * nATR (get more picky as volatility expands)
                    qfl.Options.Percent = qflp;

                    //set maxactivedeals based on 70 minus 1d RSI value
                    //we want to take on less risk as BTC becomes overbought
                    bot.MaxActiveDeals = deals;

                    //set take profit using 1.5 * nATR
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

                await Task.Delay(1000 * 60 * 1);
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
