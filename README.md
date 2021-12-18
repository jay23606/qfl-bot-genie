# qfl-deal-genie
Updates 3c qfl multipair bot settings (maxactivedeals, take profit, qfl percent) based on BTC's 1d RSI and 1hr ATRp each minute

Also cycles through the qfl timeframe (original=1h, daytrade=2h, conservative=3h, position=4h) each minute

Instructions:

Create new console c# program and include your 3c api key, secret, botId, and accountId (or exchange name) for updating bot settings


You will need to include XCommas.Net and Stock.Indicators nuget packages


Disclaimer: No idea how well this strategy will work over simply running qfl
