# QFL Bot Genie


## Updates 3Commas QFL multipair bot settings (MaxActiveDeals, Take Profit, QFL Percentage) based on BTC's 1d RSI and 1h ATRp every few minutes

MaxActiveDeals =  RSI_SCALAR*(RSI_UPPER - RSI)

(RSI_SCALAR defaulted to 1.0--this lets you increase or decrease deals relative to RSI value)

(RSI_UPPER defaulted TO 70--once the RSI of BTC approaches this value we set MaxActiveDeals to 1)

QFL% = ATRp * QFLP_SCALAR

(QFLP_SCALAR defaulted to 3.5 but you can increase if you want to be more picky when opening deals relative to ATRp)

TP% = ATRp * TP_SCALAR

(TP_SCALAR defaulted to 1.5 but you can increase if you want higher take profit relative to ATRp at time deal opens)

Also cycles through the QFL timeframes (original=1h, daytrade=2h, conservative=3h, position=4h) each time so that more QFL deals may be presented


## Instructions:

Create new console C# program and include your 3c api key, secret, botId, and accountId (or exchange name) for updating bot settings. This can be done with visual studio community edition (free on windows or mac) or using .NET core SDK on linux with the dotnet command.


You will need to include XCommas.Net and Skender.Stock.Indicators nuget packages


*Disclaimer: No idea how well this strategy will work over simply running static QFL*
